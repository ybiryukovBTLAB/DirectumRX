using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using btlab.IntegrationWith1c.UpdatePaymentsSetting;

namespace btlab.IntegrationWith1c.Shared
{
  partial class UpdatePaymentsSettingFunctions
  {

    /// <summary>
    /// Путь к папке на обработку
    /// </summary>
    /// <returns>Путь</returns>
    [Public]
    public string ProcessingPath()
    {
      return _obj.RootFolder + "\\" + _obj.ProcessingFolder;
    }
    
    /// <summary>
    /// Путь к папке с обработанными записями
    /// </summary>
    /// <returns>Путь</returns>
    [Public]
    public string ProcessedPath()
    {
      return _obj.RootFolder + "\\" + _obj.ProcessedFolder;
    }
    
    /// <summary>
    /// Путь к папке ошибок
    /// </summary>
    /// <returns>Путь</returns>
    [Public]
    public string ErrorPath()
    {
      return _obj.RootFolder + "\\" + _obj.ErrorFolder;
    }
    
    /// <summary>
    /// Путь к архивной папке
    /// </summary>
    /// <returns>Путь</returns>
    [Public]
    public string ArchivePath()
    {
      return _obj.RootFolder + "\\" + _obj.ArchiveFolder;
    }

  }
}