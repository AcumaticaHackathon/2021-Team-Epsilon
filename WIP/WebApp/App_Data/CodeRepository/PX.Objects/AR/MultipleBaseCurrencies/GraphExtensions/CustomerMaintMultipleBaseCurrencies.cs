using System;
using PX.Data;
using PX.Objects.CS;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using System.Linq;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.GL;
using PX.Objects.CA;

namespace PX.Objects.AR
{
	public class CustomerMaintMultipleBaseCurrencies : PXGraphExtension<CustomerMaint.PaymentDetailsExt, CustomerMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		protected virtual void _(Events.RowPersisting<Customer> e)
		{
			if (e.Row == null)
				return;

			if (Base.CurrentCustomer.Cache.GetStatus(e.Row) == PXEntryStatus.Updated)
			{
				object parentBAccountID = e.Row.ParentBAccountID;
				try
				{
					Base.CurrentCustomer.Cache.RaiseFieldVerifying<Customer.parentBAccountID>(e.Row, ref parentBAccountID);
				}
				catch (PXSetPropertyException ex)
				{
					Base.CurrentCustomer.Cache.RaiseExceptionHandling<Customer.parentBAccountID>(e.Row, parentBAccountID, ex);
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<Customer, Customer.cOrgBAccountID> e)
		{
			if (e.NewValue != null && (int)e.NewValue != 0 && e.Row.BaseCuryID != null)
			{
				var groupBaseCury = PXOrgAccess.GetBaseCuryID((int)e.NewValue);

				if (e.Row.BaseCuryID != groupBaseCury
					&& SelectFrom<ARHistory>
						.Where<ARHistory.customerID.IsEqual<Customer.bAccountID.FromCurrent>>
						.View.SelectSingleBound(Base, new object[]{e.Row}, null).Any())
				{
					e.NewValue = PXOrgAccess.GetCD((int)e.NewValue);

					throw new PXSetPropertyException(Messages.EntityCannotBeAssociated, PXErrorLevel.Error,
							e.Row.BaseCuryID,
							e.Row.AcctCD);
				}

				Customer childCustomer =
					SelectFrom<Customer>
					.Where<Customer.parentBAccountID.IsEqual<@P.AsInt>
					.And<Customer.consolidateToParent.IsEqual<True>
					.And<Customer.baseCuryID.IsNotEqual<@P.AsString>>>>
					.View.SelectSingleBound(Base, null, e.Row.BAccountID, groupBaseCury);

				if (childCustomer != null)
				{
					e.NewValue = PXOrgAccess.GetCD((int)e.NewValue);

					throw new PXSetPropertyException(Messages.CannotBeUsedAsParent, PXErrorLevel.Error,
							e.Row.AcctCD,
							PXOrgAccess.GetCD(childCustomer.COrgBAccountID),
							childCustomer.AcctCD);
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<Customer, Customer.parentBAccountID> e)
		{
			if (e.NewValue == null)
				return;

			BAccountR bAccount = PXSelectorAttribute.Select<Customer.parentBAccountID>(e.Cache, e.Row, (int)e.NewValue) as BAccountR;

			if (bAccount != null
				&& bAccount.BaseCuryID != e.Row.BaseCuryID
				&& e.Row.ConsolidateToParent == true)
			{
				e.NewValue = bAccount.AcctCD;

				throw new PXSetPropertyException(Messages.CannotBeUsedAsParent, PXErrorLevel.Error,
							bAccount.AcctCD,
							PXOrgAccess.GetCD(e.Row.COrgBAccountID),
							e.Row.AcctCD);
			}			
		}

		protected virtual void _(Events.FieldUpdated<Customer, Customer.cOrgBAccountID> e)
		{
			e.Row.BaseCuryID = PXOrgAccess.GetBaseCuryID(e.Row.COrgBAccountID) ?? Base.Accessinfo.BaseCuryID;

			if (e.Row.ParentBAccountID != null)
			{
				object parentBAccountID = e.Row.ParentBAccountID;
				try
				{
					e.Cache.RaiseFieldVerifying<Customer.parentBAccountID>(e.Row, ref parentBAccountID);
				}
				catch (PXSetPropertyException ex)
				{
					e.Cache.RaiseExceptionHandling<Customer.parentBAccountID>(e.Row, parentBAccountID, ex);
				}
			}
		}

		protected virtual void _(Events.RowUpdated<Customer> e)
		{
			if (e.OldRow.BaseCuryID != e.Row.BaseCuryID)
			{
				foreach (CustomerPaymentMethod paymentMethod in SelectFrom<CustomerPaymentMethod>
					.Where<CustomerPaymentMethod.bAccountID.IsEqual<Customer.bAccountID.FromCurrent>>.View.SelectMultiBound(Base, new object[] { e.Row }, null))
				{
					paymentMethod.CashAccountID = null;
					if (Base.Caches<CustomerPaymentMethod>().GetStatus(paymentMethod) == PXEntryStatus.Notchanged)
						Base.Caches<CustomerPaymentMethod>().MarkUpdated(paymentMethod);
				}

				foreach (CustomerPaymentMethodInfo paymentMethod in Base.GetExtension<CustomerMaint.PaymentDetailsExt>().PaymentMethods.Select()
					.Where(m => ((CustomerPaymentMethodInfo)m).BAccountID != null))
				{
					paymentMethod.CashAccountID = null;
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<Customer, Customer.consolidateToParent> e)
		{
			if ((bool)e.NewValue)
			{
				object parentBAccountID = e.Row.ParentBAccountID;
				try
				{
					Base.CurrentCustomer.Cache.RaiseFieldVerifying<Customer.parentBAccountID>(e.Row, ref parentBAccountID);
				}
				catch (PXSetPropertyException ex)
				{
					Base.CurrentCustomer.Cache.RaiseExceptionHandling<Customer.parentBAccountID>(e.Row, parentBAccountID, ex);
				}
			}
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<BAccountR.baseCuryID.IsEqual<Customer.baseCuryID.FromCurrent>
			.Or<Customer.consolidateToParent.FromCurrent.IsEqual<False>>>),
			Messages.CannotBeUsedAsParentRestrictor)]
		protected void Customer_ParentBAccountID_CacheAttached(PXCache sender){}

		[CashAccount(null, typeof(Search2<
					CashAccount.cashAccountID,
				InnerJoin<PaymentMethodAccount,
					On<PaymentMethodAccount.cashAccountID, Equal<CashAccount.cashAccountID>,
					And<PaymentMethodAccount.useForAR, Equal<True>,
					And<PaymentMethodAccount.paymentMethodID, Equal<Current<CustomerPaymentMethod.paymentMethodID>>>>>>,
				Where2<
					Match<Current<AccessInfo.userName>>,
					And<Where<CashAccount.baseCuryID, Equal<Current<Customer.baseCuryID>>>>>>),
				DisplayName = "Cash Account",
				Visibility = PXUIVisibility.Visible,
				Enabled = false)]
		[PXDefault(typeof(Search<CA.PaymentMethod.defaultCashAccountID, Where<CA.PaymentMethod.paymentMethodID, Equal<Current<CustomerPaymentMethod.paymentMethodID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<CustomerPaymentMethod.cashAccountID> e) { }

	}
}
