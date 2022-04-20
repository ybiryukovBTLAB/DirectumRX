using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using btlab.Shiseido.ExchangeDocument;

namespace btlab.Shiseido.Server
{
  partial class ExchangeDocumentFunctions
  {
    /// <summary>
    /// Сменить тип документа.
    /// </summary>
    /// <param name="type">Тип документа, на который нужно сменить.</param>
    /// <returns>Сконвертированный документ.</returns>
    [Public]
    public Sungero.Docflow.IOfficialDocument ChangeDocumentType(Sungero.Domain.Shared.IEntityInfo type)
    {
      Sungero.Docflow.IOfficialDocument convertedDoc = null;
      
      // Запретить смену типа, если документ или его тело заблокировано.
      var isCalledByDocument = CallContext.CalledDirectlyFrom(Sungero.Docflow.OfficialDocuments.Info);
      var lockInfo = Sungero.Core.Locks.GetLockInfo(_obj);
      if (isCalledByDocument && lockInfo.IsLockedByOther ||
          !isCalledByDocument && lockInfo.IsLocked ||
          Sungero.Core.Locks.GetLockInfo(_obj.LastVersion.Body).IsLocked)
      {
        return convertedDoc;
      }
      
     
      convertedDoc = Sungero.Docflow.OfficialDocuments.As(_obj.ConvertTo(type));
      return convertedDoc;
    }
  }
}