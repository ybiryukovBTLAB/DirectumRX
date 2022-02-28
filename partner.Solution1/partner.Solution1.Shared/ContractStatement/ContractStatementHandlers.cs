using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using partner.Solution1.ContractStatement;

namespace partner.Solution1
{
  partial class ContractStatementSharedHandlers
  {

    public override void RegistrationNumberChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      FillName();
    }

    public override void RegistrationDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      FillName();
    }

  }
}