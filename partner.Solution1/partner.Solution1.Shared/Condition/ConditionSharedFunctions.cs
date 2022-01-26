using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using partner.Solution1.Condition;

namespace partner.Solution1.Shared
{
  partial class ConditionFunctions
  {

    public override System.Collections.Generic.Dictionary<string, List<Enumeration?>> GetSupportedConditions()
    {
      var baseSupport = base.GetSupportedConditions();
      baseSupport["a523a263-bc00-40f9-810d-f582bae2205d"].Add(ConditionType.ExpensesType);
      return baseSupport;
    }
    
    public override Sungero.Docflow.Structures.ConditionBase.ConditionResult CheckCondition(Sungero.Docflow.IOfficialDocument document, Sungero.Docflow.IApprovalTask task)
    {
      if (_obj.ConditionType == ConditionType.ExpensesType)
      {
        var incomingInvoice = partner.Solution1.IncomingInvoices.As(document);
        if (incomingInvoice != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(incomingInvoice.ExpensesTypepartner == _obj.ExpensesTypepartner, string.Empty);
        else
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, "Условие не может быть вычислено. Отправляемый документ не того вида.");
        
      }
    
      return base.CheckCondition(document, task);
    
    }
    
    public override void ChangePropertiesAccess()
    {
      base.ChangePropertiesAccess();
    
      var isExpensesType = _obj.ConditionType == ConditionType.ExpensesType;
      _obj.State.Properties.ExpensesTypepartner.IsVisible = isExpensesType;
      _obj.State.Properties.ExpensesTypepartner.IsRequired = isExpensesType;
    }
    
     
    
    public override void ClearHiddenProperties()
    {
      base.ClearHiddenProperties();
      
      if (!_obj.State.Properties.ExpensesTypepartner.IsVisible)
        _obj.ExpensesTypepartner = null;
    
    }
    
  }
}