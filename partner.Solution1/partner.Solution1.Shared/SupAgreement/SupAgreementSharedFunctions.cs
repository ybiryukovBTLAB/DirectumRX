using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using partner.Solution1.SupAgreement;

namespace partner.Solution1.Shared
{
  partial class SupAgreementFunctions
  {

    /// <summary>
    /// 
    /// </summary>       
public override void FillName()
    {
      var name = "ДC ";
        {
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += Sungero.Docflow.OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (_obj.RegistrationDate != null)
          name += Sungero.Docflow.OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
        
        
      }
      if (string.IsNullOrWhiteSpace(name))

    name = "ДC ";

  

  _obj.Name = Sungero.Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
    }
}
}