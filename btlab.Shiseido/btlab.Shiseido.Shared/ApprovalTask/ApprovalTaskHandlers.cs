using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using btlab.Shiseido.ApprovalTask;

namespace btlab.Shiseido
{
  partial class ApprovalTaskSharedHandlers
  {

    public override void StatusChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      base.StatusChanged(e);
      var doc = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if(btlab.Shiseido.IncomingInvoices.Is(doc))
      {
        var contract = btlab.Shiseido.IncomingInvoices.As(doc);
        contract.ContractApprovalStatus = GetStatusStr(_obj.Status.Value);
      }
    }
    
    private string GetStatusStr(Enumeration? status)
    {
      string result = null;
      if(status.HasValue)
        if(status.Value == Sungero.Workflow.Task.Status.InProcess)
          result = Sungero.Docflow.ApprovalTasks.Resources.StateViewInProcess;//"В работе";
        else if (status.Value == Sungero.Workflow.Task.Status.Completed)
          result = Sungero.Docflow.ApprovalTasks.Resources.StateViewCompleted;//"Завершен";
        else if (status.Value == Sungero.Workflow.Task.Status.Aborted)
          result = Sungero.Docflow.ApprovalTasks.Resources.StateViewAborted;//"Отменён";
        else if (status.Value == Sungero.Workflow.Task.Status.Suspended)
          result = Sungero.Docflow.ApprovalTasks.Resources.StateViewSuspended;//"Приостановлен";
        else if (status.Value == Sungero.Workflow.Task.Status.Draft)
          result = Sungero.Docflow.ApprovalTasks.Resources.StateViewDraft;//"Черновик";
        else if (status.Value == Sungero.Workflow.Task.Status.UnderReview)
          result = Sungero.Docflow.ApprovalTasks.Resources.StateViewReview;//"На рассмотрении";
      return result;
    }

  }
}