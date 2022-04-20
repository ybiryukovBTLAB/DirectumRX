using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using btlab.Shiseido.IncomingInvoice;

namespace btlab.Shiseido.Shared
{
  partial class IncomingInvoiceFunctions
  {    
    public override void FillName()
    {
      var name = "СЧ ";
      
      if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
        name += Sungero.Docflow.OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
      if (_obj.RegistrationDate != null)
        name += Sungero.Docflow.OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
      if (string.IsNullOrWhiteSpace(name))
        name = "СЧ ";

      _obj.Name = Sungero.Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
    }

  }
}