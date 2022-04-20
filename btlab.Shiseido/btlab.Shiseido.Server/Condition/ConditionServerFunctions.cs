using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using btlab.Shiseido.Condition;

namespace btlab.Shiseido.Server
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
                 (_obj.ExpensesType==btlab.Shiseido.IncomingInvoice.ExpensesType.Com ?"Коммерческие" :"Административные"));
        }
        return base.GetConditionName();
    }
  }
}