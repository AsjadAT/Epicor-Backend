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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace EFx.AddUpdateSO.Implementation
{
    partial class Add_UpdateSO1Impl
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
            Func<DataSet, bool> isValidDataset = (ds) => {
if (ds == null) return false;
foreach (DataTable table in ds.Tables)
if (table.Rows.Count == 0) return false;
return true;
};
/*---------------------------------------Start-------------------------------------------------------*/


Erp.Tablesets.SalesOrderTableset SalesTS =new Erp.Tablesets.SalesOrderTableset();

try
{
  if (true) 
  {
  // Customer
  if (CustID == null){
      throw new Exception("CustID is Null!");}
      
  string vCustID = CustID.ToString(); 
  
  
    
  // OrderHed
  string vCompanyID = this.callContextClient.CurrentCompany;
  string vShipComment = ShipComment.ToString();
  string vOrderPO = PONum.ToString();
  string vShipToNum = ShipToNum;
  string vTermCode = TermsCode.ToString();
  int vOrderNum = OrderNum;
  DateTime? vOrderDate = OrderDate;
  DateTime? vNeedByDate = NeedByDate;
  DateTime? vRequestDate = RequestDate;
  int vCustNum = 0;
  string cCreditLimitMessage = string.Empty;
  string cAgingMessage = string.Empty;
  bool lContinue = false;

  
  

  vCustNum = (from r in Db.Customer where r.Company == this.CompanyID && r.CustID == vCustID select r.CustNum).FirstOrDefault();
  //Erp.Tablesets.SalesOrderTableset SalesTS =new Erp.Tablesets.SalesOrderTableset();

  if (OrderNum ==0)
  {  
  
      Msg = string.Format("CustID: {0}, vTermCode: {1}, OrderDate: {2}", vCustID, vTermCode, vOrderDate);
      
      //Data validation Start
      var tblCustomer = (from row in Db.Customer where row.Company == vCompanyID &&  row.CustID == vCustID select row).FirstOrDefault();
      
      if(tblCustomer == null)
      {
          throw new Exception(string.Format("CustID {0} does not exist in Company {1}....", vCustID, vCompanyID));
         
      }
      
      else
      {
          vCustNum = tblCustomer.CustNum;
      }
      
      if(vShipToNum != "")
      {
      var tblShipTo = (from row in Db.ShipTo where row.Company == vCompanyID && row.CustNum == vCustNum && row.ShipToNum == vShipToNum select row).FirstOrDefault();
      
          if(tblShipTo == null)
          {
              throw new Exception(string.Format("Ship To {0} for CustID {1} does not exist in Company {2}.", vShipToNum, vCustID, vCompanyID));
          }
      }
      
      var tblTerms = (from row in Db.Terms where row.Company == vCompanyID && row.TermsCode == vTermCode select row).FirstOrDefault();
      if(tblTerms == null)
      {
      throw new Exception(string.Format("Term code {0} does not exist in Company {1}.", vTermCode, vCompanyID));
      }
  
  
    this.CallService<Erp.Contracts.SalesOrderSvcContract>(bo => {
    bo.GetNewOrderHed(ref SalesTS);
    //bo.ChangeOrderHedShipToNum();
    var ttOrderHed = (from r in SalesTS.OrderHed select r).FirstOrDefault();
    if (ttOrderHed != null) {
    ttOrderHed.CustNum = vCustNum;
    ttOrderHed.TermsCode = vTermCode;
    ttOrderHed.OrderDate = vOrderDate;
    ttOrderHed.NeedByDate = vNeedByDate;
    ttOrderHed.RequestDate = vRequestDate;
    ttOrderHed.BTCustID = vCustID;
    ttOrderHed.Company = vCompanyID;
    ttOrderHed.ShipComment = vShipComment;
    //ttOrderHed.ShipToNum = vShipToNum;
    ttOrderHed.PONum = vOrderPO;

     
    bo.OnChangeofSoldToCreditCheck(vOrderNum, vCustID, out cCreditLimitMessage, out cAgingMessage, out lContinue, ref SalesTS);
    bo.ChangeSoldToID(ref SalesTS);
    bo.ChangeCustomer(ref SalesTS);
    bo.ChangeNeedByDate(ref SalesTS, "OrderHed");
    ttOrderHed.ShipToNum = vShipToNum;
    bo.ChangeShipToID(ref SalesTS);
    Msg = "ShipTo Address is" + ttOrderHed.ShipToAddressFormatted;
    bool lCheckForOrderChangedmsg = true;
    bool lcheckForResponse = true;
    string cTableName = "OrderHed";
    int iCustNum = vCustNum;
    int iOrderNum = vOrderNum;
    bool lweLicensed = false;

    string cResponsemsg = "";
    string cCreditShipAction ="";
    string cDisplaymsg = "";
    string cCompliantmsg = "";
    string cResponsemsgOrdRel = "";
     // string cAgingMessage = "";

    //bo.MasterUpdate(lCheckForOrderChangedmsg,lcheckForResponse,cTableName,iCustNum,iOrderNum,lweLicensed,out lContinue,
    //out cResponsemsg,out cCreditShipAction,out cDisplaymsg,out cCompliantmsg,out cResponsemsgOrdRel,out cAgingMessage,ref SalesTS );

    bo.Update(ref SalesTS);
    Msg = "ShipTo Address is" + ttOrderHed.ShipToAddressFormatted + " ShipToNum="+ ttOrderHed.ShipToNum +" "+vShipToNum + " ";
    vOrderNum = ttOrderHed.OrderNum;
    Msg += Environment.NewLine + "Order Head Created and order num is :" +ttOrderHed.OrderNum.ToString();

    }


    JArray xxLines = JArray.Parse(OrderLines);
    //foreach (DataRow dr in SalesOrderDS.Tables["OrderDtl"].Rows) {
    foreach (var dr in xxLines){
    Msg += Environment.NewLine + "Order Line Input, Part Num :" +dr ["PartNum"].ToString() +" Qty : " +dr ["Qty"].ToString();

     
    // SalesTS = new Erp.Tablesets.SalesOrderTableset();
    /*this.CallService<Erp.Contracts.SalesOrderSvcContract>(
    bo =>
    {*/
    SalesTS = bo.GetByID(vOrderNum); 
    bo.GetNewOrderDtl(ref SalesTS, vOrderNum);
    var ttOrderDtl = (from r in SalesTS.OrderDtl where r.RowMod == "A" select r).FirstOrDefault();
    if (ttOrderDtl != null) {
    bool lSubstitutePartExist = false;
    bool lIsPhantom = false;
    string uomCode = "";
    Guid SysRowID = Guid.Parse("00000000-0000-0000-0000-000000000000");
    string rowType = "";
    bool salesKitView = false;
    bool removeKitComponents = false;
    bool suppressUserPrompts = false;
    bool getPartXRefInfo = true;
    bool checkPartRevisionChange = true;
    bool checkChangeKitParent = true;
    string cDeleteComponentsMessage = "";
    string questionString = "";
    string cWarningMessage = "";
    bool multipleMatch = false;
    bool promptToExplodeBOM = false;
    string cConfigPartMessage = "";
    string cSubPartMessage = "";
    string explodeBOMerrMessage = "";
    string cmsgType = "";
    bool multiSubsAvail = false;
    bool runOutQtyAvail = false;
    decimal ipSellingQuantity = Convert.ToDecimal(dr ["Qty"] .ToString());
    bool chkSellQty = false;
    bool negInvTest = false;
    bool chgSellQty = true;
    bool chgDiscPer = true;
    // bool suppressUserPrompts = false;
    bool lKeepUnitPrice = true;
    //string pcPartNum = dr ["PartNum"].ToString();
    string pcWhseCode = "";
    string pcBinNum = "";
    string pcLotNum = "";
    int pcAttributeSetID = 0;
    string pcDimCode = "EA";
    decimal pdDimConvFactor = 1;
    string pcMessage = "";
    string pcNeqQtyAction = "";
    string opWarningmsg = "";
    string cSellingQuantityChangedmsgText = "";
    decimal vUnitPrice = Convert.ToDecimal(dr["UnitPrice"]);
    // dr["PartNum"].ToString();
    string partNum = dr ["PartNum"].ToString();
    //Data validation Start
      var tblPart = (from row in Db.Part where row.Company == vCompanyID && row.PartNum == partNum select row).FirstOrDefault();
      if(tblPart == null)
      {
      throw new Exception(string.Format("Part Number {0} does not exist in Company {1}.", partNum, vCompanyID));
      }
    //Data validation End

    ttOrderDtl.PartNum = partNum;

    bo.ChangePartNum(ref SalesTS, true, partNum);
    bo.ChangePartNumMaster(
      ref partNum, ref lSubstitutePartExist, ref lIsPhantom, ref uomCode,
      SysRowID, rowType, salesKitView, removeKitComponents,
      suppressUserPrompts, getPartXRefInfo, checkPartRevisionChange,
      checkChangeKitParent, out cDeleteComponentsMessage,
      out questionString, out cWarningMessage, out multipleMatch,
      out promptToExplodeBOM, out cConfigPartMessage, out cSubPartMessage,
      out explodeBOMerrMessage, out cmsgType, out multiSubsAvail,
      out runOutQtyAvail, ref SalesTS);
    bo.ChangeSellingQtyMaster(
    ref SalesTS, ipSellingQuantity, chkSellQty, negInvTest, chgSellQty,
    chgDiscPer, suppressUserPrompts, lKeepUnitPrice, partNum,
    pcWhseCode, pcBinNum, pcLotNum, pcAttributeSetID, pcDimCode,
    pdDimConvFactor, out pcMessage, out pcNeqQtyAction,
    out opWarningmsg, out cSellingQuantityChangedmsgText);
    ttOrderDtl.SellingQuantity = ipSellingQuantity;
    ttOrderDtl.UnitPrice = vUnitPrice;
    ttOrderDtl.DocUnitPrice = vUnitPrice;
    ttOrderDtl.DspUnitPrice = vUnitPrice;
    ttOrderDtl.DocDspUnitPrice = vUnitPrice;
    bo.ChangeUnitPrice(ref SalesTS);

    ttOrderDtl.RowMod = "A";

    bo.Update(ref SalesTS);
    Msg += Environment.NewLine + "Order Head Created and order Line is :" + ttOrderDtl.OrderLine.ToString();
    }
    Msg += Environment.NewLine + "Order Head Created and order Line is :" + ttOrderDtl.OrderLine.ToString();
    // }
    //);
    }
    Status = "Completed Successfully";
    Response = SalesTS;
    //
    });
  }
  
   else 
        {   
        
            bool lSubstitutePartExist = false;
            bool lIsPhantom = false;
            string uomCode = "";
            Guid SysRowID = Guid.Parse("00000000-0000-0000-0000-000000000000");
            string rowType = "";
            bool salesKitView = false;
            bool removeKitComponents = false;
            bool suppressUserPrompts = false;
            bool getPartXRefInfo = true;
            bool checkPartRevisionChange = true;
            bool checkChangeKitParent = true;
            string cDeleteComponentsMessage = "";
            string questionString = "";
            string cWarningMessage = "";
            bool multipleMatch = false;
            bool promptToExplodeBOM = false;
            string cConfigPartMessage = "";
            string cSubPartMessage = "";
            string explodeBOMerrMessage = "";
            string cmsgType = "";
            bool multiSubsAvail = false;
            bool runOutQtyAvail = false;
            
            bool chkSellQty = false;
            bool negInvTest = false;
            bool chgSellQty = true;
            bool chgDiscPer = true;
            // bool suppressUserPrompts = false;
            bool lKeepUnitPrice = true;
            string pcWhseCode = "";
            string pcBinNum = "";
            string pcLotNum = "";
            int pcAttributeSetID = 0;
            string pcDimCode = "EA";
            decimal pdDimConvFactor = 1;
            string pcMessage = "";
            string pcNeqQtyAction = "";
            string opWarningmsg = "";
            string cSellingQuantityChangedmsgText = "";
            
            //Erp.Tablesets.SalesOrderTableset SalesTS = new Erp.Tablesets.SalesOrderTableset();
            
            
            // Code to update existing order details
            //string vCustID = CustID;
            
            //int vOrderNum = OrderNum;
            
            JArray xxLines = JArray.Parse(OrderLines);
           
            this.CallService<Erp.Contracts.SalesOrderSvcContract>(bo =>
            {
                SalesTS = bo.GetByID(vOrderNum);
                
                var ttOrderHed = (from r in SalesTS.OrderHed where r.OrderNum == vOrderNum select r).FirstOrDefault();
                
                //ttOrderHed.CustomerCustID = vCustID;
                ttOrderHed.NeedByDate = vNeedByDate;
                ttOrderHed.RequestDate = vRequestDate;
                ttOrderHed.RowMod = "U";
                
                 bo.OnChangeofSoldToCreditCheck(vOrderNum, vCustID, out cCreditLimitMessage, out cAgingMessage, out lContinue, ref SalesTS);
                 bo.ChangeSoldToID(ref SalesTS);
                 bo.ChangeCustomer(ref SalesTS);
                 bo.ChangeNeedByDate(ref SalesTS, "OrderHed");
                

                // Update order details based on user input
                foreach (var dr in xxLines)
                {
                    string vPartNum = dr["PartNum"].ToString();
                    int vLineNumber =  Convert.ToInt32(dr["LineNumber"].ToString());
                    decimal vUnitPrice = Convert.ToDecimal(dr["UnitPrice"].ToString());
                    decimal vQty = Convert.ToDecimal(dr["Qty"].ToString());
                    
                    
                    var ttOrderDtl = (from r in SalesTS.OrderDtl where r.OrderNum == vOrderNum select r).FirstOrDefault();
                    
                    if (ttOrderDtl == null)
                        throw new Exception("OrderDtl is null!");
                        
                    if (ttOrderDtl != null)
                    {
                        // Update existing order detail
                        ttOrderDtl.PartNum = vPartNum;
                        ttOrderDtl.SellingQuantity = vQty;
                        ttOrderDtl.UnitPrice = vUnitPrice;
                        ttOrderDtl.DocUnitPrice = vUnitPrice;
                        ttOrderDtl.DspUnitPrice = vUnitPrice;
                        ttOrderDtl.DocDspUnitPrice = vUnitPrice;
                        ttOrderDtl.RowMod = "U";
                        
                        
                        bo.ChangePartNum(ref SalesTS, true, vPartNum);
                        
                        bo.ChangePartNumMaster(
                        ref vPartNum, ref lSubstitutePartExist, ref lIsPhantom, ref uomCode,
                        SysRowID, rowType, salesKitView, removeKitComponents,
                        suppressUserPrompts, getPartXRefInfo, checkPartRevisionChange,
                        checkChangeKitParent, out cDeleteComponentsMessage,
                        out questionString, out cWarningMessage, out multipleMatch,
                        out promptToExplodeBOM, out cConfigPartMessage, out cSubPartMessage,
                        out explodeBOMerrMessage, out cmsgType, out multiSubsAvail,
                        out runOutQtyAvail, ref SalesTS);
                        
                        bo.ChangeSellingQtyMaster(
                        ref SalesTS, vQty, chkSellQty, negInvTest, chgSellQty,
                        chgDiscPer, suppressUserPrompts, lKeepUnitPrice, vPartNum,
                        pcWhseCode, pcBinNum, pcLotNum, pcAttributeSetID, pcDimCode,
                        pdDimConvFactor, out pcMessage, out pcNeqQtyAction,
                        out opWarningmsg, out cSellingQuantityChangedmsgText);
                        
                        
                        bo.ChangeUnitPrice(ref SalesTS);
                        
                        
  
                        bo.Update(ref SalesTS);

                        Msg += Environment.NewLine + "Order Line Updated: PartNum: " + vPartNum + ", Quantity: " + vQty + ", Unit Price: " + vUnitPrice;
                    }
                    else
                    {
                        // Handle case where order detail not found
                        throw new Exception("Order detail with PartNum " + vPartNum + " not found in OrderNum " + vOrderNum);
                    }
                }

                Status = "Updated Successfully";
                Response = SalesTS;
            });
        }
  
  
  
  }
  //txscope.Complete();
  
 
}

catch(Exception ex)
{
  //throw new Exception(ex.Message);
  Status = "Failed!";
  Msg = ex.Message;
  //txscope.Dispose();
}


        }
    }
}
