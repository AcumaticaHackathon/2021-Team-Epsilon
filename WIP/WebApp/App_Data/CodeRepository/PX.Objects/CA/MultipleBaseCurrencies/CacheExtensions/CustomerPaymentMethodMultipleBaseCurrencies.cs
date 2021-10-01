using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.CA
{
	public sealed class CustomerPaymentMethodMultipleBaseCurrencies : PXCacheExtension<CustomerPaymentMethod>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		#region BaseCuryID
		public abstract class baseCuryID : PX.Data.BQL.BqlString.Field<baseCuryID> { }

		[PXString(5, IsUnicode = true)]
		[PXFormula(typeof(Selector<CustomerPaymentMethod.bAccountID, BAccountR.baseCuryID>))]
		[PXUIField(DisplayName = "Currency")]
		public string BaseCuryID { get; set; }
		#endregion

		#region CashAccountID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<CashAccount.baseCuryID, Equal<Current<CustomerPaymentMethodMultipleBaseCurrencies.baseCuryID>>>), null)]
		public int? CashAccountID { get; set; }
		#endregion
	}
}
