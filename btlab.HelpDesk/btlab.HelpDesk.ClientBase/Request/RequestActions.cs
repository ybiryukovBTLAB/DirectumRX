using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using btlab.HelpDesk.Request;

namespace btlab.HelpDesk.Client
{
  partial class RequestActions
  {
    public virtual void ShowEmployeeRequest(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Request.Remote.GetEmployeeRequests(_obj).Show();
    }

    public virtual bool CanShowEmployeeRequest(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void OpenRequest(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      _obj.LifeCycle = LifeCycle.InWork;
      _obj.State.IsEnabled = true;
      _obj.ClosedDate = null;
    }

    public virtual bool CanOpenRequest(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.LifeCycle.Equals(LifeCycle.Closed);
    }

  }

}