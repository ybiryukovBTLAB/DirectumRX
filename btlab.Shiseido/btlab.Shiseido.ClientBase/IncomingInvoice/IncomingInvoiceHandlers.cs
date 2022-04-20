using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using btlab.Shiseido.IncomingInvoice;

namespace btlab.Shiseido
{
  partial class IncomingInvoiceClientHandlers
  {

    public virtual void SkipContractValueInput(Sungero.Presentation.BooleanValueInputEventArgs e)
    {
      var contractControl = _obj.State.Properties.Contract;
      var skipContract = e.NewValue.HasValue && e.NewValue.Value;
    
      contractControl.IsEnabled = contractControl.IsRequired = !skipContract;
      if(skipContract)
        _obj.Contract = null;
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);
      var contractSelected = _obj.Contract != null;
      if(contractSelected)
      {
        if(_obj.ContractState == null)
        {
          var appTasks = Sungero.Docflow.PublicFunctions.Module.Remote.GetApprovalTasks(_obj.Contract);
          _obj.ContractState = appTasks.Any() ?GetStatusStr(appTasks.First().Status) :null;
        }
        _obj.State.Properties.ContractState.IsVisible = true;
      }
      else
      {
        _obj.State.Properties.ContractState.IsVisible = false;
      }
    }
    
    /*
    private string GetContractState(Sungero.Docflow.ContractualDocument contract)
    {
      
      var appTasks = Sungero.Docflow.PublicFunctions.Module.Remote.GetApprovalTasks(contract);
      if(appTasks.Any())
      {
        var status = appTasks.First().Status;
        var result = GetStatusStr(status.Value);
        return result;
      }
      return "";
    }
    */
    
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