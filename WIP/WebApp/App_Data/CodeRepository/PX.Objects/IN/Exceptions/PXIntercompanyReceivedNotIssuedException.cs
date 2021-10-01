using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN.Overrides.INDocumentRelease;

namespace PX.Objects.IN
{
	public class PXIntercompanyReceivedNotIssuedException : PXException
	{
		public PXIntercompanyReceivedNotIssuedException(PXCache cache, IBqlTable row)
			: base(Messages.IntercompanyReceivedNotIssued,
				PXForeignSelectorAttribute.GetValueExt<ItemLotSerial.lotSerialNbr>(cache, row),
				PXForeignSelectorAttribute.GetValueExt<ItemLotSerial.inventoryID>(cache, row))
		{
		}

		public PXIntercompanyReceivedNotIssuedException(Exception exc, string message, string receiptNbr)
			: base(exc, message, receiptNbr)
		{
		}

		public PXIntercompanyReceivedNotIssuedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
