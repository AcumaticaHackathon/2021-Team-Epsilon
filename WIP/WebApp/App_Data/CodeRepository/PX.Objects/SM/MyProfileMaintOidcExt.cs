using System;
using PX.Common;
using PX.OidcClient.GraphExtensions;

namespace PX.SM
{
	[PXInternalUseOnly]
	public class MyProfileMaintOidcExt : AccessOidc<MyProfileMaint>
	{
		public static bool IsActive() { return PX.OidcClient.FeaturesHelper.OpenIDConnect; }
	}
}
