using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Sungero.Core;
using Sungero.CoreEntities;

namespace btlab.DiadocIntegration.Server
{
  public class ModuleJobs
  {



    /// <summary>
    /// 
    /// </summary>
    public virtual void RedirectDocsFromDiadocJob()
    {
      try{
        Logger.Debug("========= RedirectDocsFromDiadocJob: "+Calendar.Now+" =========");
        var allEmpls = Sungero.Company.Employees.GetAll();
        var boxManager = allEmpls.Where(e => e.Id == 48).FirstOrDefault();
        var assignee = allEmpls.Where(e => e.Id == 54).FirstOrDefault();
        if(assignee == null){
          Logger.Debug("Не удалось получить изначального адресата писем диадок по его ID=54.");
        }
        if(boxManager == null){
          Logger.Debug("Не удалось получить ответственного за ящик по его ID=54.");
       }
        var tasks = btlab.Shiseido.ExchangeDocumentProcessingAssignments.GetAll()
            .Where(t => t.Allocated == null || t.Allocated == false)
            .Where(t => t.Status == btlab.Shiseido.ExchangeDocumentProcessingAssignment.Status.InProcess)
            .Where(t => assignee.Id == t.Performer.Id)
            .ToArray();
        for(var i=0; i<tasks.Length; i++)
        {
          var task = tasks[i];
          var docs = GetAllAttachments(task)
            .ToArray();
          
         if(docs.Length == 0)
         {
            Logger.Debug("Нет документов");
         }else{
            var firstDoc = btlab.Shiseido.OfficialDocuments.As(docs.First());
            var recipients = new List<Sungero.Company.IEmployee>();
            foreach (var empl in allEmpls.ToArray())
            {
              var note = Normal(firstDoc.Note);
              var fullName = Normal(GetFullName(empl));
              var shortName = Normal(empl.Person.ShortName);

              if(empl.Person!=null && (note.Contains(fullName) || note.Contains(shortName))){
                recipients.Add(empl);
              }
            }
            var recipient = recipients.Count == 0 
                ? boxManager 
                : recipients.First();
            foreach (var d in docs)
            {
              if(!btlab.Shiseido.ExchangeDocuments.Is(d))
                continue;
              var doc = btlab.Shiseido.ExchangeDocuments.As(d);
              var type = GetDocumentType(doc.Name);
              if(type == null)
                continue;
              ChangeTypeOfDoc(doc, type, recipient);
            }
            if(recipient != null){
              RedirectTask(task, recipient, task.Deadline);
            }
         }
        }
      }catch(Exception ex){
        Logger.Debug("err="+ex.Message+Environment.NewLine+ex.StackTrace);
      }
      
    }
    
    private List<Sungero.Domain.Shared.IEntity> GetAllAttachments(btlab.Shiseido.IExchangeDocumentProcessingAssignment task){
      var attachments = new List<Sungero.Domain.Shared.IEntity>();
      
      var dontNeedSigningList = task.DontNeedSigning.All.ToList();
      if(dontNeedSigningList.Count > 0)
        attachments.AddRange(dontNeedSigningList);
      
      var needSigningList = task.NeedSigning.All.ToList();
      if(needSigningList.Count > 0)
        attachments.AddRange(needSigningList);
      
      return attachments;
    }
    
    private Sungero.Domain.Shared.IEntityInfo GetDocumentType(string name)
    {
      var preparedName = name.ToLower().Trim();
      var dict = btlab.DiadocIntegration.AssocDicts.GetAll().ToArray();
      var rightDicts = new List<IAssocDict>();
      foreach (var dictRow in dict)
      {
        if (HasRightAssoc(dictRow, preparedName)){
          rightDicts.Add(dictRow);
          break;
        }
      }
      if(rightDicts.Count == 0){
        return null;
      }
      var docName = rightDicts[0].DocName.Value.Value;
      switch(docName){
        //Договор
        case "Contract": return btlab.Shiseido.Contracts.Info;
        //Дополнительное соглашение
        case "SupAgreement": return btlab.Shiseido.SupAgreements.Info;
        //Акт
        case "ContractStateme": return btlab.Shiseido.ContractStatements.Info;
        //Счет
        case "IncomingInvoice": return btlab.Shiseido.IncomingInvoices.Info;
      }
      return null;
    }
    
    private string Normal(string str){
      if(str == null){
        return "";
      }
      return str.Replace('\u00A0', ' ').Trim().ToLower();
    }
    
    private bool HasRightAssoc(IAssocDict d, string preparedName){
      var result = false;
      foreach (var str in d.Synonyms.Split(new []{ ","}, int.MaxValue, StringSplitOptions.RemoveEmptyEntries))
      {
        var assocStr = str.ToLower().Trim();
        if (!preparedName.Contains(assocStr))
          continue;
        result = true;
        break;
      }
      return result;
    }
    private void RedirectTask(btlab.Shiseido.IExchangeDocumentProcessingAssignment task, Sungero.Company.IEmployee empl, Nullable<DateTime> deadline)
    {
      task.Forward(empl, ForwardingLocation.Next, deadline);
      task.Complete(btlab.Shiseido.ExchangeDocumentProcessingAssignment.Result.Complete);
      
      var operation = new Enumeration("Redirect");
      task.History.Write(operation, operation, $"Переадресация на {empl.Person.ShortName}");
      task.Allocated = true;
      task.Save();
    }
    
    private string GetFullName(Sungero.Company.IEmployee empl)
    {
      return $"{empl.Person.LastName} {empl.Person.FirstName} {empl.Person.MiddleName}";
    }
    
    private void ChangeTypeOfDoc(btlab.Shiseido.IExchangeDocument doc, Sungero.Domain.Shared.IEntityInfo type, Sungero.Company.IEmployee empl)
    {
      var changedDoc = doc.ConvertTo(type);
      if(empl != null){
        changedDoc.AccessRights.Grant(empl, DefaultAccessRightsTypes.FullAccess);
      }else{
        Logger.Debug("Некому назначать права на документ.");
      }
      
      if(type.Equals(btlab.Shiseido.Contracts.Info)){
        if(empl==null){
          Logger.Debug("Без адресата нельзя получить подразделение, а потому и сохранить договор.");
          return;
        }
        var contract = btlab.Shiseido.Contracts.As(changedDoc);
        contract.Department = empl.Department;
        contract.Save();
      }else if(type.Equals(btlab.Shiseido.SupAgreements.Info)){
        if(empl==null){
          Logger.Debug("Без адресата нельзя получить подразделение, а потому и сохранить доп. соглашение.");
          return;
        }
        var supAgreement = btlab.Shiseido.SupAgreements.As(changedDoc);
        supAgreement.Department = empl.Department;
        supAgreement.Save();
      }else if(type.Equals(btlab.Shiseido.ContractStatements.Info)){
        if(empl==null){
          Logger.Debug("Без адресата нельзя получить подразделение, а потому и сохранить акт.");
          return;
        }
        var сontractStatements = btlab.Shiseido.ContractStatements.As(changedDoc);
        сontractStatements.Department = empl.Department;
        сontractStatements.Save();
      }else if(type.Equals(btlab.Shiseido.IncomingInvoices.Info)){
        var incInv = btlab.Shiseido.IncomingInvoices.As(changedDoc);
        incInv.SkipContract = true;
        var contractControl = incInv.State.Properties.Contract;
        contractControl.IsEnabled = contractControl.IsRequired = false;
        incInv.Contract = null;
        incInv.Save();
      }
      
      changedDoc.Save();
    }
    
    
  }
}