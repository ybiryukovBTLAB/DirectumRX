using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using btlab.HelpDesk.Request;

namespace btlab.HelpDesk
{
  partial class RequestServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.LifeCycle == LifeCycle.Closed &&  string.IsNullOrEmpty(_obj.Result))
      {
        e.AddError("Заполните результат");
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
        _obj.CreatedDate = Calendar.Today;
    }
  }

}