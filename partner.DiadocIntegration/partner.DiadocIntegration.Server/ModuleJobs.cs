using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace partner.DiadocIntegration.Server
{
  public class ModuleJobs
  {

    /// <summary>
    /// 
    /// </summary>
    public virtual void RedirectDocsFromDiadocJob()
    {
      try{
        /*
        var tasks = partner.Solution1.ExchangeDocumentProcessingAssignments.GetAll()
          .Where(t => !t.Allocatedpartner.HasValue || !t.Allocatedpartner.Value)
          .ToList();
        var count = tasks.Count;
       */
        var ttt = partner.Solution1.ExchangeDocumentProcessingTasks.GetAll()
          .Where(t => t.Status != null && t.Status == Sungero.Workflow.Task.Status.InProcess)
          .ToList();
        var tasks1 = partner.Solution1.ExchangeDocumentProcessingAssignments.GetAll()
          .Where(t => t.Subject!=null && t.Subject.Contains("6913909"))
          .ToList();
        
        var ddd = tasks1
          .Where(t => t.ActiveText!=null && t.ActiveText.Contains("333"))
          .ToList();
        var task = ddd
          .FirstOrDefault();
        //var task = tasks.First();
       // foreach(var task in tasks)
        //{
  
            var attachments = GetAllAttachments(task);
            List<partner.Solution1.IExchangeDocument> attDocs = attachments
              .Where(att => partner.Solution1.ExchangeDocuments.Is(att))
              .Select(att => partner.Solution1.ExchangeDocuments.As(att))
              .ToList();
            if(attDocs.Count == 0)
            {
              //continue;
              return;
            }
            
            var firstDoc = partner.Solution1.ExchangeDocuments.As(attDocs.First());
            var taskId = task.Id;
            var arr = attDocs
              .Select(d => d.Id)
              .ToList();
            var docId = firstDoc.Id;
            var recipient = GetRecipient(firstDoc.Note);
            if(recipient == null)
            {
              //continue;
              return;
            }
          
            
            //foreach(var doc in attDocs)
            //{
                //var type = GetDocumentType(doc.Name);
                //var type = Sungero.Docflow.IncomingDocumentBases.Info;
                var exDoc = partner.Solution1.ExchangeDocuments.As(attDocs.FirstOrDefault());
                var exDocId = exDoc.Id;
                //var zzz = Sungero.Docflow.ExchangeDocuments.As(doc);
                exDoc.ConvertTo(Sungero.Docflow.SimpleDocuments.Info);
                int i1 = 0;
            //}
    
            //AddApprover(task, recipient, task.Deadline);
            int i2 = 0;
        //}
      }catch (Exception e){
        var msg = e.Message;
        int i3 = 0;
      }
    }
    
    private List<Sungero.Domain.Shared.IEntity> GetAllAttachments(partner.Solution1.IExchangeDocumentProcessingAssignment task){
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
      return Sungero.Contracts.ContractBases.Info;
    }
    
    private Sungero.Company.IEmployee GetRecipient(string note)
    {
      //return  Sungero.Company.Employees.GetAll().ToList().Where(e=>e.Person.LastName=="Галаш").FirstOrDefault();
      return  Sungero.Company.Employees.GetAll().ToList().Where(e=>e.Person.LastName=="Юристов").FirstOrDefault();
    }
    
    private void AddApprover(Sungero.Workflow.IAssignment assignment, Sungero.Company.IEmployee newApprover, DateTime? deadline)
    {
      var operation = new Enumeration("Redirect");
      assignment.Forward(newApprover, ForwardingLocation.Next, Calendar.Now);
      assignment.History.Write(operation, operation, string.Format("Переадресация на {0}", GetShortName(newApprover)));
      //int i = 0;
      //operation = new Enumeration("Переадресация задачи");
    }
    
    private string GetShortName(Sungero.Company.IEmployee empl)
    {
      return string.Format("{0} {1}.{2}.", empl.Person.LastName, empl.Person.FirstName, empl.Person.MiddleName);
    }
    
    
  }
}