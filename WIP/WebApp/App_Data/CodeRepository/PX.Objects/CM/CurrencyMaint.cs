using System;
using System.Collections;
using PX.Data;
using PX.Objects.Common.EntityInUse;
using PX.Objects.GL;

namespace PX.Objects.CM
{
	public class CurrencyMaint : PXGraph<CurrencyMaint, CurrencyList>
	{
		[PXCopyPasteHiddenView]
		public PXSelect<CurrencyList> CuryListRecords;
		public PXSelect<Currency, Where<Currency.curyID, Equal<Current<CurrencyList.curyID>>>> CuryRecords;

		public virtual IEnumerable curyRecords()
		{
			PXResultset<Currency> res = PXSelect<Currency,Where<Currency.curyID, Equal<Current<CurrencyList.curyID>>>>.Select(this);
			if (res.Count == 0 && CuryListRecords.Current != null)
			{
				Currency newrow = new Currency();
				newrow.CuryID = CuryListRecords.Current.CuryID;
				res.Add(new PXResult<Currency>(newrow));
			}
			return res;
		}

		public PXSetup<Company> company;

		public CurrencyMaint()
		{
			Company setup = company.Current;
			if (string.IsNullOrEmpty(setup.BaseCuryID))
			{
				throw new PXSetupNotEnteredException(ErrorMessages.SetupNotEntered, typeof(Company), PXMessages.LocalizeNoPrefix(CS.Messages.BranchMaint));
			}
			PXUIFieldAttribute.SetVisible<CurrencyList.isFinancial>(CuryListRecords.Cache, null, PXAccess.FeatureInstalled<CS.FeaturesSet.multicurrency>());
		}

		protected virtual void _(Events.FieldDefaulting<Currency.decimalPlaces> e)
		{
			// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs
			e.Cache.SetValue(e.Row, typeof(Currency.decimalPlaces).Name, (short)2);
		}

		protected virtual void _(Events.FieldVerifying<CurrencyList.decimalPlaces> e)
		{
			CurrencyList currencyList = e.Row as CurrencyList;
			if (currencyList == null) return;

			WebDialogResult wdr =
				CuryListRecords.Ask(
					CS.Messages.Warning,
					CS.Messages.ChangingCurrencyPrecisionWarning,
					MessageButtons.YesNo,
					MessageIcon.Warning);

			e.NewValue = wdr == WebDialogResult.Yes 
				? e.NewValue 
				: currencyList.DecimalPlaces;
		}

		protected virtual void _(Events.RowUpdated<CurrencyList> e)
		{
			var row = e.Row as CurrencyList;
			if (row == null)
				return;

			if (CuryRecords.Cache.Current != null && row.IsFinancial != true)
			{
				CuryRecords.Cache.Delete(CuryRecords.Cache.Current);
			}
			if (row.IsFinancial == true && CuryRecords.Cache.Current != null && CuryRecords.Current.tstamp == null)
			{
				CuryRecords.Cache.Insert(CuryRecords.Cache.Current);
			}
		}

		protected void _(Events.RowSelected<CurrencyList> e)
		{
			CurrencyList currencyList = e.Row as CurrencyList;
			if (currencyList == null) return;

			if (EntityInUseHelper.IsEntityInUse<CurrencyInUse>(currencyList.CuryID))
			{
				PXUIFieldAttribute.SetEnabled<Currency.decimalPlaces>(e.Cache, currencyList, false);
			}

			if (currencyList.CuryID == company.Current.BaseCuryID) 
			{
				CuryListRecords.Cache.AllowDelete = false;
				CuryRecords.Cache.AllowDelete = false;
				PXUIFieldAttribute.SetEnabled<CurrencyList.isActive>(e.Cache, currencyList, false);
				PXUIFieldAttribute.SetEnabled<CurrencyList.isFinancial>(e.Cache, currencyList, false);
			}
		}
		protected void _(Events.RowDeleting<CurrencyList> e)
		{
			CurrencyList currencyList = e.Row as CurrencyList;

			if (currencyList.CuryID == company.Current.BaseCuryID)
			{
				throw new PXException(Messages.CurrencyCannotBeDeletedBecauseItIsBaseCurrency, currencyList.CuryID);
;			}
		}

