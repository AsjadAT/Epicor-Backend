//--Added on 20th May 2025
//If multiple BO needed to be called then you need to use Transaction scope and wrap the entire code in it 


Func<string, string, string, string, string, decimal, bool> 
QuantityAdjust = (Part,UOM,WareHouseCode, Bin, Lot, qtyToAdjust) =>
{
    CallService<Erp.Contracts.InventoryQtyAdjSvcContract>(qtyAdjSvc =>
    {
        
        var qtyAdjTS = new Erp.Tablesets.InventoryQtyAdjTableset();
        qtyAdjTS = qtyAdjSvc.GetInventoryQtyAdj(Part, UOM); 

        var adj = qtyAdjTS.InventoryQtyAdj.First();
        adj.PartNum = Part;
        adj.WareHseCode = WareHouseCode;
        adj.BinNum = Bin;
        adj.LotNum = Lot;
        adj.AdjustQuantity = -qtyToAdjust;
        adj.ReasonCode = "RBAC";
        adj.TransDate = DateTime.Now;
        adj.EnableSN = false;
        adj.RowMod = "A";

        string partTransPK;
        bool requireIP;
        qtyAdjSvc.PreSetInventoryQtyAdj(ref qtyAdjTS, out requireIP);
        qtyAdjSvc.SetInventoryQtyAdj(ref qtyAdjTS, out partTransPK);
    });

    return true;
};


QuantityAdjust("AX001","EA", "MAIN", "TESTBIN", TESTLOT, 13);