using PX.Data;

namespace PX.Objects.PR
{
	public class PaymentStatus
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute()
				: base(
				new string[] { Hold, NeedCalculation,
					PendingPayment, Paid,
					Released, LiabilityPartiallyPaid,
					Closed, Voided,
					Open, PaymentBatchCreated },
				new string[] { Messages.Hold, Messages.NeedCalculation,
					Messages.PendingPayment, Messages.Paid,
					Messages.Released, Messages.LiabilityPartiallyPaid,
					Messages.Closed, Messages.Voided,
					Messages.Open, Messages.PaymentBatchCreated})
			{ }
		}

		public class hold : PX.Data.BQL.BqlString.Constant<hold>
		{
			public hold() : base(Hold)
			{
			}
		}

		public class needCalculation : PX.Data.BQL.BqlString.Constant<needCalculation>
		{
			public needCalculation() : base(NeedCalculation)
			{
			}
		}

		public class pendingPayment : PX.Data.BQL.BqlString.Constant<pendingPayment>
		{
			public pendingPayment() : base(PendingPayment)
			{
			}
		}

		public class paid : PX.Data.BQL.BqlString.Constant<paid>
		{
			public paid() : base(Paid)
			{
			}
		}

		public class released : PX.Data.BQL.BqlString.Constant<released>
		{
			public released() : base(Released)
			{
			}
		}

		public class liabilityPartiallyPaid : PX.Data.BQL.BqlString.Constant<liabilityPartiallyPaid>
		{
			public liabilityPartiallyPaid() : base(LiabilityPartiallyPaid)
			{
			}
		}

		public class closed : PX.Data.BQL.BqlString.Constant<closed>
		{
			public closed() : base(Closed)
			{
			}
		}

		public class voided : PX.Data.BQL.BqlString.Constant<voided>
		{
			public voided() : base(Voided)
			{
			}
		}

		public class open : PX.Data.BQL.BqlString.Constant<open>
		{
			public open() : base(Open)
			{
			}
		}

		public class paymentBatchCreated : PX.Data.BQL.BqlString.Constant<paymentBatchCreated>
		{
			public paymentBatchCreated() : base(PaymentBatchCreated)
			{
			}
		}

		public const string Hold = "HLD";
		public const string NeedCalculation = "CAL";
		public const string PendingPayment = "PPA";
		public const string Paid = "PAD";
		public const string Released = "REL";
		public const string LiabilityPartiallyPaid = "LPP";
		public const string Closed = "CLS";
		public const string Voided = "VDD";
		public const string Open = "OPN";
		public const string PaymentBatchCreated = "PBC";
	}
}
