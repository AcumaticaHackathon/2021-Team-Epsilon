using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using PX.Data;

namespace PX.Objects.SO.Exceptions
{
	public class InvalidShipmentCountersException : PXSetPropertyException
	{
		public InvalidShipmentCountersException()
			: base(Messages.InvalidShipmentCounters)
		{
		}

		public InvalidShipmentCountersException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
