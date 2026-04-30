//----Added on 15th May 2025 
//This code is for Miscellaneous Shipment 
//Context is : to transfer first from rework and then from other bins, if any qty is remaining 

/* Some points to note are 

The CommitTransfer method may not have finalized the changes in time before the second execution starts, especially for the same part number or lot. This is a common issue in Epicor when using inventory transfers in tight loops or multiple rapid executions.
 
This code manually triggers Epicor’s deferred transaction processor, which:
1.Processes the records from the PartBinDeferred table.
2.Updates the actual PartBin and other inventory tables.
3.Finalizes the previous transfer, releasing the hold and allowing the next one to proceed.
 
*/
//this.PublishInfoMessage("Starting Inventory Transfer with Lot Matching...", Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
//Custom Function for Inventory Transfer
Func<Erp.Contracts.InvTransferSvcContract, Erp.Tablesets.MscShpDtRow, 
     string, string, string, string, string, string, string, string, 
     string, decimal, decimal, bool> ExecuteTransfer = (svc, tt, 
     fromWhse, fromWhseDesc, fromBin, fromBinDesc, toWhse, toWhseDesc, 
     toBin, toBinDesc, lotNum, transferQty, fromOnHandQty) =>
{
    try
    {
        string vMessage = "";
        bool ReqIp = false;
        string PTransPK = "";
        var dsTransfer = new Erp.Tablesets.InvTransferTableset();
        bool rowAdded = false;
        
        // Create new transfer record
        svc.GetNewInventoryTransfer("", ref dsTransfer);
        svc.GetTransferRecord(tt.PartNum, Guid.Empty, "", "", out rowAdded, ref dsTransfer);

        // Update transfer details
        var transferRow = dsTransfer.InvTrans.FirstOrDefault();
        if (transferRow == null) return false;

        transferRow.TransferQty = transferQty;
        transferRow.TrackingQty = transferQty;
        transferRow.FromLotNumber = lotNum;
        transferRow.ToLotNumber = tt.LotNum; // Always use the shipping lot number as destination
        transferRow.FromOnHandQty = fromOnHandQty;
        transferRow.ToOnHandQty = 0;
        transferRow.FromWarehouseCode = fromWhse;
        transferRow.FromWarehouseDesc = fromWhseDesc;
        transferRow.FromBinNum = fromBin;
        transferRow.FromBinDesc = fromBinDesc;
        transferRow.ToWarehouseCode = toWhse;
        transferRow.ToWarehouseDesc = toWhseDesc;
        transferRow.ToBinNum = toBin;
        transferRow.ToBinDesc = toBinDesc;
        transferRow.RowMod = "U";

        
        
        string negQtyAction, fromBinAction, toBinAction, msg;
        svc.MasterInventoryBinTests(ref dsTransfer, out negQtyAction, out msg, out fromBinAction, out msg, out toBinAction, out msg);
        
        svc.PreCommitTransfer(ref dsTransfer, out ReqIp);
        
        if(!string.IsNullOrEmpty(PTransPK))
        {
            this.PublishInfoMessage($"PartTransPK: {PTransPK}",Ice.Common.BusinessObjectMessageType.Information,Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
            PTransPK = "";
        }
        
        svc.CommitTransfer(ref dsTransfer, out vMessage, out PTransPK);
        Erp.Internal.Lib.DeferredUpdate libDeferredUpdate = new Erp.Internal.Lib.DeferredUpdate(Db);
        libDeferredUpdate.UpdPQDemand();

        
        return true;
    }
    catch (Exception ex)
    {
        this.PublishInfoMessage($"Error transferring from {fromBin} bin: {ex.Message}",Ice.Common.BusinessObjectMessageType.Error,Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
        return false;
    }
};


// Initialize variables
//string Message = "";
//bool RequiresUserInput = false;
//string PartTransPK = "";
string FromWhse = "";
string FromBin = "";
string LotNum = "";
string FromBinDesc = "";
string ToWhse = "";
string ToWhseDesc = "";
string ToBin = "MiscPckSlp";
string ToBinDesc = "";
bool DoNotTransfer = false;
string FromWhseDesc = "";


var invTransferSvc = Ice.Assemblies.ServiceRenderer.GetService<Erp.Contracts.InvTransferSvcContract>(Db);


foreach (var tt in ds.MscShpDt)
{
    try
    {
        this.PublishInfoMessage($"Processing Part: {tt.PartNum}, Lot: {tt.LotNum}, Qty: {tt.Quantity}", Ice.Common.BusinessObjectMessageType.Information,Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");

        
        var shippingHeader = Db.MscShpHd
            .Where(h => h.Company == tt.Company && h.PackNum == tt.PackNum)
            .FirstOrDefault();
        
        if (shippingHeader == null)
        {
            this.PublishInfoMessage($"Shipping header not found for PackNum: {tt.PackNum}", Ice.Common.BusinessObjectMessageType.Warning,Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
            continue;
        }

        //Source and destination warehouses logic from earlier
        switch (shippingHeader.ServRef1)
        {
            case "TOWARMSP":
                ToWhse = "WSCW";
                FromWhse = "MAULE";
                break;
            case "TOGIBSON":
                ToWhse = "MAULE";
                FromWhse = "WSCW";
                break;
            default:
                DoNotTransfer = true;
                this.PublishInfoMessage($"Unknown destination warehouse code: {shippingHeader.ServRef1}", Ice.Common.BusinessObjectMessageType.Warning,Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
                break;
        }

        if (DoNotTransfer)
        {
            DoNotTransfer = false;
            continue;
        }

        decimal remainingQty = tt.Quantity;
        decimal transferQty = 0;

        FromWhseDesc = Db.Warehse
            .Where(w => w.Company == tt.Company && w.WarehouseCode == FromWhse)
            .Select(w => w.Description)
            .FirstOrDefault() ?? "";
            
        ToWhseDesc = Db.Warehse
            .Where(w => w.Company == tt.Company && w.WarehouseCode == ToWhse)
            .Select(w => w.Description)
            .FirstOrDefault() ?? "";
            
        ToBinDesc = Db.WhseBin
            .Where(wb => wb.Company == tt.Company && wb.WarehouseCode == ToWhse && wb.BinNum == ToBin)
            .Select(wb => wb.Description)
            .FirstOrDefault() ?? "";

        // FIRST: Try to transfer from REWORK bin(s) with EXACT lot match
        
        var reworkBins = Db.PartBin
            .Join(Db.WhseBin, 
                pb => new { pb.Company, pb.WarehouseCode, pb.BinNum },
                wb => new { wb.Company, wb.WarehouseCode, wb.BinNum },
                (pb, wb) => new { PartBin = pb, BinDescription = wb.Description })
            .Where(x => x.PartBin.Company == tt.Company 
                     && x.PartBin.PartNum == tt.PartNum
                     && x.PartBin.WarehouseCode == FromWhse 
                     && x.PartBin.BinNum.ToUpper() == "REWORK"
                     && x.PartBin.LotNum == tt.LotNum // Exact lot match
                     && x.PartBin.OnhandQty > 0)
            .ToList();

      
        foreach (var reworkBin in reworkBins)
        {
            if (remainingQty <= 0) break;
            
            transferQty = Math.Min(remainingQty, reworkBin.PartBin.OnhandQty); //if on hand is less than remaininng then use up all of it from rework
            
            if (ExecuteTransfer(invTransferSvc, tt, FromWhse, FromWhseDesc, reworkBin.PartBin.BinNum, 
                            reworkBin.BinDescription, ToWhse, ToWhseDesc, ToBin, ToBinDesc, 
                            reworkBin.PartBin.LotNum, transferQty, reworkBin.PartBin.OnhandQty))
            {
                remainingQty -= transferQty; 
                this.PublishInfoMessage($"Transferred {transferQty} from REWORK bin (Lot: {reworkBin.PartBin.LotNum}), remaining: {remainingQty}", Ice.Common.BusinessObjectMessageType.Information,Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
            }
        }

        //If After Rework bin is consumed, qunatity is remaining then fetch from other bin
        if (remainingQty > 0)
        {
        
           
            
            var exactLotBin = Db.PartBin
                .Join(Db.WhseBin,
                    pb => new { pb.Company, pb.WarehouseCode, pb.BinNum },
                    wb => new { wb.Company, wb.WarehouseCode, wb.BinNum },
                    (pb, wb) => new { PartBin = pb, BinDescription = wb.Description })
                .Where(x => x.PartBin.Company == tt.Company
                         && x.PartBin.PartNum == tt.PartNum
                         && x.PartBin.WarehouseCode == FromWhse
                         && x.PartBin.LotNum == tt.LotNum
                         && x.PartBin.OnhandQty >= remainingQty
                         && x.PartBin.BinNum.ToUpper() != "REWORK")
                .Take(1)
                .FirstOrDefault();
        
            if (exactLotBin != null)
            {
                
                transferQty = remainingQty;
                
                this.PublishInfoMessage($"Before executing transfer : FromWarehouse = {FromWhse}, FromBin = {exactLotBin.PartBin.BinNum}, ToWarehouse = {ToWhse}, ToBin = {ToBin}, LotNum = {exactLotBin.PartBin.LotNum}, transferQty = {transferQty}, OnHandQty = {exactLotBin.PartBin.OnhandQty}",Ice.Common.BusinessObjectMessageType.Information,Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
                
                if (ExecuteTransfer(invTransferSvc, tt, FromWhse, FromWhseDesc, exactLotBin.PartBin.BinNum,
                                    exactLotBin.BinDescription, ToWhse, ToWhseDesc, ToBin, ToBinDesc,
                                    exactLotBin.PartBin.LotNum, transferQty, exactLotBin.PartBin.OnhandQty))
                {
                    remainingQty -= transferQty;
        
                    this.PublishInfoMessage($"Transferred {transferQty} from {exactLotBin.PartBin.BinNum} bin (Lot: {exactLotBin.PartBin.LotNum}), remaining: {remainingQty}",Ice.Common.BusinessObjectMessageType.Information,Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
                }
                
                
                
            }
        }

        if (remainingQty > 0)
        {
            this.PublishInfoMessage($"Warning: Could not transfer full quantity for Part: {tt.PartNum}, Lot: {tt.LotNum}. Remaining: {remainingQty}",  Ice.Common.BusinessObjectMessageType.Warning, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
        }
    }
    catch (Exception ex)
    {
        this.PublishInfoMessage($"Error processing Part: {tt.PartNum}, Lot: {tt.LotNum}. Error: {ex.Message}", Ice.Common.BusinessObjectMessageType.Error, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
    }
}

this.PublishInfoMessage("Inventory Transfer Process with Lot Matching Completed", Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");