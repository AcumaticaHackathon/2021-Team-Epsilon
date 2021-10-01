using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.SO
{
	public class SOIntercompanyAcctDefault
	{
		public class AcctSalesListAttribute : SOCustomListAttribute
		{
			private static Tuple<string, string>[] Pairs =>
				new[]
				{
					Pair(MaskItem, IN.Messages.MaskItem),
					Pair(MaskLocation, MaskLocationLabel),
				};

			public AcctSalesListAttribute() : base(Pairs) { }
			protected override Tuple<string, string>[] GetPairs() => Pairs;
		}

		public class AcctCOGSListAttribute : PXStringListAttribute
		{
			public AcctCOGSListAttribute() : base(
				new[]
				{
					Pair(MaskItem, IN.Messages.MaskItem),
					Pair(MaskLocation, AR.Messages.MaskCustomer),
				})
			{
			}
		}

		public const string MaskItem = "I";
		public const string MaskLocation = "L";

		public class maskItem : PX.Data.BQL.BqlString.Constant<maskItem>
		{
			public maskItem() : base(MaskItem) { }
		}

		public class maskLocation : PX.Data.BQL.BqlString.Constant<maskLocation>
		{
			public maskLocation() : base(MaskLocation) { }
		}
	}
}
