using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Contracts;
using partner.Solution1.IncomingInvoice;

namespace partner.Solution1
{
  partial class IncomingInvoiceSharedHandlers
  {

    public override void ContractChanged(Sungero.Contracts.Shared.IncomingInvoiceContractChangedEventArgs e)
    {
      base.ContractChanged(e);
      
      var contractSelected = e.NewValue != null;
      _obj.State.Properties.ContractStatepartner.IsVisible = contractSelected;
    }
    
    private string GetContractState(IContractualDocument contract)
    {
      
      var appTasks = Sungero.Docflow.PublicFunctions.Module.Remote.GetApprovalTasks(contract);
      if(appTasks.Any())
      {
        var status = appTasks.First().Status;
        var result = GetStatusStr(status.Value);
        //var statuses = appTasks.Select(x => GetStatusStr(x.Status)).ToList();
        return result;//string.Join(", ", statuses);
      }
      return "";
    }
    
    private string GetStatusStr(Enumeration? status)
    {
      var result = "";
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