using System;
using Sungero.Core;

namespace btlab.IntegrationWith1c.Constants
{
  public static class Module
  {

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
      public const string TIN = "ПолучательИНН";
      
      /// <summary>
      /// Название свойства КонецДокумента
      /// </summary>
      public const string EndDocument = "КонецДокумента";
    }

  }
}