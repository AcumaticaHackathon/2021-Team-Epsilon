using System;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.CR;

namespace PX.Objects.CA
{
    public sealed class CATransferMultipleBaseCurrenciesRestriction : PXCacheExtension<CATransfer>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
        }

        [PXMergeAttributes(Method = MergeMethod.Append)]
        [PXRestrictor(typeof(Where<Current2<CATransfer.inAccountID>, IsNull,
                Or<CashAccount.baseCuryID, Equal<Current<inAccountCuryID>>>>), null)]
        public int? OutAccountID { get; set; }
        
        [PXMergeAttributes(Method = MergeMethod.Append)]
        [PXRestrictor(typeof(Where<Current2<CATransfer.outAccountID>, IsNull,
            Or<CashAccount.baseCuryID, Equal<Current<outAccountCuryID>>>>), null)]
        public int? InAccountID { get; set; }
        
        #region InAccountCuryID
        public new abstract class inAccountCuryID : PX.Data.BQL.BqlString.Field<inAccountCuryID> { }

        [PXString]
        [PXFormula(typeof(Selector<CATransfer.inAccountID, CashAccount.baseCuryID>))]
        public string InAccountCuryID { get; set; }
        #endregion
        #region OutAccountCuryID
        public new abstract class outAccountCuryID : PX.Data.BQL.BqlString.Field<outAccountCuryID> { }

        [PXString]
        [PXFormula(typeof(Selector<CATransfer.outAccountID, CashAccount.baseCuryID>))]
        public string OutAccountCuryID { get; set; }
        #endregion
    }
}