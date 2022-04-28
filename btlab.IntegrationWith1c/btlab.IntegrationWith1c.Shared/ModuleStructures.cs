using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace btlab.IntegrationWith1c.Structures.Module
{

  /// <summary>
  /// Настройки OData
  /// </summary>
  [Public]
  partial class ODataSetting
  {    
    public IOdataSetting Data {
      get{
        var odataSettingArr = btlab.IntegrationWith1c.OdataSettings.GetAllCached().ToArray();
        if(odataSettingArr.Length > 0){
          return odataSettingArr[0];
        }else{
          Logger.Debug("Не удалось получить настройки Odata из одноименного справочника.");
          return null;
        }
      }
    }
  }

  /// <summary>
  /// Информация о платеже
  /// </summary>
  [Public]
  partial class PaymentInfo
  {
    // Номер
    public string Number { get; set; }
    // Дата
    public DateTime Date { get; set; }
    // Получатель
    public string Recipient { get; set; }
    // КПП
    public string TRRC { get; set; }
    //ИНН
    public string TIN { get; set; }
  }

}