		public struct SettingsByFeatures 
		{
			public bool showRealGL;
			public bool reqRealGL;

			public bool showUnrealGL;
			public bool reqUnrealGL;

			public bool showAPProvAcct;
			public bool reqAPProvAcct;

			public bool showRevalGL;
			public bool reqRevalGL;

			public bool showTransGL;
			public bool reqTransGL;

			public bool showRoundingGL;
			public bool reqRoundingGL;
			public bool showRoundingLimit;
		}

		protected virtual SettingsByFeatures GetSettingsByFeatures(string curyID, string baseCuryID, bool? isFinancial)
		{
			// bool f_subAccount = PXAccess.FeatureInstalled<CS.FeaturesSet.subAccount>();
			bool f_multipleBaseCurrencies = PXAccess.FeatureInstalled<CS.FeaturesSet.multipleBaseCurrencies>();
			bool f_multicurrency = PXAccess.FeatureInstalled<CS.FeaturesSet.multicurrency>();
			bool f_invoiceRounding = PXAccess.FeatureInstalled<CS.FeaturesSet.invoiceRounding>();
			bool f_finStatementCurTranslation = PXAccess.FeatureInstalled<CS.FeaturesSet.finStatementCurTranslation>();
			bool f_finStandart = PXAccess.FeatureInstalled<CS.FeaturesSet.financialStandard>();

			bool showRealGL = false,		reqRealGL = false;
			bool showUnrealGL = false,		reqUnrealGL = false;
			bool showAPProvAcct = false,	reqAPProvAcct = false;
			bool showRevalGL = false,		reqRevalGL = false;
			bool showTransGL = false,		reqTransGL = false;
			bool showRoundingGL = true,	reqRoundingGL = false, showRoundingLimit = false;

			if (f_multipleBaseCurrencies) // If the Multiple Base Currencies feature is on
			{
				// then all sections with accounts should be visible and mandatory for all currencies; 
				showRealGL = true;
				reqRealGL = true;

				showUnrealGL = true;
				reqUnrealGL = true;

				// only fields from the UNREALIZED GAIN AND LOSS PROVISIONING ACCOUNTS section should be optional. 
				showAPProvAcct = true;
				reqAPProvAcct = false;

				showRevalGL = true;
				reqRevalGL = true;

				// Translation Gain/Loss Account/Sub depends on Translation of Financial Statements feature
				showTransGL = f_finStatementCurTranslation;
				reqTransGL = f_finStatementCurTranslation;

				reqRoundingGL = true;
				showRoundingLimit = isFinancial == true;
			}
			else // If the Multiple Base Currencies feature is off
			{
				//If the Multi-Currency Accounting feature is on, then
				if (f_multicurrency)
				{
					// The REALIZED GAIN AND LOSS ACCOUNTS section should be visible; as previously, its fields should be mandatory for any currency.
					showRealGL = true;
					reqRealGL = true;

					// The UNREALIZED GAIN AND LOSS ACCOUNTS section should be visible for non-base currencies and hidden for the base one; for non-base currencies, as previously, its fields should be mandatory.
					showUnrealGL = curyID != baseCuryID;
					reqUnrealGL = true;

					// The REVALUATION GAIN AND LOSS ACCOUNTS section should be visible for non-base currencies and hidden for the base one; for non-base currencies, as previously, its fields should be mandatory.
					showRevalGL = curyID != baseCuryID;
					reqRevalGL = true;

					// If the Translation of Financial Statements feature is ON, 
					// then the TRANSLATION GAIN AND LOSS ACCOUNTS section should be visible; as previously, its fields should be mandatory for any currency.
					if (f_finStatementCurTranslation)
					{
						showTransGL = true;
						reqTransGL = true;
					}

					// The UNREALIZED GAIN AND LOSS PROVISIONING ACCOUNTS section should be visible; as previously, its fields should be optional.
					showAPProvAcct = curyID != baseCuryID;
					reqAPProvAcct = false;

					// The ROUNDING GAIN AND LOSS ACCOUNTS section should be visible; as previously, its fields should be mandatory for any currency.
					showRoundingGL = true;
					reqRoundingGL = true;
				}
				else // if the Multi-Currency Accounting feature is off, 
				{ 
				  // then the REALIZED GAIN AND LOSS ACCOUNTS, 
					showRealGL = false;
					reqRealGL = false;
					// UNREALIZED GAIN AND LOSS ACCOUNTS, 
					showUnrealGL = false;
					reqUnrealGL = false;
					// UNREALIZED GAIN AND LOSS PROVISIONING ACCOUNTS, 
					showAPProvAcct = false;
					reqAPProvAcct = false;
					// REVALUATION GAIN AND LOSS ACCOUNTS 
					showRevalGL = false;
					reqRevalGL = false;
					// and TRANSACTION GAIN AND LOSS ACCOUNTS 
					// sections shouldn't be visible. 
					showTransGL = false;
					reqTransGL = false;

					if (curyID != baseCuryID) 
					{
						reqRoundingGL = false;
					}

					// The ROUNDING GAIN AND LOSS ACCOUNTS section should be visible
					if (f_invoiceRounding) // If the Invoice Rounding feature is ON, then the section's fields should be mandatory for the base currency, optional for non-base currency. 
					{
						reqRoundingGL = curyID == baseCuryID;
					}
					else // If the Invoice Rounding feature is OFF, then the section's fields should be optional for any currency 
					{
						reqRoundingGL = false;
					}
				}
				if ((f_finStandart || f_invoiceRounding) && curyID == baseCuryID)
				{
					showRoundingLimit = true;
				}
			}

			return new SettingsByFeatures
			{
				showRealGL = showRealGL,
				reqRealGL = reqRealGL && showRealGL,
				showUnrealGL = showUnrealGL,
				reqUnrealGL = reqUnrealGL && showUnrealGL,
				showAPProvAcct = showAPProvAcct,
				reqAPProvAcct = reqAPProvAcct && showAPProvAcct,
				showRevalGL = showRevalGL,
				reqRevalGL = reqRevalGL && showRevalGL,
				showTransGL = showTransGL,
				reqTransGL = reqTransGL && showTransGL,
				showRoundingGL = showRoundingGL,
				reqRoundingGL = reqRoundingGL && showRoundingGL,
				showRoundingLimit = showRoundingLimit
			};
		}

