using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using partner.Solution1.ApprovalRule;

namespace partner.Solution1.Server
{
  partial class ApprovalRuleFunctions
  {
    /*
    public override bool CheckRoutePossibility(List<Structures.ApprovalRuleBase.RouteStep> route, List<Structures.ApprovalRuleBase.ConditionRouteStep> ruleConditions, Structures.ApprovalRuleBase.RouteStep conditionStep)
    {
      // Получить список допустимых веток по базовой логике.
      var possibleStage = base.CheckRoutePossibility(route, ruleConditions, conditionStep);
      var conditionType = _obj.Conditions.First(c => c.Number == conditionStep.StepNumber).Condition.ConditionType;
      
      // Проверить условия по виду закупки.
      if (conditionType == DEV.DevelopmentExample.ContractCondition.)
      {
        // Получить все условия по виду закупки из ветки.
        var purchaseKindConditions = this.GetPurchaseKindConditionsInRoute(route).Where(c => c.StepNumber != conditionStep.StepNumber).ToList();
    
        // Определить допустимость ветки с точки зрения условий по виду закупки.
        possibleStage = this.CheckPurchaseKindConditions(purchaseKindConditions, conditionStep);
      }
      return possibleStage;
    }
    
    public List<Structures.ApprovalRuleBase.RouteStep> GetPurchaseKindConditionsInRoute(List<Structures.ApprovalRuleBase.RouteStep> route)
    {
      return route.Where(e => _obj.Conditions.Any(c => Equals(c.Number, e.StepNumber) && c.Condition.ConditionType == DEV.DevelopmentExample.ContractCondition.ConditionType.PurchaseKind)).ToList();
    }
    
    public bool CheckPurchaseKindConditions(List<Sungero.Docflow.Structures.ApprovalRuleBase.RouteStep> allConditions, Sungero.Docflow.Structures.ApprovalRuleBase.RouteStep condition)
    {
      var conditionItem = _obj.Conditions.Where(x => x.Number == condition.StepNumber).FirstOrDefault();
      var contractCondition = DEV.DevelopmentExample.ContractConditions.As(conditionItem.Condition); 
      var purchaseKind = contractCondition.PurchaseKind;
    
      // Проверить непротиворечивость условия с предыдущими условиями этого же типа в ветке.
      foreach (var previousCondition in allConditions.TakeWhile(x => !Equals(x, condition)))
      {
        var previousConditionItem = _obj.Conditions.Where(x => x.Number == previousCondition.StepNumber).FirstOrDefault();
        var previousContractCondition = DEV.DevelopmentExample.ContractConditions.As(previousConditionItem.Condition);
        var previousPurchaseKind = previousContractCondition.PurchaseKind;
    
        // Если вид закупки в предыдущем условии такой же, а переход отличается – ветка не валидна.
        if (purchaseKind == previousPurchaseKind && previousCondition.Branch != condition.Branch)
          return false;
      }
      return true;
    }
    */
  }
}