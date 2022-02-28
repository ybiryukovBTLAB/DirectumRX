using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using partner.Solution1.Contract;

namespace partner.Solution1
{
  partial class ContractSharedHandlers
  {

    public override void RegistrationDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      FillName();
    }

    public override void RegistrationNumberChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
     FillName();
    }

  }
}