using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    #region Фоновые процессы для отправки документов в 1с
    
    #region Передача документов Directum (Акт, Накладная, УПД) -> 1C (ПоступлениеТоваровУслуг)
    /// <summary>
    /// 
    /// </summary>
    public virtual void ReceiptGoodsServicesJob()
    {
     Logger.Debug($"========= ReceiptGoodsServicesJob: {Calendar.Now} =========");
      try{
        var closingDocs = new List<btlab.Shiseido.IAccountingDocumentBase>();
        closingDocs.AddRange(btlab.Shiseido.ContractStatements.GetAll().ToArray());//Акт
        closingDocs.AddRange(btlab.Shiseido.UniversalTransferDocuments.GetAll().ToArray());//УПД
        closingDocs.AddRange(btlab.Shiseido.Waybills.GetAll().ToArray());//Накладная
        
        var docs = closingDocs
          .Where(d => d.Synhronyse1C.HasValue && d.Synhronyse1C.Value == true)
          .Where(d => !d.WasExportedTo1c.HasValue || d.WasExportedTo1c == false);
        
        foreach(var doc in docs){
          if(btlab.Shiseido.ContractStatements.Is(doc)){
            Logger.Debug($"=== Акт: {doc.Id} ===");
          }else
          if(btlab.Shiseido.UniversalTransferDocuments.Is(doc)){
            Logger.Debug($"=== УПД: {doc.Id} ===");
          }else
          if(btlab.Shiseido.Waybills.Is(doc)){
            Logger.Debug($"=== Накладная: {doc.Id} ===");
          }else{
            Logger.Debug($"=== Неведомый: {doc.Id} ===");
          }
          if(!DocIsLocked(doc)){
            ExportClosingDocTo1c(doc);
          }else{
            Logger.Debug("Документ заблокирован");
          }
        }
      }catch(Exception ex){
        Logger.Debug("err="+ex.Message+Environment.NewLine+ex.StackTrace);
      }
    }
    
    private void ExportClosingDocTo1c(btlab.Shiseido.IAccountingDocumentBase doc){

      var contractorRef = GetContractorRefFrom1c(doc.Counterparty);
      var contractRef = GetContractRefFrom1c(doc.LeadingDocument);
      var ourUnitRef = Constants.Module.OneC_ShiseidoOrgRef;
      
      var data = new Dictionary<string,object>();
      if(string.IsNullOrEmpty(doc.RegistrationNumber)){
        Logger.Debug($"Нет регистрационного номера");
        return;
      }
      
      var formatDate = Constants.Module.OneC_DateFormat;
      var docRegNum = doc.RegistrationNumber;
      //var docRegNum = "T"+Calendar.Now.ToString("HHmmss");
      //data.Add("Number", docRegNum);
      //data.Add("Date", doc.RegistrationDate.Value.ToString(formatDate));
      data.Add("Организация@odata.bind", $"Catalog_Организации(guid'{ourUnitRef}')");
      data.Add("НомерВходящегоДокумента", docRegNum);
      data.Add("ДатаВходящегоДокумента", doc.RegistrationDate.Value.ToString(formatDate));
      data.Add("Склад@odata.bind", $"Catalog_Склады(guid'{Constants.Module.OneC_CommonStorageRef}')");
      if(!string.IsNullOrEmpty(contractorRef)){
        data.Add("Контрагент@odata.bind", $"Catalog_Контрагенты(guid'{contractorRef}')");
      }
      //data.Add("Posted", true);
      if(!string.IsNullOrEmpty(contractRef)){
        data.Add("ДоговорКонтрагента@odata.bind", $"Catalog_ДоговорыКонтрагентов(guid'{contractRef}')");
      }
      if(!string.IsNullOrEmpty(doc.Subject)){
        data.Add("Комментарий", doc.Subject);
      }
      var isUpd = btlab.Shiseido.UniversalTransferDocuments.Is(doc);
      data.Add("ЭтоУниверсальныйДокумент", isUpd); 
      
      var closingDocResult = CreateDocIn1c("ПоступлениеТоваровУслуг", data);
      var responseString = closingDocResult.Content.ReadAsStringAsync().Result;
      if(closingDocResult.StatusCode != HttpStatusCode.Created){
        Logger.Debug($"status={closingDocResult.StatusCode} resp={responseString}");
        return;
      }
      SetDocumentWasExportedTo1c(doc);
      var result = GetDeserializeData(responseString);
      var docRef = result["Ref_Key"] as string;
      
      data = new Dictionary<string,object>();
      data.Add("Статус", "ОригиналПолучен");
      data.Add("Статус_Type", "StandardODATA.СтатусыДокументовПоступления");
      //var orgRef = btlab.Shiseido.BusinessUnits.As(doc.BusinessUnit)?.Id1c;
      //if(string.IsNullOrEmpty(orgRef)){
      //  Logger.Debug($"Не удалось получить нашу организацию");
      //}
      var docType = "StandardODATA.Document_ПоступлениеТоваровУслуг";
      var statusDocResult = UpdateObjIn1c("InformationRegister_СтатусыДокументов", ourUnitRef, docRef, docType, data);
      responseString = statusDocResult.Content.ReadAsStringAsync().Result;
      if (statusDocResult.StatusCode != HttpStatusCode.OK)
      {
        Logger.Debug($"status={statusDocResult.StatusCode} resp={responseString}");
        return;
      }
      
      Logger.Debug("isUpd="+isUpd);
      var isAct = btlab.Shiseido.ContractStatements.Is(doc);
      if(!isAct){
        return;
      }
      
      
      if(string.IsNullOrEmpty(docRef)){
        Logger.Debug("Не удалось получить Ref созданного документа");
        return;
      }else{
        Logger.Debug($"Ref созданного документа: {docRef}");
      }
      
      var relatedFromDocs = doc.Relations.GetRelatedFrom().ToArray();
      var allRelatedDocs = new List<Sungero.Content.IElectronicDocument>(relatedFromDocs);
      foreach(var r in relatedFromDocs){
        allRelatedDocs.AddRange(r.Relations.GetRelated().ToArray());
      }
      var docs = allRelatedDocs
        .Where(r => Sungero.FinancialArchive.IncomingTaxInvoices.Is(r))
        .ToArray();
      
      if(docs.Length < 1){
        Logger.Debug($"Нет счет-фактур в связях");
        return;
      }
      Logger.Debug($"Берем счет-фактуру={docs[0].Id}");
      var incInv = Sungero.FinancialArchive.IncomingTaxInvoices.As(docs[0]);
      
      data = new Dictionary<string,object>();
      if(!string.IsNullOrEmpty(incInv.RegistrationNumber) && incInv.RegistrationDate.HasValue){
        var regNum = incInv.RegistrationNumber;
        //var regNum = "T"+Calendar.Now.ToString("HHmmss");
        //data.Add("Number", regNum);
        data.Add("Date", incInv.RegistrationDate.Value.ToString(formatDate));
        data.Add("НомерВходящегоДокумента", regNum);
        data.Add("ДатаВходящегоДокумента", incInv.RegistrationDate.Value.ToString(formatDate));
      }else{
        Logger.Debug($"Нет регистрационного номера/даты в счет-фактуре");
      }
      
      if(!string.IsNullOrEmpty(contractorRef)){
        data.Add("Контрагент@odata.bind", $"Catalog_Контрагенты(guid'{contractorRef}')");
      }
      data.Add("ДокументОснование", $"{docRef}");
      data.Add("ДокументОснование_Type", "StandardODATA.Document_ПоступлениеТоваровУслуг");
      
      var incInvResult = CreateDocIn1c("СчетФактураПолученный", data);
      responseString = incInvResult.Content.ReadAsStringAsync().Result;
      if(incInvResult.StatusCode != HttpStatusCode.Created){
        Logger.Debug($"status={incInvResult.StatusCode} resp={responseString}");
        return;
      }      
    }
    #endregion

    #region Передача документов Directum (Входящий счет) -> 1C (ПлатежноеПоручение)
    /// <summary>
    /// 
    /// </summary>
    public virtual void IncomingInvoiceJob()
    {
     Logger.Debug($"========= IncomingInvoiceJob: {Calendar.Now} =========");
     try{
         var incInvDocs = btlab.Shiseido.IncomingInvoices.GetAll()
           .ToArray();
         foreach(var doc in incInvDocs){
           Logger.Debug($"=== Входящий счет: {doc.Id} ===");
           if(doc.Synhronyse1C.HasValue && doc.Synhronyse1C.Value == true){
             if(!doc.WasExportedTo1c.HasValue || doc.WasExportedTo1c.Value == false){
                if(!DocIsLocked(doc)){
                  ExportIncomingInvoiceDocTo1c(doc);
                }else{
                  Logger.Debug($"Документ заблокирован");
                }
             }else{
                Logger.Debug($"Уже был экспортирован в 1с={doc.WasExportedTo1c}");
             }
           }else{
             Logger.Debug($"Синхронизировать с 1с={doc.Synhronyse1C}");
           }
         }
        
     }catch(Exception e){
       Logger.Debug($"IncomingInvoiceJob: err= {e.Message+Environment.NewLine+e.StackTrace}");
     }
    
    }
    
    private void ExportIncomingInvoiceDocTo1c(btlab.Shiseido.IIncomingInvoice doc){
      var formatDate = Constants.Module.OneC_DateFormat;
      var contractor = Sungero.Parties.Companies.As(doc.Counterparty);
         
       var inn = contractor?.TIN;
       var trrc = contractor?.TRRC;
       var bankAccNum = doc.Number;
       var amount = doc.TotalAmount;
       var nds = GetRightNds(doc.NdsLink?.NdsValue);
       var subject = doc.Subject;
       //var docRegDate = doc.RegistrationDate;
       //var docRegNum = doc.RegistrationNumber;
       //var ourUnitRef = btlab.Shiseido.BusinessUnits.As(doc.BusinessUnit)?.Id1c;
       var ourUnitRef = Constants.Module.OneC_ShiseidoOrgRef;
      
       if(string.IsNullOrEmpty(inn) || string.IsNullOrEmpty(trrc) || string.IsNullOrEmpty(bankAccNum)){
         Logger.Debug($"Не все рекомендованные данные заполнены: inn={inn}, trrc={trrc}, bankAccNum={bankAccNum}.");
         return;
       }
  
       var filterData = new Dictionary<string, string> {
         {"ИНН", inn},
         {"КПП", trrc}
       };
       var contractorData = GetObjData("Catalog_Контрагенты", filterData);
       var contractorRef = GetValue(contractorData, "Ref_Key");
       if(string.IsNullOrEmpty(contractorRef))
       {
         Logger.Debug($"Неправильный Ref контрагента: {contractorRef}");
         return;
       }
       if(!string.IsNullOrEmpty(ourUnitRef)){
         Logger.Debug($"Не удалось получить нашу организацию");
       }
       filterData = new Dictionary<string, string> {
         {"НомерСчета", bankAccNum}
       };
       var addFilterData = new Dictionary<string, string> {
         {"Owner", ourUnitRef}
       };
       var bankData = GetObjData("Catalog_БанковскиеСчета", filterData, addFilterData);
       var bankAccRef = GetValue(bankData, "Ref_Key");
       if(string.IsNullOrEmpty(bankAccRef)){
         Logger.Debug($"Неправильный банковский счет: {bankAccRef}");
       }
       
       string contractRef = null;
       if(doc.Contract != null){
          var coContractor = Sungero.Parties.Companies.As(doc.Contract.Counterparty);
          var coInn = coContractor.TIN;
          var coTrrc = coContractor.TRRC;
          var coRegNum = doc.Contract.RegistrationNumber;
          var coRegDate = doc.Contract.RegistrationDate;
          if(!coRegDate.HasValue || string.IsNullOrEmpty(coRegNum) || string.IsNullOrEmpty(coInn) || string.IsNullOrEmpty(coTrrc)){
            Logger.Debug("Присутствуют не все необходимые, для поиска договора, данные:");
            Logger.Debug($"coInn={coInn}, coTrrc={coTrrc}, coRegNum={coRegNum}, coRegDate={coRegDate}");
          } else {
              string coContractorRef;
              if(coInn!=inn || coTrrc!=trrc){
                filterData = new Dictionary<string, string> {
                  {"ИНН", coInn},
                  {"КПП", coTrrc}
                };
                var coContractorData = GetObjData("Catalog_Контрагенты", filterData);
                coContractorRef = GetValue(coContractorData, "Ref_Key");
              }else{
                coContractorRef = contractorRef;
              }
              filterData = new Dictionary<string, string> {
                {"Номер", coRegNum},
                {"Дата", coRegDate.Value.ToString(formatDate)},
                {"Owner_Key", coContractorRef}
              };
              var contractData = GetObjData("Catalog_ДоговорыКонтрагентов", filterData);
              contractRef = GetValue(contractData, "Ref_Key");
            }
        }
      
       var data = new Dictionary<string,object>();
       data.Add("ВидОперации", "ОплатаПоставщику");
       data.Add("Контрагент", contractorRef);
       data.Add("Контрагент_Type", "StandardODATA.Catalog_Контрагенты");
       //data.Add("Организация@odata.bind", "Catalog_Организации(guid'b7befbac-a735-44a9-8de3-18e236cda461')");
       if(!string.IsNullOrEmpty(ourUnitRef)){
         data.Add("Организация@odata.bind", $"Catalog_Организации(guid'{ourUnitRef}')");//"8a276db6-ce58-11e5-982d-14dae9b19a48"
         data.Add("Организация_Type", "StandardODATA.Catalog_Организации");
       }
       if(!string.IsNullOrEmpty(bankAccRef)){
         data.Add("СчетОрганизации@odata.bind", $"Catalog_БанковскиеСчета(guid'{bankAccRef}')");
       }
       if(!string.IsNullOrEmpty(contractRef)){
         data.Add("ДоговорКонтрагента@odata.bind", $"Catalog_ДоговорыКонтрагентов(guid'{contractRef}')");
       }
       if(amount.HasValue){
         data.Add("СуммаДокумента", $"{amount.Value.ToString()}");
       }
       if(!string.IsNullOrEmpty(nds)){
         data.Add("СтавкаНДС", nds);
       }
       if(!string.IsNullOrEmpty(subject)){
         data.Add("НазначениеПлатежа", subject);
       }
       var tasks = new List<Sungero.Docflow.IFreeApprovalTask>();
       foreach(var t in Sungero.Docflow.FreeApprovalTasks.GetAll()){
         if(t.Attachments.Any(a => a.Id==doc.Id)){
           tasks.Add(t);
         }
       }
       if(tasks.Count > 0){
         var approverFios = tasks[0].Approvers.Select(a => a.Approver.Name).ToArray();
         var fio1 = approverFios.Length > 0 ?approverFios[0] :null;
         var fio2 = approverFios.Length > 1 ?approverFios[1] :null;
         
         if(!string.IsNullOrEmpty(fio1)){
           filterData = new Dictionary<string, string> {
             {"ФИО", fio1}
           };
           var fizLic1Data = GetObjData("Catalog_ФизическиеЛица", filterData);
           var fizLic1Ref = GetValue(fizLic1Data, "Ref_Key");
           if(!string.IsNullOrEmpty(fizLic1Ref)){
            data.Add("Согласовано1_Key",fizLic1Ref);
           }
         }
         if(!string.IsNullOrEmpty(fio2)){
           filterData = new Dictionary<string, string> {
             {"ФИО", fio2}
           };
           var fizLic2Data = GetObjData("Catalog_ФизическиеЛица", filterData);
           var fizLic2Ref = GetValue(fizLic2Data, "Ref_Key");
           if(!string.IsNullOrEmpty(fizLic2Ref)){
            data.Add("Согласовано2_Key",fizLic2Ref);
           }
         }
       }
       
       var json = JsonConvert.SerializeObject(data);
       //Logger.Debug("requestBody="+json);
       var incInvResult = CreateDocIn1c("ПлатежноеПоручение", data);
       var responseString = incInvResult.Content.ReadAsStringAsync().Result;
       if(incInvResult.StatusCode != HttpStatusCode.Created){
         Logger.Debug($"status={incInvResult.StatusCode} resp={responseString}");
         return;
       }
       SetDocumentWasExportedTo1c(doc);
       var result = GetDeserializeData(responseString);
       var docRef = result["Ref_Key"] as string;
       if(string.IsNullOrEmpty(docRef)){
         Logger.Debug("Не удалось получить Ref созданного документа");
         return;
       }else{
         Logger.Debug($"Ref созданного документа: {docRef}");
       }
       ConductIt("ПлатежноеПоручение", docRef);
    }
    #endregion
    
    #region Общие функции
    private string GetContractorRefFrom1c(Sungero.Parties.ICounterparty counterparty){
      var contractor = Sungero.Parties.Companies.As(counterparty);
      var inn = contractor?.TIN;
      var trrc = contractor?.TRRC;
      if(string.IsNullOrEmpty(inn) || string.IsNullOrEmpty(trrc)){
        return null;
      }
      var filterData = new Dictionary<string, string> {
        {"ИНН", inn},
        {"КПП", trrc}
      };
      var coContractorData = GetObjData("Catalog_Контрагенты", filterData);
      return GetValue(coContractorData, "Ref_Key");
    }
    
    private string GetContractRefFrom1c(Sungero.Docflow.IContractualDocumentBase contract){
      if(contract == null){
        return null;
      }
      var contractorRef = GetContractorRefFrom1c(contract.Counterparty);
      var regNum = contract.RegistrationNumber;
      var regDate = contract.RegistrationDate;
      if(string.IsNullOrEmpty(regNum) || !regDate.HasValue || string.IsNullOrEmpty(contractorRef)){
        return null;
      }
      var filterData = new Dictionary<string, string> {
        {"Номер", regNum},
        {"Дата", regDate.Value.ToString(Constants.Module.OneC_DateFormat)},
        {"Owner_Key", contractorRef}
      };
      var contractData = GetObjData("Catalog_ДоговорыКонтрагентов", filterData);
      return GetValue(contractData, "Ref_Key");
    }
    
    private string GetRightNds(string nds){
      switch(nds){
        case "20%": return "НДС20";
        case "0%": return "НДС0";
        case "Без НДС": return "БезНДС";
      }
      return null;
    }
    
    private void SetDocumentWasExportedTo1c(btlab.Shiseido.IAccountingDocumentBase docBase){
      if(btlab.Shiseido.IncomingInvoices.Is(docBase)){
        var doc = btlab.Shiseido.IncomingInvoices.As(docBase);
        doc.SkipContract = true;
        var contractControl = doc.State.Properties.Contract;
        contractControl.IsEnabled = contractControl.IsRequired = false;
        doc.WasExportedTo1c = true;
        doc.Save();
      }else{
        docBase.WasExportedTo1c = true;
        docBase.Save();
      }
    }
    
    private bool DocIsLocked(Sungero.Content.IElectronicDocument doc){
      var lockInfo = Locks.GetLockInfo(doc);
      return lockInfo != null && lockInfo.IsLockedByOther;
    }
    
    private string GetRightValue(string key, string value){
      if(key == "Дата"){
        return $"datetime'{value}'";
      }else
      if(key.EndsWith("_Key")){
        return $"guid'{value}'";
      }
      return $"'{value}'";
    }
    
    private Dictionary<string, object> GetDeserializeData(string strData){
      var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(strData);
      if (result == null) 
          return null;
      RecurseDeserialize(result);
      return result;
    }
    
    private Dictionary<string, object> GetObjData(string objType, Dictionary<string, string> filterData, Dictionary<string, string> addFilter = null)
    {
     var filterStr = string.Join(" and ", filterData.Select(pair => $"{pair.Key} eq {GetRightValue(pair.Key, pair.Value)}"));
     var resMsg = GetDataFrom1c(objType, filterStr);
     var resStr = resMsg.Content.ReadAsStringAsync().Result;

     if(resMsg.StatusCode == HttpStatusCode.OK){
       var result = GetDeserializeData(resStr);
       PostFilter(result, addFilter);
       return result;
     }
     return null;
    }
    
    private void ConductIt(string docType, string docRef)
    {
      var odataSetting = new Structures.Module.ODataSetting();
      var url = odataSetting.Data.Url;
      var userName = odataSetting.Data.UserName;
      var pass = odataSetting.Data.Pass;
      
      Logger.Debug("=== ConductIt start "+Calendar.Now+" ===");
      string requestUri = $"{url}Document_{docType}(guid'{docRef}')/Post?PostingModeOperational=false";
      Logger.Debug("requestUri="+requestUri);
      HttpClientHandler handler = new HttpClientHandler(){
        PreAuthenticate = true,
        Credentials = new NetworkCredential(userName, pass)
      };
       HttpClient client = new HttpClient(handler);
       client.DefaultRequestHeaders.Authorization =
           new AuthenticationHeaderValue("Basic",
               Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(userName+":"+pass)));
       client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
       
       var jsonContent = "{}";
       Logger.Debug("jsonContent="+jsonContent);
       var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
       var response = client.PostAsync(requestUri, content);
       var responseString = response.Result.Content.ReadAsStringAsync().Result;
       if (response.Result.StatusCode == HttpStatusCode.OK)
       {
           var responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
       }
       else
       {
         Logger.Debug("status="+response.Result.StatusCode+Environment.NewLine+responseString);
       }
    }

    private void PostFilter(Dictionary<string, object> data, Dictionary<string, string> addFilter = null)
    {
        var dataArr = data["value"] as Dictionary<string, object>[];
        if (dataArr == null || dataArr.Length < 1)
            return;
        data["value"] = dataArr
            .Where(d => d["DeletionMark"] is bool && !(bool) d["DeletionMark"])
            .Where(d => addFilter == null || addFilter.All(f => d[f.Key] != null && (d[f.Key] as string) == f.Value))
            .ToArray();
    }
    
    private string GetValue(Dictionary<string, object> jDict, string key)
    {
      if (jDict == null){
          return null;
      }
       var dictArr = jDict["value"] as Dictionary<string, object>[];
       if (dictArr == null || dictArr.Length < 1){
          return null;
       }

       return dictArr[0][key] as string;
    }
    
    private void RecurseDeserialize(Dictionary<string, object> result)
    {
       foreach (var pair in result.ToArray()) {
           var jArray = pair.Value as JArray;
           if (jArray == null)
               continue;
           var dictArr = JsonConvert.DeserializeObject<Dictionary<string, object>[]>(jArray.ToString());
           result[pair.Key] = dictArr;
           foreach (var dictionary in dictArr) {
               RecurseDeserialize(dictionary);
           }
       }
    }

   
    private HttpResponseMessage GetDataFrom1c(string type, string filter)
    {
      var odataSetting = new Structures.Module.ODataSetting();
      var url = odataSetting.Data.Url;
      var userName = odataSetting.Data.UserName;
      var pass = odataSetting.Data.Pass;

      string requestUri = url + type + "?$format=json";
      if(!string.IsNullOrEmpty(filter)){
        requestUri += "&$filter=" + filter;
      }
      
      HttpClient client = new HttpClient();
      client.DefaultRequestHeaders.Authorization =
          new AuthenticationHeaderValue("Basic",
              Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(userName+":"+pass)));
    
      client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      
      var response = client.GetAsync(requestUri);
      Logger.Debug($"requestUri={requestUri}");
      return response.Result;
    }
    
    private HttpResponseMessage UpdateObjIn1c(string objType, string orgRef, string docRef, string docType, Dictionary<string, object> requestData)
    {
        var odataSetting = new Structures.Module.ODataSetting();
        var url = odataSetting.Data.Url;
        var userName = odataSetting.Data.UserName;
        var pass = odataSetting.Data.Pass;
        Logger.Debug("=== UpdateObjIn1c start "+Calendar.Now+" ===");
        string requestUri = url + objType + $"(Организация_Key=guid'{orgRef}', Документ=guid'{docRef}', Документ_Type='{docType}')";
        Logger.Debug("requestUri="+requestUri);
        HttpClientHandler handler = new HttpClientHandler(){
            PreAuthenticate = true,
            Credentials = new NetworkCredential(userName, pass)
        };
        HttpClient client = new HttpClient(handler);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(userName+":"+pass)));
    
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.BaseAddress = new Uri(requestUri);
         
        var jsonContent = JsonConvert.SerializeObject(requestData);
        Logger.Debug("jsonContent="+jsonContent);
        var httpVerb = new HttpMethod("PATCH");
        var httpRequestMessage = new HttpRequestMessage(httpVerb, requestUri)
        {
            Content = new StringContent(jsonContent)
        };
       
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        var response = client.SendAsync(httpRequestMessage);
        return response.Result;
    }
  
    private HttpResponseMessage CreateDocIn1c(string docType, Dictionary<string, object> requestData)
    {
      var odataSetting = new Structures.Module.ODataSetting();
      var url = odataSetting.Data.Url;
      var userName = odataSetting.Data.UserName;
      var pass = odataSetting.Data.Pass;
      
      Logger.Debug("=== CreateDocIn1c start "+Calendar.Now+" ===");
      string requestUri = url + "Document_" + docType + "?$format=json";
      Logger.Debug("requestUri="+requestUri);
      HttpClientHandler handler = new HttpClientHandler(){
        PreAuthenticate = true,
        Credentials = new NetworkCredential(userName, pass)
      };
       HttpClient client = new HttpClient(handler);
       client.DefaultRequestHeaders.Authorization =
           new AuthenticationHeaderValue("Basic",
               Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(userName+":"+pass)));
    
       client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      
       
       var jsonContent = JsonConvert.SerializeObject(requestData);
       Logger.Debug("jsonContent="+jsonContent);
       var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
       var response = client.PostAsync(requestUri, content);
       return response.Result;
    }
    #endregion

    #endregion
  }
}