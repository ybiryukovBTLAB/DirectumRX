using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using partner.Solution1.Condition;

namespace partner.Solution1.Server
{
  partial class ConditionFunctions
  {
    public override string GetConditionName()
    {
        using (TenantInfo.Culture.SwitchTo())
        {
            // значение ресурса PurchaseKindNameFormat – "Вид закупки – {0}?"
            if (_obj.ConditionType == ConditionType.ExpensesType)
              return string.Format("Вид закупки - {0}", 
                 (_obj.ExpensesTypepartner==partner.Solution1.IncomingInvoice.ExpensesTypepartner.com ?"Коммерческие" :"Административные"));
        }
        return base.GetConditionName();
    }
  }
}