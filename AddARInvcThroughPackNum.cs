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
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace EFx.CreateARInvc.Implementation
{
    partial class AddARInvcImpl
    {
        protected override int RunStep(int workflowStep)
        {
            var conditionBlockValue = false;

            switch (workflowStep)
            {
                case Epicor.Functions.PredefinedStep.Start: // Execute Custom Code 0
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
 
 
//try{
 
    //Keeping it for future use if needed
 
    Erp.Tablesets.ARInvoiceTableset ArTS = new Erp.Tablesets.ARInvoiceTableset();
 
    Erp.Tablesets.InvcGrpTableset InvcTS = new Erp.Tablesets.InvcGrpTableset();
 
 
    this.CallService<Erp.Contracts.InvcGrpSvcContract>(bo=>
 
    {
 
      //Fetching Grp Id
 
       var tblGrp = (from r in Db.InvcGrp where r.GroupID == GroupID select r).FirstOrDefault();
 
       var tblShip = (from r in Db.ShipHead where r.PackNum.ToString() == PackSlips select r).FirstOrDefault();
 
       if (tblShip==null)
 
       {
 
          throw new Exception("PackSlip does not exist! Make sure you enter the correct Number.");
 
       }
 
 
        //Grp Id exists
 
        if (tblGrp!=null)
 
          {
 
            bo.GetInvcGrp(GroupID, ref InvcTS);
 
            if(ApplyDate!= null && InvoiceDate != null)
 
            {
 
               try{
                bo.OnChangeofInvoiceDate(GroupID, InvoiceDate, ref InvcTS);
                bo.OnChangeofApplyDate(GroupID, ApplyDate, ref InvcTS);
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
                if(ApplyDate!= null && InvoiceDate != null)
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

         //Parameters for getshipment
 
         string CustList = " ";
 
         //string PackSlips = " ";
 
         string Plant = "CURRENT";
 
         string Invoices ="";
 
         string Errors = " ";
 
         string msgNumInvoices = " ";
 
         decimal grpTotalInvAmt = 0;
 
 
         this.CallService<Erp.Contracts.ARInvoiceSvcContract>(bo=>
 
        {
 
            //Getting the Invoice number

 
            bo.GetShipments(GroupID,CustList,PackSlips,Plant,true,false,out Invoices, out Errors, out msgNumInvoices, out grpTotalInvAmt );


 
            //Putting the recieved invoice num into the response parameter
 
            InvoiceNum = Invoices;
 
 
            //Filling the tableset
 
            //Keeping it for future use if needed
 
            ArTS = bo.GetByID(Convert.ToInt32(Invoices));
 
            var tblInvcDtl = (from dr in Db.InvcDtl where dr.PackNum.ToString() == PackSlips select dr).FirstOrDefault();
 
 
          });
 
    Msg = "Invoice generated";
 
//bracket end for try
        }
    }
}
