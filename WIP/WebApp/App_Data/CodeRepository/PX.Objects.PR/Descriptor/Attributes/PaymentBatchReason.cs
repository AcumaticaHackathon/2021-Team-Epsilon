using PX.Data;

namespace PX.Objects.PR
{
	public class PaymentBatchReason : PXStringListAttribute
	{
		public PaymentBatchReason()
			: base(
				new string[] { PrinterIssue, Lost, Damaged, Stolen, Corrected, AddedPaycheck, OtherReason, WrongEmployeeConfiguration, PaycheckError, BatchError },
				new string[] { Messages.PrinterIssue, Messages.Lost, Messages.Damaged, Messages.Stolen, Messages.Corrected, Messages.AddedPaycheck, Messages.OtherReason, Messages.WrongEmployeeConfiguration, Messages.PaycheckError, Messages.BatchError })
		{ }

		public PaymentBatchReason(string[] allowedValues, string[] allowedLabels)
			: base(allowedValues, allowedLabels)
		{ }

		public const string Initial = "Initial Export";
		public const string OtherReason = "Other";

		//Print
		public const string PrinterIssue = "Printer Issue";
		public const string Lost = "Lost";
		public const string Damaged = "Damaged";
		public const string Stolen = "Stolen";
		public const string Corrected = "Corrected";
		public const string AddedPaycheck = "Added paycheck";

		//Export
		public const string WrongEmployeeConfiguration = "Wrong Employee Configuration";
		public const string PaycheckError = "Error in paycheck";
		public const string BatchError = "Error in the batch";
		public const string Prenote = "Prenote Export";

		public class initial : PX.Data.BQL.BqlString.Constant<initial>
		{
			public initial() : base(Initial)
			{
			}
		}

		public class printerIssue : PX.Data.BQL.BqlString.Constant<printerIssue>
		{
			public printerIssue() : base(PrinterIssue)
			{
			}
		}
		public class lost : PX.Data.BQL.BqlString.Constant<lost>
		{
			public lost() : base(Lost)
			{
			}
		}
		public class damaged : PX.Data.BQL.BqlString.Constant<damaged>
		{
			public damaged() : base(Damaged)
			{
			}
		}
		public class stolen : PX.Data.BQL.BqlString.Constant<stolen>
		{
			public stolen() : base(Stolen)
			{
			}
		}
		public class corrected : PX.Data.BQL.BqlString.Constant<corrected>
		{
			public corrected() : base(Corrected)
			{
			}
		}

		public class addedPaycheck : PX.Data.BQL.BqlString.Constant<addedPaycheck>
		{
			public addedPaycheck() : base(AddedPaycheck)
			{
			}
		}

		public class otherReason : PX.Data.BQL.BqlString.Constant<otherReason>
		{
			public otherReason() : base(OtherReason)
			{
			}
		}

		public class wrongEmployeeConfiguration : PX.Data.BQL.BqlString.Constant<wrongEmployeeConfiguration>
		{
			public wrongEmployeeConfiguration() : base(WrongEmployeeConfiguration)
			{
			}
		}

		public class paycheckError : PX.Data.BQL.BqlString.Constant<paycheckError>
		{
			public paycheckError() : base(PaycheckError)
			{
			}
		}

		public class batchError : PX.Data.BQL.BqlString.Constant<batchError>
		{
			public batchError() : base(BatchError)
			{
			}
		}
	}

	public class PaymentBatchExportReason : PaymentBatchReason
	{
		public PaymentBatchExportReason()
			: base(
				new string[] { WrongEmployeeConfiguration, PaycheckError, BatchError, Prenote, OtherReason },
				new string[] { Messages.WrongEmployeeConfiguration, Messages.PaycheckError, Messages.BatchError, Messages.Prenote, Messages.OtherReason }
			)
		{ }
	}

	public class PaymentBatchPrintReason : PaymentBatchReason
	{
		public PaymentBatchPrintReason()
			: base(
				new string[] { PrinterIssue, Lost, Damaged, Stolen, Corrected, AddedPaycheck, OtherReason },
				new string[] { Messages.PrinterIssue, Messages.Lost, Messages.Damaged, Messages.Stolen, Messages.Corrected, Messages.AddedPaycheck, Messages.OtherReason })
		{ }
	}
}
