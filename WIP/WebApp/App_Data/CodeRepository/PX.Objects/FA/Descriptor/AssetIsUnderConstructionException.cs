using System;
using System.Runtime.Serialization;
using PX.Data;

namespace PX.Objects.FA
{
	public class AssetIsUnderConstructionException : PXException, ISerializable
	{
		public AssetIsUnderConstructionException() : base(Messages.AssetIsUnderConstructionAndWillNotBeDepreciated) { }
		public AssetIsUnderConstructionException(string message) : base(message) { }
		public AssetIsUnderConstructionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