		protected void _(Events.RowSelected<Currency> e)
		{
			var cache = e.Cache;
			CurrencyList currencyListRow = CuryListRecords.Current;
			Currency currencyRow = (Currency)e.Row;
			PXUIFieldAttribute.SetEnabled(cache, currencyRow, currencyListRow != null && currencyListRow.IsFinancial == true);

			SettingsByFeatures f_setting = GetSettingsByFeatures(currencyRow.CuryID, company.Current.BaseCuryID, currencyListRow?.IsFinancial ?? false);

			#region Realized Gain and Loss Accounts
			PXUIFieldAttribute.SetVisible<Currency.realGainAcctID>(cache, currencyRow, f_setting.showRealGL);
			PXUIFieldAttribute.SetVisible<Currency.realGainSubID>(cache, currencyRow, f_setting.showRealGL);
			PXUIFieldAttribute.SetVisible<Currency.realLossAcctID>(cache, currencyRow, f_setting.showRealGL);
			PXUIFieldAttribute.SetVisible<Currency.realLossSubID>(cache, currencyRow, f_setting.showRealGL);

			PXUIFieldAttribute.SetRequired<Currency.realGainAcctID>(cache, f_setting.reqRealGL);
			PXUIFieldAttribute.SetRequired<Currency.realGainSubID>(cache, f_setting.reqRealGL);
			PXUIFieldAttribute.SetRequired<Currency.realLossAcctID>(cache, f_setting.reqRealGL);
			PXUIFieldAttribute.SetRequired<Currency.realLossSubID>(cache, f_setting.reqRealGL);
			#endregion
			#region Unrealized Gain and Loss Accounts
			PXUIFieldAttribute.SetVisible<Currency.unrealizedGainAcctID>(cache, currencyRow, f_setting.showUnrealGL);
			PXUIFieldAttribute.SetVisible<Currency.unrealizedGainSubID>(cache, currencyRow, f_setting.showUnrealGL);
			PXUIFieldAttribute.SetVisible<Currency.unrealizedLossAcctID>(cache, currencyRow, f_setting.showUnrealGL);
			PXUIFieldAttribute.SetVisible<Currency.unrealizedLossSubID>(cache, currencyRow, f_setting.showUnrealGL);

			PXUIFieldAttribute.SetRequired<Currency.unrealizedGainAcctID>(cache, f_setting.reqUnrealGL);
			PXUIFieldAttribute.SetRequired<Currency.unrealizedGainSubID>(cache, f_setting.reqUnrealGL);
			PXUIFieldAttribute.SetRequired<Currency.unrealizedLossAcctID>(cache, f_setting.reqUnrealGL);
			PXUIFieldAttribute.SetRequired<Currency.unrealizedLossSubID>(cache, f_setting.reqUnrealGL);
			#endregion
			#region Unrealized Gain and Loss Provisioning Accounts
			PXUIFieldAttribute.SetVisible<Currency.aPProvAcctID>(cache, currencyRow, f_setting.showAPProvAcct);
			PXUIFieldAttribute.SetVisible<Currency.aPProvSubID>(cache, currencyRow, f_setting.showAPProvAcct);
			PXUIFieldAttribute.SetVisible<Currency.aRProvAcctID>(cache, currencyRow, f_setting.showAPProvAcct);
			PXUIFieldAttribute.SetVisible<Currency.aRProvSubID>(cache, currencyRow, f_setting.showAPProvAcct);

			PXUIFieldAttribute.SetRequired<Currency.aPProvAcctID>(cache, f_setting.reqAPProvAcct);
			PXUIFieldAttribute.SetRequired<Currency.aPProvSubID>(cache, f_setting.reqAPProvAcct);
			PXUIFieldAttribute.SetRequired<Currency.aRProvAcctID>(cache, f_setting.reqAPProvAcct);
			PXUIFieldAttribute.SetRequired<Currency.aRProvSubID>(cache, f_setting.reqAPProvAcct);
			#endregion
			#region Revaluation Gain and Loss Accounts
			PXUIFieldAttribute.SetVisible<Currency.revalGainAcctID>(cache, currencyRow, f_setting.showRevalGL);
			PXUIFieldAttribute.SetVisible<Currency.revalGainSubID>(cache, currencyRow, f_setting.showRevalGL);
			PXUIFieldAttribute.SetVisible<Currency.revalLossAcctID>(cache, currencyRow, f_setting.showRevalGL);
			PXUIFieldAttribute.SetVisible<Currency.revalLossSubID>(cache, currencyRow, f_setting.showRevalGL);

			PXUIFieldAttribute.SetRequired<Currency.revalGainAcctID>(cache, f_setting.reqRevalGL);
			PXUIFieldAttribute.SetRequired<Currency.revalGainSubID>(cache, f_setting.reqRevalGL);
			PXUIFieldAttribute.SetRequired<Currency.revalLossAcctID>(cache, f_setting.reqRevalGL);
			PXUIFieldAttribute.SetRequired<Currency.revalLossSubID>(cache, f_setting.reqRevalGL);
			#endregion
			#region Translation Gain and Loss Accounts
			PXUIFieldAttribute.SetVisible<Currency.translationGainAcctID>(cache, currencyRow, f_setting.showTransGL);
			PXUIFieldAttribute.SetVisible<Currency.translationGainSubID>(cache, currencyRow, f_setting.showTransGL);
			PXUIFieldAttribute.SetVisible<Currency.translationLossAcctID>(cache, currencyRow, f_setting.showTransGL);
			PXUIFieldAttribute.SetVisible<Currency.translationLossSubID>(cache, currencyRow, f_setting.showTransGL);

			PXUIFieldAttribute.SetRequired<Currency.translationGainAcctID>(cache, f_setting.reqTransGL);
			PXUIFieldAttribute.SetRequired<Currency.translationGainSubID>(cache, f_setting.reqTransGL);
			PXUIFieldAttribute.SetRequired<Currency.translationLossAcctID>(cache, f_setting.reqTransGL);
			PXUIFieldAttribute.SetRequired<Currency.translationLossSubID>(cache, f_setting.reqTransGL);
			#endregion
			#region Rounding Gain and Loss Accounts
			PXUIFieldAttribute.SetVisible<Currency.roundingGainAcctID>(cache, currencyRow, f_setting.showRoundingGL);
			PXUIFieldAttribute.SetVisible<Currency.roundingGainSubID>(cache, currencyRow, f_setting.showRoundingGL);
			PXUIFieldAttribute.SetVisible<Currency.roundingLossAcctID>(cache, currencyRow, f_setting.showRoundingGL);
			PXUIFieldAttribute.SetVisible<Currency.roundingLossSubID>(cache, currencyRow, f_setting.showRoundingGL);

			PXUIFieldAttribute.SetRequired<Currency.roundingGainAcctID>(cache, f_setting.reqRoundingGL);
			PXUIFieldAttribute.SetRequired<Currency.roundingGainSubID>(cache, f_setting.reqRoundingGL);
			PXUIFieldAttribute.SetRequired<Currency.roundingLossAcctID>(cache, f_setting.reqRoundingGL);
			PXUIFieldAttribute.SetRequired<Currency.roundingLossSubID>(cache, f_setting.reqRoundingGL);
			#endregion
			#region Rounding Settings
			PXUIFieldAttribute.SetEnabled<Currency.aRInvoiceRounding>(cache, currencyRow, currencyRow.UseARPreferencesSettings == false);
			PXUIFieldAttribute.SetEnabled<Currency.aRInvoicePrecision>(cache, currencyRow, currencyRow.UseARPreferencesSettings == false && currencyRow.ARInvoiceRounding != CS.RoundingType.Currency);
			PXUIFieldAttribute.SetEnabled<Currency.aPInvoiceRounding>(cache, currencyRow, currencyRow.UseAPPreferencesSettings == false);
			PXUIFieldAttribute.SetEnabled<Currency.aPInvoicePrecision>(cache, currencyRow, currencyRow.UseAPPreferencesSettings == false && currencyRow.APInvoiceRounding != CS.RoundingType.Currency);
			PXUIFieldAttribute.SetVisible<Currency.roundingLimit>(e.Cache, currencyRow, f_setting.showRoundingLimit);
			#endregion
		}

