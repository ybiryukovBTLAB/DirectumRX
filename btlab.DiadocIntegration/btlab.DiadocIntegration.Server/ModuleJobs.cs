using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Sungero.Core;
using Sungero.CoreEntities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace btlab.DiadocIntegration.Server
{
  public class ModuleJobs
  {
    
    //---Костыль, т.к. иначе студия ругается "В классе могут быть объявлены только методы".
    private string GetUrl(){
      return "http://172.16.0.207/InfoBaseTest2/odata/standard.odata/";
    }
    
    private string GetUserName(){
      return "odata.user";
    }
    
    private string GetPass(){
      return "aSWS3dc12345";
    }
    
    private string GetFormatDate(){
      return "yyyy-MM-ddT00:00:00";
    }
    //---
    
    
    /// <summary>
    /// 
    /// </summary>
    public virtual void ReceiptGoodsServicesJob()
    {
     Log($"========= ReceiptGoodsServicesJob: {Calendar.Now} =========");
      try{
        var closingDocs = new List<btlab.Shiseido.IAccountingDocumentBase>();
        closingDocs.AddRange(btlab.Shiseido.ContractStatements.GetAll().ToArray());//Акт
        closingDocs.AddRange(btlab.Shiseido.UniversalTransferDocuments.GetAll().ToArray());//УПД
        closingDocs.AddRange(btlab.Shiseido.Waybills.GetAll().ToArray());//Накладная
        
        var docs = closingDocs
          .Where(d => d.Synhronyse1C.HasValue && d.Synhronyse1C.Value == true);
        
        foreach(var doc in docs){
          if(btlab.Shiseido.ContractStatements.Is(doc)){
            Log($"=== Акт: {doc.Id} ===");
          }else
          if(btlab.Shiseido.UniversalTransferDocuments.Is(doc)){
            Log($"=== УПД: {doc.Id} ===");
          }else
          if(btlab.Shiseido.Waybills.Is(doc)){
            Log($"=== Накладная: {doc.Id} ===");
          }else{
            Log($"=== Неведомый: {doc.Id} ===");
          }
          ExportClosingDocTo1c(doc);
        }
       //var doc = btlab.Shiseido.ContractStatements.Get(687);
       //ExportClosingDocTo1c(doc);
      }catch(Exception ex){
        Log("err="+ex.Message+Environment.NewLine+ex.StackTrace);
      }
    }
    
    private string GetContractorRefFrom1c(Sungero.Parties.ICounterparty counterparty){
      var contractor = Sungero.Parties.Companies.As(counterparty);
      var inn = contractor?.TIN;
      var trrc = contractor?.TRRC;
      if(string.IsNullOrEmpty(inn) || string.IsNullOrEmpty(trrc)){
        return null;
      }
      var filterData = new Dictionary<string, string> {
        {"ИНН", inn},
        {"КПП", trrc}
      };
      var coContractorData = GetObjData("Catalog_Контрагенты", filterData);
      return GetValue(coContractorData, "Ref_Key");
    }
    
    private string GetContractRefFrom1c(Sungero.Docflow.IContractualDocumentBase contract){
      if(contract == null){
        return null;
      }
      var contractorRef = GetContractorRefFrom1c(contract.Counterparty);
      Log($"Ref контрагента договора: {contractorRef}");
      var regNum = contract.RegistrationNumber;
      var regDate = contract.RegistrationDate;
      if(string.IsNullOrEmpty(regNum) || !regDate.HasValue || string.IsNullOrEmpty(contractorRef)){
        return null;
      }
      var filterData = new Dictionary<string, string> {
        {"Номер", regNum},
        {"Дата", regDate.Value.ToString(GetFormatDate())},
        {"Owner_Key", contractorRef}
      };
      var contractData = GetObjData("Catalog_ДоговорыКонтрагентов", filterData);
      return GetValue(contractData, "Ref_Key");
    }
    
    private void ExportClosingDocTo1c(btlab.Shiseido.IAccountingDocumentBase doc){

      var contractorRef = GetContractorRefFrom1c(doc.Counterparty);
      Log($"Ref контрагента: {contractorRef}");
      var contractRef = GetContractRefFrom1c(doc.LeadingDocument);
      Log($"Ref договора: {contractRef}");
      var subject = doc.Subject;
      
      var data = new Dictionary<string,object>();
      if(string.IsNullOrEmpty(doc.RegistrationNumber)){
        Log($"Нет регистрационного номера");
        return;
      }
      
      var formatDate = GetFormatDate();
      var docRegNum = doc.RegistrationNumber;
      //var docRegNum = "T"+Calendar.Now.ToString("HHmmss");
      data.Add("Number", docRegNum);
      data.Add("Date", doc.RegistrationDate.Value.ToString(formatDate));
      data.Add("НомерВходящегоДокумента", docRegNum);
      data.Add("ДатаВходящегоДокумента", doc.RegistrationDate.Value.ToString(formatDate));
      
      if(!string.IsNullOrEmpty(contractorRef)){
        data.Add("Контрагент@odata.bind", $"Catalog_Контрагенты(guid'{contractorRef}')");
      }
      //data.Add("Posted", true);
      if(!string.IsNullOrEmpty(contractRef)){
        data.Add("ДоговорКонтрагента@odata.bind", $"Catalog_ДоговорыКонтрагентов(guid'{contractRef}')");
      }
      if(!string.IsNullOrEmpty(doc.Subject)){
        data.Add("Комментарий", doc.Subject);
      }
      var isUpd = btlab.Shiseido.UniversalTransferDocuments.Is(doc);
      data.Add("ЭтоУниверсальныйДокумент", isUpd); 
      
      var closingDocResult = CreateDocIn1c("ПоступлениеТоваровУслуг", data);
      var responseString = closingDocResult.Content.ReadAsStringAsync().Result;
      if(closingDocResult.StatusCode != HttpStatusCode.Created){
        Log($"status={closingDocResult.StatusCode} resp={responseString}");
        return;
      }
      
      var result = GetDeserializeData(responseString);
      var docRef = result["Ref_Key"] as string;
      
      data = new Dictionary<string,object>();
      data.Add("Статус", "ОригиналПолучен");
      data.Add("Статус_Type", "StandardODATA.СтатусыДокументовПоступления");
      //var orgRef = btlab.Shiseido.BusinessUnits.As(doc.BusinessUnit)?.Id1c;
      //if(string.IsNullOrEmpty(orgRef)){
      //  Log($"Не удалось получить нашу организацию");
      //}
      var docType = "StandardODATA.Document_ПоступлениеТоваровУслуг";
      var statusDocResult = UpdateObjIn1c("InformationRegister_СтатусыДокументов", docRef, docType, data);
      responseString = statusDocResult.Content.ReadAsStringAsync().Result;
      if (statusDocResult.StatusCode != HttpStatusCode.OK)
      {
        Log($"status={statusDocResult.StatusCode} resp={responseString}");
        return;
      }
      
      Log("isUpd="+isUpd);
      var isAct = btlab.Shiseido.ContractStatements.Is(doc);
      if(!isAct){
        return;
      }
      
      
      if(string.IsNullOrEmpty(docRef)){
        Log("Не удалось получить Ref созданного документа");
        return;
      }else{
        Log($"Ref созданного документа: {docRef}");
      }
      
      var relatedFromDocs = doc.Relations.GetRelatedFrom().ToArray();
      var allRelatedDocs = new List<Sungero.Content.IElectronicDocument>(relatedFromDocs);
      foreach(var r in relatedFromDocs){
        allRelatedDocs.AddRange(r.Relations.GetRelated().ToArray());
      }
      var docs = allRelatedDocs
        .Where(r => btlab.Shiseido.IncomingInvoices.Is(r))
        .ToArray();
      
      if(docs.Length < 1){
        Log($"Нет счет-фактур в связях");
        return;
      }
      Log($"Берем счет-фактуру={docs[0].Id}");
      var incInv = btlab.Shiseido.IncomingInvoices.As(docs[0]);
      data = new Dictionary<string,object>();
      if(!string.IsNullOrEmpty(incInv.RegistrationNumber) && incInv.RegistrationDate.HasValue){
        var regNum = incInv.RegistrationNumber;
        //var regNum = "T"+Calendar.Now.ToString("HHmmss");
        data.Add("Number", regNum);
        data.Add("Date", incInv.RegistrationDate.Value.ToString(formatDate));
        data.Add("НомерВходящегоДокумента", regNum);
        data.Add("ДатаВходящегоДокумента", incInv.RegistrationDate.Value.ToString(formatDate));
      }else{
        Log($"Нет регистрационного номера/даты в счет-фактуре");
      }
      
      if(!string.IsNullOrEmpty(contractorRef)){
        data.Add("Контрагент@odata.bind", $"Catalog_Контрагенты(guid'{contractorRef}')");
      }
      data.Add("ДокументОснование", $"{docRef}");
      data.Add("ДокументОснование_Type", "StandardODATA.Document_ПоступлениеТоваровУслуг");
      
      var incInvResult = CreateDocIn1c("СчетФактураПолученный", data);
      responseString = incInvResult.Content.ReadAsStringAsync().Result;
      if(incInvResult.StatusCode != HttpStatusCode.Created){
        Log($"status={incInvResult.StatusCode} resp={responseString}");
        return;
      }      
    }

    private string GetRightNds(string nds){
      switch(nds){
        case "20%": return "НДС20";
        case "0%": return "НДС0";
        case "Без НДС": return "БезНДС";
      }
      return null;
    }
    
    private void ExportIncomingInvoiceDocTo1c(btlab.Shiseido.IIncomingInvoice doc){
      var formatDate = GetFormatDate();
      var contractor = Sungero.Parties.Companies.As(doc.Counterparty);
         
       var inn = contractor?.TIN;
       var trrc = contractor?.TRRC;
       var bankAccNum = doc.Number;
       var amount = doc.TotalAmount;
       var nds = GetRightNds(doc.NdsLink?.NdsValue);
       var subject = doc.Subject;
       //var docRegDate = doc.RegistrationDate;
       //var docRegNum = doc.RegistrationNumber;
       var ourUnitRef = btlab.Shiseido.BusinessUnits.As(doc.BusinessUnit)?.Id1c;
      
       if(string.IsNullOrEmpty(inn) || string.IsNullOrEmpty(trrc) || string.IsNullOrEmpty(bankAccNum)){
         Log($"Не все рекомендованные данные заполнены: inn={inn}, trrc={trrc}, bankAccNum={bankAccNum}.");
         return;
       }
  
       var filterData = new Dictionary<string, string> {
         {"ИНН", inn},
         {"КПП", trrc}
       };
       var contractorData = GetObjData("Catalog_Контрагенты", filterData);
       var contractorRef = GetValue(contractorData, "Ref_Key");
       Log($"Ref контрагента: {contractorRef}");
       if(string.IsNullOrEmpty(contractorRef))
       {
         Log($"Неправильный Ref контрагента: {contractorRef}");
         return;
       }
       if(!string.IsNullOrEmpty(ourUnitRef)){
         Log($"Не удалось получить нашу организацию");
       }
       Log($"Ref организации: {ourUnitRef}");
       filterData = new Dictionary<string, string> {
         {"НомерСчета", bankAccNum}
       };
       var addFilterData = new Dictionary<string, string> {
         {"Owner", ourUnitRef}
       };
       var bankData = GetObjData("Catalog_БанковскиеСчета", filterData, addFilterData);
       var bankAccRef = GetValue(bankData, "Ref_Key");
       Log($"Банковский счет: {bankAccRef}");
       if(string.IsNullOrEmpty(bankAccRef)){
         Log($"Неправильный банковский счет: {bankAccRef}");
       }
       
       string contractRef = null;
       if(doc.Contract != null){
          var coContractor = Sungero.Parties.Companies.As(doc.Contract.Counterparty);
          var coInn = coContractor.TIN;
          var coTrrc = coContractor.TRRC;
          var coRegNum = doc.Contract.RegistrationNumber;
          var coRegDate = doc.Contract.RegistrationDate;
          if(!coRegDate.HasValue || string.IsNullOrEmpty(coRegNum) || string.IsNullOrEmpty(coInn) || string.IsNullOrEmpty(coTrrc)){
            Log("Присутствуют не все необходимые, для поиска договора, данные:");
            Log($"coInn={coInn}, coTrrc={coTrrc}, coRegNum={coRegNum}, coRegDate={coRegDate}");
          } else {
              string coContractorRef;
              if(coInn!=inn || coTrrc!=trrc){
                filterData = new Dictionary<string, string> {
                  {"ИНН", coInn},
                  {"КПП", coTrrc}
                };
                var coContractorData = GetObjData("Catalog_Контрагенты", filterData);
                coContractorRef = GetValue(coContractorData, "Ref_Key");
                Log($"Ref контрагента договора: {coContractorRef}");
              }else{
                coContractorRef = contractorRef;
              }
              filterData = new Dictionary<string, string> {
                {"Номер", coRegNum},
                {"Дата", coRegDate.Value.ToString(formatDate)},
                {"Owner_Key", coContractorRef}
              };
              var contractData = GetObjData("Catalog_ДоговорыКонтрагентов", filterData);
              contractRef = GetValue(contractData, "Ref_Key");
              Log($"Ref договора: {contractRef}");
            }
        }
      
       var data = new Dictionary<string,object>();
       /*
       if(docRegDate.HasValue && !string.IsNullOrEmpty(docRegNum)){
         data.Add("Date", docRegDate.Value.ToString(formatDate));
         //docRegNum = "Т"+Calendar.Now.ToString("HHmmss");
         data.Add("Number", docRegNum);
       }else{
         Log($"regDate={docRegDate}, regNum={docRegNum}");
       }
       */
       data.Add("ВидОперации", "ОплатаПоставщику");
       data.Add("Контрагент", contractorRef);
       data.Add("Контрагент_Type", "StandardODATA.Catalog_Контрагенты");
       //data.Add("Организация@odata.bind", "Catalog_Организации(guid'b7befbac-a735-44a9-8de3-18e236cda461')");
       if(!string.IsNullOrEmpty(ourUnitRef)){
         data.Add("Организация@odata.bind", $"Catalog_Организации(guid'{ourUnitRef}')");//"8a276db6-ce58-11e5-982d-14dae9b19a48"
         data.Add("Организация_Type", "StandardODATA.Catalog_Организации");
       }
       if(!string.IsNullOrEmpty(bankAccRef)){
         data.Add("СчетОрганизации@odata.bind", $"Catalog_БанковскиеСчета(guid'{bankAccRef}')");
       }
       if(!string.IsNullOrEmpty(contractRef)){
         data.Add("ДоговорКонтрагента@odata.bind", $"Catalog_ДоговорыКонтрагентов(guid'{contractRef}')");
       }
       if(amount.HasValue){
         data.Add("СуммаДокумента", $"{amount.Value.ToString()}");
       }
       if(!string.IsNullOrEmpty(nds)){
         data.Add("СтавкаНДС", nds);
       }
       if(!string.IsNullOrEmpty(subject)){
         data.Add("НазначениеПлатежа", subject);
       }
       var tasks = new List<Sungero.Docflow.IFreeApprovalTask>();
       foreach(var t in Sungero.Docflow.FreeApprovalTasks.GetAll()){
         if(t.Attachments.Any(a => a.Id==doc.Id)){
           tasks.Add(t);
         }
       }
       if(tasks.Count > 0){
         var approverFios = tasks[0].Approvers.Select(a => a.Approver.Name).ToArray();
         var fio1 = approverFios.Length > 0 ?approverFios[0] :null;
         var fio2 = approverFios.Length > 1 ?approverFios[1] :null;
         
         if(!string.IsNullOrEmpty(fio1)){
           filterData = new Dictionary<string, string> {
             {"ФИО", fio1}
           };
           var fizLic1Data = GetObjData("Catalog_ФизическиеЛица", filterData);
           var fizLic1Ref = GetValue(fizLic1Data, "Ref_Key");
           Log($"Ref первого согласующего: {fizLic1Ref}");
           if(!string.IsNullOrEmpty(fizLic1Ref)){
            data.Add("Согласовано1_Key",fizLic1Ref);
           }
         }
         if(!string.IsNullOrEmpty(fio2)){
           filterData = new Dictionary<string, string> {
             {"ФИО", fio2}
           };
           var fizLic2Data = GetObjData("Catalog_ФизическиеЛица", filterData);
           var fizLic2Ref = GetValue(fizLic2Data, "Ref_Key");
           Log($"Ref второго согласующего: {fizLic2Ref}");
           if(!string.IsNullOrEmpty(fizLic2Ref)){
            data.Add("Согласовано2_Key",fizLic2Ref);
           }
         }
       }
       
       var json = JsonConvert.SerializeObject(data);
       //Log("requestBody="+json);
       var incInvResult = CreateDocIn1c("ПлатежноеПоручение", data);
       Log("001");
       var responseString = incInvResult.Content.ReadAsStringAsync().Result;
       Log("002");
       if(incInvResult.StatusCode != HttpStatusCode.Created){
         Log($"status={incInvResult.StatusCode} resp={responseString}");
         return;
       }
       doc.WasExportedTo1c = true;
       doc.Save();
       Log("003");
       var result = GetDeserializeData(responseString);
       Log("004");
       var docRef = result["Ref_Key"] as string;
       Log("005");
       if(string.IsNullOrEmpty(docRef)){
         Log("Не удалось получить Ref созданного документа");
         return;
       }else{
         Log($"Ref созданного документа: {docRef}");
       }
       Log("006");
       ConductIt("ПлатежноеПоручение", docRef);
    }
    /// <summary>
    /// 
    /// </summary>
    public virtual void IncomingInvoiceJob()
    {
     Log($"========= IncomingInvoiceJob: {Calendar.Now} =========");
     try{/*
         var d = btlab.Shiseido.IncomingInvoices.Get(815);
         if(d.Synhronyse1C.HasValue && d.Synhronyse1C.Value == true){
            Log($"Synhronyse1C={d.Synhronyse1C}");
           if(!d.WasExportedTo1c.HasValue || d.WasExportedTo1c.Value == false){
              Log($"WasExportedTo1c={d.WasExportedTo1c}");
              if(!docIsLocked(d)){
                ExportIncomingInvoiceDocTo1c(d);
              }else{
                Log($"docIsLocked={docIsLocked(d)}");
              }
           }else{
             Log("WasExportedTo1c=True");
           }
         }else{
           Log("Synhronyse1C=False");
         }
         */
         
         var incInvDocs = btlab.Shiseido.IncomingInvoices.GetAll()
           //.ToArray()//Иначе ругается
           //.Where(d => d.Synhronyse1C.HasValue && d.Synhronyse1C.Value == true)
           //.Where(d => !d.WasExportedTo1c.HasValue || d.WasExportedTo1c.Value == false)
           .ToArray();
         foreach(var doc in incInvDocs){
           Log($"=== Входящий счет: {doc.Id} ===");
           Log($"Synhronyse1C={doc.Synhronyse1C}");
              Log($"WasExportedTo1c={doc.WasExportedTo1c}");
           if(doc.Synhronyse1C.HasValue && doc.Synhronyse1C.Value == true){
              
             if(!doc.WasExportedTo1c.HasValue || doc.WasExportedTo1c.Value == false){
                
                if(!docIsLocked(doc)){
                  ExportIncomingInvoiceDocTo1c(doc);
                }else{
                  Log($"Документ заблокирован");
                }
             }else{
                Log($"Уже был экспортирован в 1с={doc.WasExportedTo1c}");
             }
           }else{
             Log($"Синхронизировать с 1с={doc.Synhronyse1C}");
           }
         }
        
     }catch(Exception e){
       Log($"IncomingInvoiceJob: err= {e.Message+Environment.NewLine+e.StackTrace}");
     }
    
    }
    
    private bool docIsLocked(Sungero.Content.IElectronicDocument doc){
      var lockInfo = Locks.GetLockInfo(doc);
      return lockInfo != null && lockInfo.IsLockedByOther;
    }
    
    private string GetRightValue(string key, string value){
      if(key == "Дата"){
        return $"datetime'{value}'";
      }else
      if(key.EndsWith("_Key")){
        return $"guid'{value}'";
      }
      return $"'{value}'";
    }
    
    private Dictionary<string, object> GetDeserializeData(string strData){
      var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(strData);
      if (result == null) 
          return null;
      RecurseDeserialize(result);
      return result;
    }
    
    private Dictionary<string, object> GetObjData(string objType, Dictionary<string, string> filterData, Dictionary<string, string> addFilter = null)
    {
     var filterStr = string.Join(" and ", filterData.Select(pair => $"{pair.Key} eq {GetRightValue(pair.Key, pair.Value)}"));
     var resMsg = GetDataFrom1c(objType, filterStr);
     var resStr = resMsg.Content.ReadAsStringAsync().Result;

     if(resMsg.StatusCode == HttpStatusCode.OK){
       var result = GetDeserializeData(resStr);
       PostFilter(result, addFilter);
       return result;
     }
     return null;
    }
    
    private void ConductIt(string docType, string docRef)
    {
      Log("007");
      var url = GetUrl();
      var userName = GetUserName();
      var pass = GetPass();
      
      Log("=== ConductIt start "+Calendar.Now+" ===");
      string requestUri = $"{url}Document_{docType}(guid'{docRef}')/Post?PostingModeOperational=false";
      Log("requestUri="+requestUri);
      HttpClientHandler handler = new HttpClientHandler(){
        PreAuthenticate = true,
        Credentials = new NetworkCredential(userName, pass)
      };
       HttpClient client = new HttpClient(handler);
       client.DefaultRequestHeaders.Authorization =
           new AuthenticationHeaderValue("Basic",
               Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(userName+":"+pass)));
       client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
       
       var jsonContent = "{}";
       Log("jsonContent="+jsonContent);
       var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
       var response = client.PostAsync(requestUri, content);
       var responseString = response.Result.Content.ReadAsStringAsync().Result;
       if (response.Result.StatusCode == HttpStatusCode.OK)
       {
           var responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
       }
       else
       {
         Log("status="+response.Result.StatusCode+Environment.NewLine+responseString);
       }
    }

    private void PostFilter(Dictionary<string, object> data, Dictionary<string, string> addFilter = null)
    {
        var dataArr = data["value"] as Dictionary<string, object>[];
        if (dataArr == null || dataArr.Length < 1)
            return;
        data["value"] = dataArr
            .Where(d => d["DeletionMark"] is bool && !(bool) d["DeletionMark"])
            .Where(d => addFilter == null || addFilter.All(f => d[f.Key] != null && (d[f.Key] as string) == f.Value))
            .ToArray();
    }
    
    private string GetValue(Dictionary<string, object> jDict, string key)
    {
      if (jDict == null){
        Log("1");
          return null;
      }
       var dictArr = jDict["value"] as Dictionary<string, object>[];
       if (dictArr == null || dictArr.Length < 1){
         Log("2 = "+(dictArr==null ?"null" :(dictArr.Length+"")));
          return null;
       }
       Log("3");
       return dictArr[0][key] as string;
    }
    
    private void RecurseDeserialize(Dictionary<string, object> result)
    {
       foreach (var pair in result.ToArray()) {
           var jArray = pair.Value as JArray;
           if (jArray == null)
               continue;
           var dictArr = JsonConvert.DeserializeObject<Dictionary<string, object>[]>(jArray.ToString());
           result[pair.Key] = dictArr;
           foreach (var dictionary in dictArr) {
               RecurseDeserialize(dictionary);
           }
       }
    }

   
    private HttpResponseMessage GetDataFrom1c(string type, string filter)
    {
      var url = GetUrl();
      var userName = GetUserName();
      var pass = GetPass();

      string requestUri = url + type + "?$format=json";
      if(!string.IsNullOrEmpty(filter)){
        requestUri += "&$filter=" + filter;
      }
      
      HttpClient client = new HttpClient();
      client.DefaultRequestHeaders.Authorization =
          new AuthenticationHeaderValue("Basic",
              Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(userName+":"+pass)));
    
      client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      
      var response = client.GetAsync(requestUri);
      Log($"requestUri={requestUri}");
      return response.Result;
    }
    
    private HttpResponseMessage UpdateObjIn1c(string objType, string docRef, string docType, Dictionary<string, object> requestData)
    {
        var url = GetUrl();
        var userName = GetUserName();
        var pass = GetPass();
        Log("=== UpdateObjIn1c start "+Calendar.Now+" ===");
        string requestUri = url + objType + $"(Организация_Key=guid'00000000-0000-0000-0000-000000000000', Документ=guid'{docRef}', Документ_Type='{docType}')";
        Log("requestUri="+requestUri);
        HttpClientHandler handler = new HttpClientHandler(){
            PreAuthenticate = true,
            Credentials = new NetworkCredential(userName, pass)
        };
        HttpClient client = new HttpClient(handler);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(userName+":"+pass)));
    
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.BaseAddress = new Uri(requestUri);
         
        var jsonContent = JsonConvert.SerializeObject(requestData);
        Log("jsonContent="+jsonContent);
        var httpVerb = new HttpMethod("PATCH");
        var httpRequestMessage = new HttpRequestMessage(httpVerb, requestUri)
        {
            Content = new StringContent(jsonContent)
        };
       
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        var response = client.SendAsync(httpRequestMessage);
        return response.Result;
    }
  
    private HttpResponseMessage CreateDocIn1c(string docType, Dictionary<string, object> requestData)
    {
      var url = GetUrl();
      var userName = GetUserName();
      var pass = GetPass();
      
      Log("=== CreateDocIn1c start "+Calendar.Now+" ===");
      string requestUri = url + "Document_" + docType + "?$format=json";
      Log("requestUri="+requestUri);
      HttpClientHandler handler = new HttpClientHandler(){
        PreAuthenticate = true,
        Credentials = new NetworkCredential(userName, pass)
      };
       HttpClient client = new HttpClient(handler);
       client.DefaultRequestHeaders.Authorization =
           new AuthenticationHeaderValue("Basic",
               Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(userName+":"+pass)));
    
       client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      
       
       var jsonContent = JsonConvert.SerializeObject(requestData);
       Log("jsonContent="+jsonContent);
       var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
       var response = client.PostAsync(requestUri, content);
       return response.Result;
    }

    /// <summary>
    /// 
    /// </summary>
    public virtual void TestJob()
    {
      try{
        
        Log("=========TestJob: "+Calendar.Now+"=========");
       var d = btlab.Shiseido.IncomingInvoices.Get(813);
       d.WasExportedTo1c = false;
       d = btlab.Shiseido.IncomingInvoices.Get(814);
       d.WasExportedTo1c = false;
      } catch(Exception ex){
        Log("err="+ex.Message+Environment.NewLine+ex.StackTrace);
      }
    }
    
    public void Log(string logMessage)
    {
      using (System.IO.StreamWriter w = System.IO.File.AppendText("C:\\log\\log.txt"))
      {
        w.WriteLine($"{logMessage}");
      }
    }
    
    
    /// <summary>
    /// 
    /// </summary>
    public virtual void RedirectDocsFromDiadocJob()
    {
      try{
        Log("========="+Calendar.Now+"=========");
        var allEmpls = Sungero.Company.Employees.GetAll();
        var boxManager = allEmpls.Where(e => e.Id == 48).FirstOrDefault();
        var assignee = allEmpls.Where(e => e.Id == 54).FirstOrDefault();
        if(assignee == null){
          Log("Не удалось получить изначального адресата писем диадок по его ID=54.");
        }
        if(boxManager == null){
          Log("Не удалось получить ответственного за ящик по его ID=54.");
       }
        var tasks = btlab.Shiseido.ExchangeDocumentProcessingAssignments.GetAll()
            .Where(t => t.Allocated == null || t.Allocated == false)
            .Where(t => t.Status == btlab.Shiseido.ExchangeDocumentProcessingAssignment.Status.InProcess)
            .Where(t => assignee.Id == t.Performer.Id)
            .ToArray();
        Log("tasks="+tasks.Length);
        for(var i=0; i<tasks.Length; i++)
        {
          var task = tasks[i];
          Log("task="+task.Id);
          /*
          if(task.Id != 751)
            continue;
          */
          var docs = GetAllAttachments(task)
            //.Where(d => btlab.Shiseido.ExchangeDocuments.Is(d))
            .ToArray();
          
         if(docs.Length == 0)
         {
            Log("Нет документов");
         }else{
            var firstDoc = btlab.Shiseido.OfficialDocuments.As(docs.First());
            Log("docId="+firstDoc.Id+" firstDoc.Note="+(firstDoc.Note==null ?"null" :firstDoc.Note));
            var arr = allEmpls.ToArray();
            Log("empls.len="+arr.Length);
            var recipients = new List<Sungero.Company.IEmployee>();
            for(var q=0; q<arr.Length; q++){
              var e = arr[q];
              Log("e="+e.Id+"|"+e.Name);
              var w1 = e.Person!=null;
              Log("E1 w1="+w1);
              var w2 = GetFullName(e);
              Log("E2 w2="+w2);
              var w3 = e.Person.ShortName;
              Log("E3 w3="+w3);
              /*
              string s = "";
              var aaa = firstDoc.Note.ToCharArray();
              for(var h=0; h<aaa.Length; h++){
                var chNum = (int)aaa[h];
                s += "["+aaa[h]+":"+chNum+"]";
              }
              Log("Note chars = "+s);
              s = "";
              aaa = w3.Trim().Replace('\u00A0', ' ').ToCharArray();
              for(var h=0; h<aaa.Length; h++){
                var chNum = (int)aaa[h];
                s += "["+aaa[h]+":"+chNum+"]";
              }
              Log("fio chars = "+s);
              Log("!!! = "+firstDoc.Note.Contains(w3.Trim().Replace('\u00A0', ' ')));
              */
             /*
              String[] spearator = { "\u00A0", " " };
              Int32 count = 999;
              */
              var note = Normal(firstDoc.Note);
              
              var w4 = note.Contains(Normal(w2));
              Log("E4 w4="+w4);
              var w5 = note.Contains(Normal(w3));
              Log("E5 w5="+w5);
              //var sss = w3.Trim().Split(spearator, count, StringSplitOptions.RemoveEmptyEntries);
              /*
              Log("sss len="+sss.Length);
              for(var t=0; t<sss.Length; t++){
                Log("ss="+sss[t]+" => "+firstDoc.Note.Contains(sss[t]));
              }
              */
              if(w1 && (w4 || w5)){
                recipients.Add(e);
                //break;
              }
            }
            Log("wuw");
            /*
            var recipients = allEmpls
              .Where(e => {
                  Log("e="+e.Id+"|"+e.Name);
                  return e.Person!=null && (firstDoc.Note.Contains(GetFullName(e)) || firstDoc.Note.Contains(e.Person.ShortName));
               })
              .ToArray();
              */
            Sungero.Company.IEmployee recipient;
            if(recipients.Count == 0)
            {
              //Log("Не удалось понять кому из сотрудников переадрисовывать.");
              recipient = boxManager;
              //continue;
            }
            else
            {
              recipient = recipients.First();
            }
            //var wasRightType = false;
            for(var j=0; j<docs.Length; j++)
            {
              if(!btlab.Shiseido.ExchangeDocuments.Is(docs[j])){
                Log("формализованый документ="+docs[j].Info.LocalizedName);
                continue;
              }
              var doc = btlab.Shiseido.ExchangeDocuments.As(docs[j]);
              Log("doc["+j+"]="+doc.Id+" name="+doc.Name);
              var type = GetDocumentType(doc.Name);
              if(type == null)
              {
                Log("Не удалось понять какой тип должен быть у документа.");
                continue;
              }/*else if(type.Equals(btlab.Shiseido.IncomingInvoices.Info)){
                Log("Со счетом ещё не определились.");
                continue;
              }*/else
              {
                //wasRightType = true;
                Log("name="+doc.Name+" type="+type.LocalizedName);
              }
              ChangeTypeOfDoc(doc, type, recipient);
            }
            if(recipient != null/* && wasRightType*/){
              RedirectTask(task, recipient, task.Deadline);
              //break;
            }
            
         }
        }
      }catch(Exception ex){
        Log("err="+ex.Message+Environment.NewLine+ex.StackTrace);
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
      Log("0 GetDocumentType");
      var preparedName = name.ToLower().Trim();
      Log("1");
      var dicts = btlab.DiadocIntegration.AssocDicts.GetAll().ToArray();
      Log("1.3 dicts.len="+dicts.Length);
      var rightDicts = new List<IAssocDict>();
      for(var i=0; i<dicts.Length; i++){
        if(foo1(dicts[i], preparedName)){
          rightDicts.Add(dicts[i]);
          break;
        }
      }
      Log("1.5 rightDicts.len="+rightDicts.Count);
      var firstDict = rightDicts.FirstOrDefault();
      if(firstDict == null){
        return null;
      }
      var docName = rightDicts.First().DocName.Value.Value;
      /*
      var docName = btlab.DiadocIntegration.AssocDicts.GetAll()
        .Where(d => foo1(d, preparedName))
        .Select(d => d.DocName.Value.Value)
        .First();
        */
      Log("1.7 docName="+docName);
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
    
    private bool foo2(string s, string preparedName){
      Log("3.3 foo2 name="+ preparedName);
      var q3 = s.ToLower().Trim();
      Log("3.5 foo2 word="+ q3);
      var q4 = preparedName.Contains(q3);
      Log("3.7 foo2 result="+ q4);
      return q4;
    }
    private bool foo1(IAssocDict d, string preparedName){
      String[] spearator = { ","};
      Int32 count = 999;
      //var sss = w3.Trim()
      Log("2 foo1 id="+ d.Id);
      Log("2 foo1 syns="+ d.Synonyms);
      var q1 = d.Synonyms.Split(spearator, count, StringSplitOptions.RemoveEmptyEntries);
      //var q1 = Split(d.Synonyms, ",");
      Log("3 foo1 len="+q1.Length);
      var result = false;
      for(var i=0; i<q1.Length; i++){
        if(foo2(q1[i], preparedName)){
          result = true;
          break;
        }
      }
      //var q2 = q1.Any(s => foo2(s, preparedName));
      Log("4 foo1 res="+result);
      return result;
    }
    private void RedirectTask(btlab.Shiseido.IExchangeDocumentProcessingAssignment task, Sungero.Company.IEmployee empl, Nullable<DateTime> deadline)
    {
      task.Forward(empl, ForwardingLocation.Next, deadline);
      task.Complete(btlab.Shiseido.ExchangeDocumentProcessingAssignment.Result.Complete);
      
      var operation = new Enumeration("Redirect");
      task.History.Write(operation, operation, string.Format("Переадресация на {0}", empl.Person.ShortName));
      task.Allocated = true;
      task.Save();
    }
    
    private string GetFullName(Sungero.Company.IEmployee empl)
    {
      return string.Format("{0} {1} {2}", empl.Person.LastName, empl.Person.FirstName, empl.Person.MiddleName);
    }
    
    private void ChangeTypeOfDoc(btlab.Shiseido.IExchangeDocument doc, Sungero.Domain.Shared.IEntityInfo type, Sungero.Company.IEmployee empl)
    {
      var changedDoc = doc.ConvertTo(type);
      if(empl != null){
        changedDoc.AccessRights.Grant(empl, DefaultAccessRightsTypes.FullAccess);
      }else{
        Log("Некому назначать права на документ.");
      }
      
      if(type.Equals(btlab.Shiseido.Contracts.Info)){
        if(empl==null){
          Log("Без адресата нельзя получить подразделение, а потому и сохранить договор.");
          return;
        }
        var contract = btlab.Shiseido.Contracts.As(changedDoc);
        contract.Department = empl.Department;
        contract.Save();
      }else if(type.Equals(btlab.Shiseido.SupAgreements.Info)){
        if(empl==null){
          Log("Без адресата нельзя получить подразделение, а потому и сохранить доп. соглашение.");
          return;
        }
        var supAgreement = btlab.Shiseido.SupAgreements.As(changedDoc);
        supAgreement.Department = empl.Department;
        supAgreement.Save();
      }else if(type.Equals(btlab.Shiseido.ContractStatements.Info)){
        if(empl==null){
          Log("Без адресата нельзя получить подразделение, а потому и сохранить акт.");
          return;
        }
        var сontractStatements = btlab.Shiseido.ContractStatements.As(changedDoc);
        Log("Акт empl.dep="+empl.Department.ShortName);
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