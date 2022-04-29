using System;
using Sungero.Core;

namespace btlab.IntegrationWith1c.Constants
{
  public static class Module
  {

    /// <summary>
    /// Ref Организации Шисейдо в 1С
    /// </summary>
    public const string OneC_ShiseidoOrgRef = "b7befbac-a735-44a9-8de3-18e236cda461";
    /// <summary>
    /// Ref Общего склада в 1С
    /// </summary>
    public const string OneC_CommonStorageRef = "b7befbac-a735-44a9-8de3-18e236cda461";
    /// <summary>
    /// Формат даты при передаче в 1С
    /// </summary>
    public const string OneC_DateFormat = "yyyy-MM-ddT00:00:00";
    /// <summary>
    /// Путь к своему логу
    /// </summary>
    public const string LogPath = "C:\\log\\log.txt";
    
    /// <summary>
    /// Формат xml
    /// </summary>
    public const string XMLFileFormat = "*.xml";
    
    public static class PaymentInfoProperties {
      
      /// <summary>
      /// Название свойства СекцияДокумент
      /// </summary>
      public const string StartDocument = "СекцияДокумент";
      
      /// <summary>
      /// Название свойства Назначение платежа
      /// </summary>
      public const string PaymentPurpose = "НазначениеПлатежа=";
      
      /// <summary>
      /// Название свойства Номер платежа
      /// </summary>
      public const string Number = "Номер=";
      
      /// <summary>
      /// Название свойства Дата платежа
      /// </summary>
      public const string Date = "Дата=";
      
      /// <summary>
      /// Название свойства Получатель
      /// </summary>
      public const string Recipient = "Получатель=";
      
      /// <summary>
      /// Название свойства КПП
      /// </summary>
      public const string TRRC = "ПолучательКПП=";
      
      /// <summary>
      /// Название свойства ИНН
      /// </summary>
      public const string TIN = "ПолучательИНН=";
      
      /// <summary>
      /// Название свойства КонецДокумента
      /// </summary>
      public const string EndDocument = "КонецДокумента";
    }

  }
}