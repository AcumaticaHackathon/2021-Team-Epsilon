using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Objects.AR.CCPaymentProcessing.Common;

namespace PX.Objects.AR
{
	public static class CCSyncStatusCode
	{
		public const string None = "N";
		public const string Error = "E";
		public const string Warning = "W";
		public const string Success = "S";

		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(ValueLabelPairs())
			{

			}
		}

		public static Tuple<string, string>[] ValueLabelPairs()
		{
			var arr = new Tuple<string, string>[]
			{
					new Tuple<string,string>(None, Messages.CCSyncStatusNone),
					new Tuple<string,string>(Error, Messages.CCSyncStatusError),
					new Tuple<string, string>(Warning, Messages.CCSyncStatusWarning),
					new Tuple<string, string>(Success, Messages.CCSyncStatusSuccess)
			};
			return arr;
		}

		public static SyncStatus GetSyncStatusBySyncStatusStr(string syncStatusCode)
		{
			if (!mapping.Where(i => i.Item2 == syncStatusCode).Any())
			{
				throw new PXInvalidOperationException();
			}
			return mapping.Where(i => i.Item2 == syncStatusCode).Select(i => i.Item1).First();
		}

		public static string GetSyncStatusStrBySyncStatus(SyncStatus syncStatus)
		{
			return mapping.Where(i => i.Item1 == syncStatus).Select(i => i.Item2).First();
		}

		private static (SyncStatus, string)[] mapping = new[] {
			(SyncStatus.None, CCSyncStatusCode.None),
			(SyncStatus.Error, CCSyncStatusCode.Error),
			(SyncStatus.Warning, CCSyncStatusCode.Warning),
			(SyncStatus.Success, CCSyncStatusCode.Success)
		};

		public class error : PX.Data.BQL.BqlString.Constant<error>
		{
			public error() : base(CCSyncStatusCode.Error) { }
		}
	}
}
