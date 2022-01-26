using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using partner.Solution1.ContractBase;

namespace partner.Solution1.Client
{
  partial class ContractBaseActions
  {
    public override void Save(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.Save(e);
    }

    public override bool CanSave(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSave(e);
    }

  }

}