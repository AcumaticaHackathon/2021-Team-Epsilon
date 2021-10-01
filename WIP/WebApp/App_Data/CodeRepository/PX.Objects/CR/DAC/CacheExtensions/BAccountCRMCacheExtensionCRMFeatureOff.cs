using System;
using System.Collections;
using PX.Data;
using PX.Data.EP;
using PX.Objects.AR;
using PX.Objects.CS;

namespace PX.Objects.CR
{
	#region Extension for handle CRM feature switch off
	/// <exclude/>
	[Serializable]
	[CRCacheIndependentPrimaryGraphList(new Type[]{
		typeof(AR.CustomerMaint),
		typeof(AR.CustomerMaint),
		typeof(AR.CustomerMaint),
		typeof(AP.VendorMaint),
		typeof(AP.VendorMaint),
		typeof(AP.VendorMaint),
		typeof(AR.CustomerMaint),
		typeof(AR.CustomerMaint)
	},
		new Type[]{
			typeof(Select<CR.BAccount, Where<CR.BAccount.bAccountID, Equal<Current<BAccount.bAccountID>>,
					And<Current<BAccount.viewInCrm>, Equal<True>>>>),
			typeof(Select<AR.Customer, Where<AR.Customer.bAccountID, Equal<Current<BAccount.bAccountID>>>>),
			typeof(Select<AR.Customer, Where<AR.Customer.bAccountID, Equal<Current<BAccountR.bAccountID>>>>),
			typeof(Select<AP.VendorR, Where<AP.VendorR.bAccountID, Equal<Current<BAccount.bAccountID>>>>),
			typeof(Select<AP.Vendor, Where<AP.Vendor.bAccountID, Equal<Current<BAccountR.bAccountID>>>>),
			typeof(Where<CR.BAccountR.bAccountID, Less<Zero>,
					And<BAccountR.type, Equal<BAccountType.vendorType>>>),
			typeof(Where<CR.BAccountR.bAccountID, Less<Zero>,
					And<BAccountR.type, Equal<BAccountType.customerType>>>),
			typeof(Select<CR.BAccount,
				Where2<Where<
					CR.BAccount.type, Equal<BAccountType.prospectType>,
					Or<CR.BAccount.type, Equal<BAccountType.customerType>,
					Or<CR.BAccount.type, Equal<BAccountType.vendorType>,
					Or<CR.BAccount.type, Equal<BAccountType.combinedType>>>>>,
						And<Where<CR.BAccount.bAccountID, Equal<Current<BAccount.bAccountID>>,
						Or<Current<BAccount.bAccountID>, Less<Zero>>>>>>)
		})]
	[PXHidden]
	public sealed class BAccountCRMCacheExtensionCRMFeatureOff : PXCacheExtension<BAccountCRM>
	{
		public static bool IsActive()
		{
			return (PXAccess.FeatureInstalled<FeaturesSet.customerModule>() == false);
		}
	}
	#endregion
}
