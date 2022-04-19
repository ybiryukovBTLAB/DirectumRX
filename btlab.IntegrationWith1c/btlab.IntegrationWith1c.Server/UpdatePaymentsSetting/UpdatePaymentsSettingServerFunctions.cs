using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using btlab.IntegrationWith1c.UpdatePaymentsSetting;

namespace btlab.IntegrationWith1c.Server
{
  partial class UpdatePaymentsSettingFunctions
  {
    
    [Remote]
    public bool Check()
    {
      if (_obj != null && !string.IsNullOrEmpty(_obj.RootFolder) && Directory.Exists(_obj.RootFolder)
          && !string.IsNullOrEmpty(_obj.ProcessingFolder) && !string.IsNullOrEmpty(_obj.ProcessedFolder)
          && !string.IsNullOrEmpty(_obj.ErrorFolder) && !string.IsNullOrEmpty(_obj.ArchiveFolder)
         ) {
        var processingFolderPath = Functions.UpdatePaymentsSetting.ProcessingPath(_obj);
        if (!Directory.Exists(processingFolderPath)) {
          Directory.CreateDirectory(processingFolderPath);
        }
        var processedFolderPath = Functions.UpdatePaymentsSetting.ProcessedPath(_obj);
        if (!Directory.Exists(processedFolderPath)) {
          Directory.CreateDirectory(processedFolderPath);
        }
        var errorFolderPath = Functions.UpdatePaymentsSetting.ErrorPath(_obj);
        if (!Directory.Exists(errorFolderPath)) {
          Directory.CreateDirectory(errorFolderPath);
        }
        var archiveFolderPath = Functions.UpdatePaymentsSetting.ArchivePath(_obj);
        if (!Directory.Exists(archiveFolderPath)) {
          Directory.CreateDirectory(archiveFolderPath);
        }
        return true;
      }
      return false;
    }
    
  }
}