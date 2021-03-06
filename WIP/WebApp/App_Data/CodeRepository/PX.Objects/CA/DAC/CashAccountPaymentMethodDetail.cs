using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;

namespace PX.Objects.CA
{
	[Serializable]
	[PXCacheName(Messages.CashAccountPaymentMethodDetail)]
	public partial class CashAccountPaymentMethodDetail : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<CashAccountPaymentMethodDetail>.By<accountID, paymentMethodID, detailID>
		{
			public static CashAccountPaymentMethodDetail Find(PXGraph graph, int? accountID, string paymentMethodID, string detailID) => FindBy(graph, accountID, paymentMethodID, detailID);
		}

		public static class FK
		{
			public class CashAccount : CA.CashAccount.PK.ForeignKeyOf<CashAccountPaymentMethodDetail>.By<accountID> { }
			public class PaymentMethod : CA.PaymentMethod.PK.ForeignKeyOf<CashAccountPaymentMethodDetail>.By<paymentMethodID> { }
			public class PaymentMethodDetail : CA.PaymentMethodDetail.PK.ForeignKeyOf<CashAccountPaymentMethodDetail>.By<paymentMethodID, detailID> { }
			public class PaymentMethodForCashAccount : CA.PaymentMethodAccount.PK.ForeignKeyOf<CashAccountPaymentMethodDetail>.By<paymentMethodID, accountID> { }
		}

		#endregion

		#region AccountID
		public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }

		[PXDBInt(IsKey = true)]
		[PXDBDefault(typeof(CashAccount.cashAccountID))]
		[PXUIField(DisplayName = "Cash Account", Visible = false, Enabled = false)]
		[PXParent(typeof(Select<PaymentMethodAccount,
			Where<PaymentMethodAccount.cashAccountID, Equal<Current<CashAccountPaymentMethodDetail.accountID>>,
			And<PaymentMethodAccount.paymentMethodID, Equal<Current<CashAccountPaymentMethodDetail.paymentMethodID>>,
			And<PaymentMethodAccount.useForAP, Equal<True>>>>>))]
		public virtual int? AccountID
		{
			get;
			set;
		}
		#endregion
		#region PaymentMethodID
		public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID> { }

		[PXDBString(10, IsUnicode = true, IsKey = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Payment Method", Visible = false)]
		[PXSelector(typeof(Search<PaymentMethod.paymentMethodID>))]
		public virtual string PaymentMethodID
		{
			get;
			set;
		}
		#endregion
		#region DetailID
		public abstract class detailID : PX.Data.BQL.BqlString.Field<detailID> { }

		[PXDBString(10, IsUnicode = true, IsKey = true)]
		[PXDefault]
		[PXUIField(DisplayName = "ID", Visible = false, Enabled = false)]
		[PXSelector(typeof(Search<PaymentMethodDetail.detailID, Where<PaymentMethodDetail.paymentMethodID,
					Equal<Current<CashAccountPaymentMethodDetail.paymentMethodID>>,
						And<Where<PaymentMethodDetail.useFor, Equal<PaymentMethodDetailUsage.useForCashAccount>,
						Or<PaymentMethodDetail.useFor, Equal<PaymentMethodDetailUsage.useForAll>>>>>>))]
		public virtual string DetailID
		{
			get;
			set;
		}
		#endregion
		#region DetailValue
		public abstract class detailValue : PX.Data.BQL.BqlString.Field<detailValue> { }

		[PXDBStringWithMask(255, typeof(Search<PaymentMethodDetail.entryMask, Where<PaymentMethodDetail.paymentMethodID, Equal<Current<CashAccountPaymentMethodDetail.paymentMethodID>>,
									   And<PaymentMethodDetail.detailID, Equal<Current<CashAccountPaymentMethodDetail.detailID>>,
									   And<Where<PaymentMethodDetail.useFor, Equal<PaymentMethodDetailUsage.useForCashAccount>,
										   Or<PaymentMethodDetail.useFor, Equal<PaymentMethodDetailUsage.useForAll>>>>>>>), IsUnicode = true)]
		[PXUIField(DisplayName = "Value")]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[DynamicValueValidation(typeof(Search<PaymentMethodDetail.validRegexp, Where<PaymentMethodDetail.paymentMethodID, Equal<Current<CashAccountPaymentMethodDetail.paymentMethodID>>,
										And<PaymentMethodDetail.detailID, Equal<Current<CashAccountPaymentMethodDetail.detailID>>,
										And<Where<PaymentMethodDetail.useFor, Equal<PaymentMethodDetailUsage.useForCashAccount>,
											Or<PaymentMethodDetail.useFor, Equal<PaymentMethodDetailUsage.useForAll>>>>>>>))]
		public virtual string DetailValue
		{
			get;
			set;
		}
		#endregion
	}
}
