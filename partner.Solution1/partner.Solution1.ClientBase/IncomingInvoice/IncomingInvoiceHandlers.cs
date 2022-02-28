using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using partner.Solution1.IncomingInvoice;

namespace partner.Solution1
{
  partial class IncomingInvoiceClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);
      var contractSelected = _obj.Contract != null;
      if(contractSelected)
      {
        if(_obj.ContractStatepartner == null)
        {
          var appTasks = Sungero.Docflow.PublicFunctions.Module.Remote.GetApprovalTasks(_obj.Contract);
          _obj.ContractStatepartner = appTasks.Any() ?GetStatusStr(appTasks.First().Status) :null;
        }
        _obj.State.Properties.ContractStatepartner.IsVisible = true;
      }
      else
      {
        _obj.State.Properties.ContractStatepartner.IsVisible = false;
      }
    }

    
    public virtual void SkipContractpartnerValueInput(Sungero.Presentation.BooleanValueInputEventArgs e)
    {
      var contractControl = _obj.State.Properties.Contract;
      var skipContract = e.NewValue.HasValue && e.NewValue.Value;
    
      contractControl.IsEnabled = contractControl.IsRequired = !skipContract;
      if(skipContract)
        _obj.Contract = null;
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