using System;
using PX.Common;
using PX.Data;

namespace PX.Objects.IN.WMS.Legacy
{
	[Obsolete(INScanCount.ObsoleteMsg.ScanClass)]
	[PXInternalUseOnly]
	public class LegacyWMSAttribute : DynamicDataSourceAttribute
	{
		protected override bool CanBeUsed => LegacyWMS.Value;

		private class LegacyWMS : IPrefetchable
		{
			private bool _value;
			void IPrefetchable.Prefetch()
			{
				using (PXDataRecord rec = PXDatabase.SelectSingle<INSetup>(new PXDataField<INSetup.useLegacyWMS>()))
					_value = rec?.GetBoolean(0) ?? false;
			}
			public static bool Value => PXDatabase.GetSlot<LegacyWMS>(typeof(LegacyWMS).FullName, typeof(INSetup))?._value == true;
		}
	}
}