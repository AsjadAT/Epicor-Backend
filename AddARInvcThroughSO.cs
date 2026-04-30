using Epicor.Customization.Bpm;
using Epicor.Data;
using Epicor.Hosting;
using Epicor.Utilities;
using Erp;
using Erp.Tables;
using Ice;
using Ice.Contracts;
using Ice.ExtendedData;
using Ice.Tables;
using Ice.Tablesets;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace EFx.CreateARInvc.Implementation
{
    partial class AddARInvc1Impl
    {
        protected override int RunStep(int workflowStep)
        {
            var conditionBlockValue = false;

            switch (workflowStep)
            {
                case Epicor.Functions.PredefinedStep.Start: // Execute Custom Code 1
                    try
                    {
                        this.A001_CustomCodeAction();
                    }
                    catch (Ice.Common.BusinessObjectException ex)
                    {
                        this.RememberException(ex);
                    }
                    return Epicor.Functions.PredefinedStep.Exit;
                default:
                    return Epicor.Functions.PredefinedStep.Unknown;
            }
        }

        private void A001_CustomCodeAction()
        {
            /*Func<DataSet, bool> isValidDataset = (ds) =>
 
    {
 
       if (ds == null) return false;
 
       foreach (DataTable table in ds.Tables)
 
          if (table.Rows.Count == 0) return false;
 
    return true;
 
    };*/
 

 
    Erp.Tablesets.ARInvoiceTableset ArTS = new Erp.Tablesets.ARInvoiceTableset();
 
    Erp.Tablesets.InvcGrpTableset InvcTS = new Erp.Tablesets.InvcGrpTableset();
    
    
 
 
    this.CallService<Erp.Contracts.InvcGrpSvcContract>(bo=>
 
    {
 
      //Fetching Grp Id
 
       var tblGrp = (from r in Db.InvcGrp where r.GroupID == GroupID select r).FirstOrDefault();
 
 
        //Grp Id exists
 
        if (tblGrp!=null)
 
          {
 
            bo.GetInvcGrp(GroupID, ref InvcTS);
 
            if(grpApplyDate!= null && grpInvoiceDate != null)
 
            {
 
               try{
               
                tblGrp.InvoiceDate = grpInvoiceDate;
                tblGrp.ApplyDate = grpApplyDate;
                bo.OnChangeofInvoiceDate(GroupID, grpInvoiceDate, ref InvcTS);
                bo.OnChangeofApplyDate(GroupID, grpApplyDate, ref InvcTS);
              }
              catch{
                    Msg = "Invoice generation failed!";
 
                }

 
                bo.Update(ref InvcTS);
 
            }
 
          }
 
 
        //Grp Id does not exist
 
        else
 
          {
 
            bo.GetNewInvcGrp(ref InvcTS);
            var tblInvcts = (from row in InvcTS.InvcGrp select row).FirstOrDefault();
            if(tblInvcts != null)
            {
                tblInvcts.GroupID = GroupID;
                tblInvcts.RowMod = "A";
                if(grpApplyDate!= null && grpInvoiceDate != null)
                {
                    bo.OnChangeofInvoiceDate(GroupID, InvoiceDate, ref InvcTS);
                    bo.OnChangeofApplyDate(GroupID, ApplyDate, ref InvcTS);
                }
                else
                {
                    throw new Exception("Make sure to Input both ApplyDate and Invoice date!");            
                }
            }
 
          }
 
          bo.Update(ref InvcTS);
 
    });

        
        
 
         this.CallService<Erp.Contracts.ARInvoiceSvcContract>(bo=>
 
        {
            var tblOrder = (from r in Db.InvcHead where r.OrderNum.ToString() == OrderNum select r).FirstOrDefault();
            
            if(tblOrder == null)
            {
                throw new Exception("Order not found");
            }
            
            
            //*Params for Invcheadtype
            string InvoiceType = "MISC-BILL";
            
            bo.GetNewInvcHeadType(GroupID,InvoiceType, ref ArTS);
            
            /*
            Params for OnChangeofOrderNum
            1)InvoiceNum in the response is declared
            2)OrderNum is given in the input
            3)bool checkForResponse = true;
            */
            string responseMessage = " ";
           
            
            //checking if ordernumber exists or not
            var tblInvHed = (from r in ArTS.InvcHead select r).FirstOrDefault();
            
            if(tblInvHed !=null)
            {
            
            
              //Passing InvoiceNum as 0 as per the trace logs
              bo.OnChangeofOrderNum(0,Convert.ToInt32(OrderNum),true, out responseMessage, ref ArTS);
              
              
              var tblInvchead = (from r in ArTS.InvcHead where r.OrderNum.ToString()==OrderNum select r).FirstOrDefault();
              
              if(PONum==" "){throw new Exception("Please Enter PO Num!");}
              
              
              if(tblInvchead==null){ throw new Exception("OrderNum not found!");}
              
              tblInvchead.PONum = PONum;
              tblInvchead.InvoiceDate = InvoiceDate;
              tblInvchead.ShipDate = ShipmentDate;
              tblInvchead.ApplyDate = ApplyDate;
              
              //Params for InvoiceDate
              string recalcamts = "";
              string cMessageText = "";
              
            
             
              //var ttInvcHed = (from row in ArTS.InvcHead select row).FirstOrDefault();
              //ttInvcHed.RowMod = "A";
              
              
              
              //bo.OnChangeofShipDate(tblInvchead.InvoiceNum,ShipmentDate, ref ArTS);
              //bo.OnChangeofInvDateEx(tblInvchead.InvoiceNum,InvoiceDate,recalcamts,out cMessageText, ref ArTS );
              //bo.GetvalidEAD();
              
              bo.Update(ref ArTS);
              
              
              InvoiceNum = tblInvchead.InvoiceNum;
            }
            
            
            
            
           



// Invoice lines
JArray Lines = JArray.Parse(InvoiceLines);

foreach (var xxinvoiceLine in Lines)
{
    int invoiceLine = Convert.ToInt32(xxinvoiceLine["InvoiceLine"]);
    int orderNum = Convert.ToInt32(xxinvoiceLine["OrderNum"]);
    int orderLine = Convert.ToInt32(xxinvoiceLine["OrderLine"]);
    var tblOrderDetail = (from row in Db.OrderDtl where row.OrderNum.ToString() == OrderNum && row.OrderLine == orderLine select row).FirstOrDefault();
    if (tblOrderDetail == null)
    {
        throw new Exception($"Line Number {orderLine} does not exist");
    }
    else
    {
        ArTS = bo.GetByID(InvoiceNum);
        bo.GetNewInvcDtl(ref ArTS, InvoiceNum);
        var tblInvcDtl = (from row in ArTS.InvcDtl where row.RowMod == "A" select row).FirstOrDefault();
        var tblOrderDtl = (from row in Db.OrderDtl where row.Company==this.CompanyID  && row.OrderNum.ToString()== OrderNum &&row.OrderLine == orderLine select row).FirstOrDefault();

        if (tblInvcDtl != null)
        {
            

            tblInvcDtl.InvoiceLine = invoiceLine;
            tblInvcDtl.OrderNum = orderNum;
            tblInvcDtl.OrderLine = orderLine;
            tblInvcDtl.OrderRelNum = 1;
            tblInvcDtl.SellingOrderQty = tblOrderDtl.OrderQty;
            //tblInvcDtl.SellingShipQty = tblOrderDtl.OrderQty;
            decimal NewQty = tblOrderDtl.OrderQty;
            bo.OnChangeofLineQty(InvoiceNum,invoiceLine, NewQty,ref ArTS );
            
            //tblInvcDtl.PartNum = tblOrderDtl.PartNum;
            tblInvcDtl.RowMod = "A";
    
            bo.OnChangeofLineOrderLine(InvoiceNum, invoiceLine, orderLine, ref ArTS);
            //bo.OnChangeofLineOrderRelease(InvoiceNum, 1, 1, ref ArTS);
            /*tblInvcDtl.OrderNum = orderNum;
            tblInvcDtl.OrderLine = orderLine;
            tblInvcDtl.OrderRelNum = 1;*/
            tblInvcDtl.RowMod = "A";
            
            
            
            /*
            bool genAmortSched = false;
            string cARLOCID = "";
            decimal dTotalCharges = 0;
            decimal grpTotalinvAmt = 0;
            string opGenMessage = "";
            string opLtrCrdMsg = "";
            bool IUpdateRan = true;
            
            bo.UpdateMaster(ref ArTS,GroupID, "InvcDtl",false,false, ref genAmortSched,false, InvoiceNum, invoiceLine,cARLOCID,false,dTotalCharges,false,out grpTotalinvAmt, out opGenMessage, out opLtrCrdMsg, out IUpdateRan);
            */
            
            string NewPartNum = tblOrderDtl.PartNum;
            bo.OnChangeofLinePartNum(InvoiceNum,invoiceLine,NewPartNum, ref ArTS);
            bo.Update(ref ArTS);
        }
        
    }
}
            
            
          });
          
    
    Msg = "Invoice generated"; //Invoice is generated but only one line is added
    
    
    
   
    
    
    
    
   
    
    
    
    
 

        }
    }
}
