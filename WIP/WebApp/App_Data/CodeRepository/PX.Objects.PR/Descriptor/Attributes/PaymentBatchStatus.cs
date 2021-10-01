using PX.Data;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR
{
	public class PaymentBatchStatus
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute()
				: base(
					new string[] { ReadyForExport, WaitingPaycheckCalculation, Paid, Closed },
					new string[] { Messages.ReadyForExport, Messages.WaitingPaycheckCalculation, Messages.Paid, Messages.Closed }
				)
			{ }
		}

		public const string ReadyForExport = "RFE";
		public const string WaitingPaycheckCalculation = "WPC";
		public const string Paid = "PAD";
		public const string Closed = "CLD";

		public static string GetStatus(IEnumerable<PRPayment> results)
		{
			string status = ReadyForExport;
			if (results.Any(x => x.Released == true))
			{
				status = Closed;
			}
			else if (!results.Any(x => x.Paid == false))
			{
				status = Paid;
			}
			else if (results.Any(x => x.Calculated == false))
			{
				status = WaitingPaycheckCalculation;
			}

			return status;
		}

		public static string GetStatus(PRPayment payment)
		{
			return GetStatus(new List<PRPayment>() { payment });
		}
	}
}
