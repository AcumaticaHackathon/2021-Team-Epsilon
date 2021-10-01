using System;
using PX.Data;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.Objects.IN;

namespace PX.Objects.AR
{
    public sealed class ARTranMultipleBaseCurrencies: PXCacheExtension<ARTran>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
        }
        #region InventoryID
        public abstract class curyInventoryCD : PX.Data.BQL.BqlString.Field<curyInventoryCD> { }
        [PXString]
        [PXSelector(typeof(Search<InventoryItemCurySettings.curyID, 
            Where<InventoryItemCurySettings.inventoryID, Equal<Current<ARTran.inventoryID>>>>))]
        [PXFormula(typeof(Current<ARInvoiceMultipleBaseCurrenciesRestriction.branchBaseCuryID>))]        
        public string CuryInventoryCD
        {
            get;
            set;
        }
        #endregion
        
        #region AccruedCost
        public abstract class accruedCost : PX.Data.BQL.BqlDecimal.Field<accruedCost> { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXFormula(typeof(Switch<
            Case<Where<ARTran.accrueCost, Equal<True>, And<ARTran.costBasis, Equal<CostBasisOption.standardCost>>>, Mult<ARTran.baseQty, Selector<curyInventoryCD, InventoryItemCurySettings.stdCost>>>,
            decimal0>))]
        public Decimal? AccruedCost { get; set; }

        #endregion

    }
}