		protected void _(Events.RowPersisting<Currency> e)
		{
			var row = e.Row as Currency;
			if (row == null) return;

			CurrencyList currencyListRow = CuryListRecords.Current;
			if (currencyListRow != null && currencyListRow.IsFinancial == false && e.Operation == PXDBOperation.Insert)
			{
				e.Cancel = true;
				return;
			}
			var cache = e.Cache;
			SettingsByFeatures f_setting = GetSettingsByFeatures(row.CuryID, company.Current.BaseCuryID, currencyListRow?.IsFinancial ?? false);

			#region SetPersistingCheck
			PXDefaultAttribute.SetPersistingCheck<Currency.realGainAcctID>(cache, row, f_setting.reqRealGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.realGainSubID>(cache, row, f_setting.reqRealGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.realLossAcctID>(cache, row, f_setting.reqRealGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.realLossSubID>(cache, row, f_setting.reqRealGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);

			PXDefaultAttribute.SetPersistingCheck<Currency.unrealizedGainAcctID>(cache, row, f_setting.reqUnrealGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.unrealizedGainSubID>(cache, row, f_setting.reqUnrealGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.unrealizedLossAcctID>(cache, row, f_setting.reqUnrealGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.unrealizedLossSubID>(cache, row, f_setting.reqUnrealGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);

			PXDefaultAttribute.SetPersistingCheck<Currency.aPProvAcctID>(cache, row, f_setting.reqAPProvAcct ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.aPProvSubID>(cache, row, f_setting.reqAPProvAcct ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.aRProvAcctID>(cache, row, f_setting.reqAPProvAcct ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.aRProvSubID>(cache, row, f_setting.reqAPProvAcct ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);

			PXDefaultAttribute.SetPersistingCheck<Currency.revalGainAcctID>(cache, row, f_setting.reqRevalGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.revalGainSubID>(cache, row, f_setting.reqRevalGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.revalLossAcctID>(cache, row, f_setting.reqRevalGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.revalLossSubID>(cache, row, f_setting.reqRevalGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);

			PXDefaultAttribute.SetPersistingCheck<Currency.translationGainAcctID>(cache, row, f_setting.reqTransGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.translationGainSubID>(cache, row, f_setting.reqTransGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.translationLossAcctID>(cache, row, f_setting.reqTransGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.translationLossSubID>(cache, row, f_setting.reqTransGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);

			PXDefaultAttribute.SetPersistingCheck<Currency.roundingGainAcctID>(cache, row, f_setting.reqRoundingGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.roundingGainSubID>(cache, row, f_setting.reqRoundingGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.roundingLossAcctID>(cache, row, f_setting.reqRoundingGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.roundingLossSubID>(cache, row, f_setting.reqRoundingGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);

			PXDefaultAttribute.SetPersistingCheck<Currency.aRInvoiceRounding>(cache, row, f_setting.reqTransGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.aRInvoicePrecision>(cache, row, f_setting.reqTransGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.aPInvoiceRounding>(cache, row, f_setting.reqTransGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXDefaultAttribute.SetPersistingCheck<Currency.aPInvoicePrecision>(cache, row, f_setting.reqTransGL ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			#endregion
		}

		[PXDBString(5, IsUnicode = true, IsKey = true, InputMask = ">LLLLL")]
		[PXDefault()]
		[PXUIField(DisplayName = "Currency ID", Visibility = PXUIVisibility.SelectorVisible)]
		[PXSelector(typeof(Search<CurrencyList.curyID>))]
		[PX.Data.EP.PXFieldDescription]
		protected virtual void _(Events.CacheAttached<CurrencyList.curyID> a) { }

		#region Currency Control Accounts
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[AvoidControlAccounts]
		protected virtual void  _(Events.CacheAttached<Currency.realGainAcctID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[AvoidControlAccounts]
		protected virtual void _(Events.CacheAttached<Currency.realLossAcctID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[AvoidControlAccounts]
		protected virtual void _(Events.CacheAttached<Currency.revalGainAcctID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[AvoidControlAccounts]
		protected virtual void _(Events.CacheAttached<Currency.revalLossAcctID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[AvoidControlAccounts]
		protected virtual void _(Events.CacheAttached<Currency.aRProvAcctID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[AvoidControlAccounts]
		protected virtual void _(Events.CacheAttached<Currency.aPProvAcctID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIRequired(typeof(False))]
		[AvoidControlAccounts]
		protected virtual void _(Events.CacheAttached<Currency.translationGainAcctID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIRequired(typeof(False))]
		protected virtual void _(Events.CacheAttached<Currency.translationGainSubID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIRequired(typeof(False))]
		[AvoidControlAccounts]
		protected virtual void _(Events.CacheAttached<Currency.translationLossAcctID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIRequired(typeof(False))]
		protected virtual void _(Events.CacheAttached<Currency.translationLossSubID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[AvoidControlAccounts]
		protected virtual void _(Events.CacheAttached<Currency.unrealizedGainAcctID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[AvoidControlAccounts]
		protected virtual void _(Events.CacheAttached<Currency.unrealizedLossAcctID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[AvoidControlAccounts]
		protected virtual void _(Events.CacheAttached<Currency.roundingGainAcctID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[AvoidControlAccounts]
		protected virtual void _(Events.CacheAttached<Currency.roundingLossAcctID> e) { }
		#endregion
	}
}
