using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace btlab.IntegrationWith1c.Server
{
  public class ModuleJobs
  {

    #region Задание для обновления информации о платежах
    
    /// <summary>
    /// Задание для обновления информации о платежах
    /// </summary>
    public virtual void UpdatePaymentsJob()
    {
      Logger.Debug(btlab.IntegrationWith1c.Resources.UpdatePaymentsJobStarted);
      
      var settings = UpdatePaymentsSettings.GetAll(setting => setting.Actual.HasValue && setting.Actual.Value).ToList();
      foreach (var setting in settings) {
        if (Functions.UpdatePaymentsSetting.Check(setting)) {
          Logger.DebugFormat(btlab.IntegrationWith1c.Resources.UpdatePaymentsBySetting, setting.Name);
          UpdatePaymentsProcess(setting);
        } else {
          Logger.DebugFormat(btlab.IntegrationWith1c.Resources.IsInvalidUpdatePaymentSetting, setting.Name);
        }
      }
    }
    
    /// <summary>
    /// Обновление платежи по настройке
    /// </summary>
    /// <param name="setting">Настройка для обновления платежей</param>
    private void UpdatePaymentsProcess(IUpdatePaymentsSetting setting)
    {
      var processingFolder = Functions.UpdatePaymentsSetting.ProcessingPath(setting);
      Logger.DebugFormat(btlab.IntegrationWith1c.Resources.PocessingFilesInFolder, processingFolder);
      
      setting.LastUpdateDateTime = Calendar.Now;
      setting.Save();
      
      var files = Directory.GetFiles(processingFolder, Constants.Module.XMLFileFormat);
      foreach(var file in files) {
        UpdatePaymentsProcessFile(setting, file);
      }
    }
    
    /// <summary>
    /// Обработка файла обновлений платежей
    /// </summary>
    /// <param name="setting">Настройка для обновления платежей</param>
    /// <param name="file">Файл</param>
    private void UpdatePaymentsProcessFile(IUpdatePaymentsSetting setting, String file)
    {
      Logger.DebugFormat(btlab.IntegrationWith1c.Resources.ProcessingFile, file);
      
      System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.CreateSpecificCulture("ru-RU");
      
      // Поиск платежя
      var lines = File.ReadAllLines(file);
      var paymentsList = new List<Structures.Module.IPaymentInfo>();
      var paymentInfo = Structures.Module.PaymentInfo.Create();
      foreach (var line in lines) {
        // СекцияДокумент=
        if (line.StartsWith(Constants.Module.PaymentInfoProperties.StartDocument)) {
          Logger.Debug(btlab.IntegrationWith1c.Resources.PaymentInfoFound);
          paymentInfo = Structures.Module.PaymentInfo.Create();
          continue;
        }
        // НазначениеПлатежа=
        if (line.StartsWith(Constants.Module.PaymentInfoProperties.PaymentPurpose)) {
          // Оплата по: СЧ <номер счета (может состоять из цифр, букв, специальных символов)> от: <дата счета>
          var paymentPurpose = line.Substring(32, line.Length - 32);
          var indexOf = paymentPurpose.LastIndexOf(" от: ");
          if (indexOf > -1) {
            paymentInfo.Number = paymentPurpose.Substring(0, indexOf);
            paymentInfo.Date = DateTime.Parse(paymentPurpose.Substring(indexOf + 5, paymentPurpose.Length - indexOf - 5), culture);
          }
          continue;
        }
        /*
        // Номер=
        if (line.StartsWith(Constants.Module.PaymentInfoProperties.Number)) {
          paymentInfo.Number = line.Substring(6, line.Length - 6);
          continue;
        }
        // Дата=
        if (line.StartsWith(Constants.Module.PaymentInfoProperties.Date)) {
          paymentInfo.Date = DateTime.Parse(line.Substring(5, line.Length - 5));
          continue;
        }
        */
        // Получатель=
        if (line.StartsWith(Constants.Module.PaymentInfoProperties.Recipient)) {
          paymentInfo.Recipient = line.Substring(11, line.Length - 11);
          continue;
        }
        // ПолучательКПП=
        if (line.StartsWith(Constants.Module.PaymentInfoProperties.TRRC)) {
          paymentInfo.TRRC = line.Substring(14, line.Length - 14);
          continue;
        }
        // ПолучательИНН=
        if (line.StartsWith(Constants.Module.PaymentInfoProperties.TIN)) {
          paymentInfo.TIN = line.Substring(14, line.Length - 14);
          continue;
        }
        // КонецДокумента
        if (line.StartsWith(Constants.Module.PaymentInfoProperties.EndDocument)) {
          Logger.DebugFormat(btlab.IntegrationWith1c.Resources.PaymentInfoAdded, paymentInfo.Number, paymentInfo.Date, paymentInfo.TRRC, paymentInfo.TIN);
          paymentsList.Add(paymentInfo);
          continue;
        }
      }
      
      // Обработка найденных платежей
      foreach (var payment in paymentsList) {
        UpdatePaymentsProcessPayment(setting, payment);
      }
      
      // Перемещаем обработанный файл
      try {
        var moveFile = file.Replace(setting.ProcessingFolder, setting.ArchiveFolder);
        if (File.Exists(moveFile)) {
          File.Delete(moveFile);
        }
        File.Move(file, moveFile);
      }
      catch (Exception e) {
        Logger.ErrorFormat(btlab.IntegrationWith1c.Resources.CannotMoveProcessedFileInArchive, file);
        Logger.Error(e.Message);
      }
    }
    
    /// <summary>
    /// Обновление информации о платеже во входящем счете
    /// </summary>
    /// <param name="setting">Настройка для обновления платежей</param>
    /// <param name="payment">Платеж</param>
    private void UpdatePaymentsProcessPayment(IUpdatePaymentsSetting setting, Structures.Module.IPaymentInfo payment)
    {
      if (payment != null) {
        Logger.DebugFormat(btlab.IntegrationWith1c.Resources.ProcessingPaymentInfo, payment.Number);
        
        // Контрагент платежа
        var counterparty = Sungero.Parties.CompanyBases.GetAll(company => Equals(company.TRRC, payment.TRRC) && Equals(company.TIN, payment.TIN)).SingleOrDefault();
        if (counterparty == null) {
          Logger.DebugFormat(btlab.IntegrationWith1c.Resources.CannotFoundCounterpartyByPayment, payment.Number);
          UpdatePaymentsProcessLog(setting, payment, true);
          return;
        }
        
        // Входящий счет
        var invoice = btlab.Shiseido.IncomingInvoices.GetAll(doc => Equals(doc.Number, payment.Number) && doc.Date.HasValue
                                                             && doc.Date.Value.Year == payment.Date.Year && doc.Date.Value.Month == payment.Date.Month && doc.Date.Value.Day == payment.Date.Day
                                                             && Equals(doc.Counterparty, counterparty)).FirstOrDefault();
        if (invoice == null) {
          Logger.DebugFormat(btlab.IntegrationWith1c.Resources.CannotFoundInvoiceByPayment, payment.Number);
          UpdatePaymentsProcessLog(setting, payment, true);
          return;
        }
        
        var lockInfo = Locks.GetLockInfo(invoice);
        if (lockInfo != null && lockInfo.IsLockedByOther) {
          Logger.DebugFormat(btlab.IntegrationWith1c.Resources.IncomingInvoiceLocked, invoice.Id);
          UpdatePaymentsProcessLog(setting, payment, true);
          return;
        }
        
        try {
          invoice.LifeCycleState = btlab.Shiseido.IncomingInvoice.LifeCycleState.Paid;
          invoice.Save();
          UpdatePaymentsProcessLog(setting, payment, false);
        }
        catch (Exception e) {
          Logger.Error(e.Message);
          UpdatePaymentsProcessLog(setting, payment, true);
        }
      }
    }
    
    /// <summary>
    /// Запись информации об обновлении информации о платеже в файл логирования
    /// </summary>
    /// <param name="setting">Настройка для обновления платежей</param>
    /// <param name="payment">Информация об оплате</param>
    /// <param name="isError">Сообщение об ошибке?</param>
    private void UpdatePaymentsProcessLog(IUpdatePaymentsSetting setting, Structures.Module.IPaymentInfo payment, bool isError)
    {
      var logFile = (isError ? Functions.UpdatePaymentsSetting.ErrorPath(setting) : Functions.UpdatePaymentsSetting.ProcessedPath(setting))
        + "\\" + setting.LastUpdateDateTime.Value.ToString("dd-MM-yyyy_HH-mm-ss") + ".txt";
      
      var fileFooter = string.Empty;
      if (!File.Exists(logFile)) {
        fileFooter = isError ? btlab.IntegrationWith1c.Resources.ProcessedPaymentsWithError : btlab.IntegrationWith1c.Resources.ProcessedPayments;
      }
      
      using (StreamWriter w = File.AppendText(logFile))
      {
        if (!string.IsNullOrEmpty(fileFooter)) {
          w.WriteLine(fileFooter);
        }
        w.WriteLine(btlab.IntegrationWith1c.Resources.UpdatePaymentsProcessLogSeparator);
        w.WriteLine(string.Format(btlab.IntegrationWith1c.Resources.PaymentInfoStringPattern, Constants.Module.PaymentInfoProperties.Number, payment.Number));
        w.WriteLine(string.Format(btlab.IntegrationWith1c.Resources.PaymentInfoStringPattern, Constants.Module.PaymentInfoProperties.Date, payment.Date.ToString("dd.MM.yyyy")));
        w.WriteLine(string.Format(btlab.IntegrationWith1c.Resources.PaymentInfoStringPattern, Constants.Module.PaymentInfoProperties.Recipient, payment.Recipient));
        w.WriteLine(string.Format(btlab.IntegrationWith1c.Resources.PaymentInfoStringPattern, Constants.Module.PaymentInfoProperties.TRRC, payment.TRRC));
        w.WriteLine(string.Format(btlab.IntegrationWith1c.Resources.PaymentInfoStringPattern, Constants.Module.PaymentInfoProperties.TIN, payment.TIN));
      }
    }
    
    #endregion

  }
}