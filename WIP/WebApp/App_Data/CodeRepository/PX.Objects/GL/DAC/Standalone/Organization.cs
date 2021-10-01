using System;
using System.Diagnostics;
using PX.Data;
using PX.Objects.CR;
using PX.SM;
using PX.Objects.CS;
using PX.Objects.CS.DAC;
using PX.Objects.AP;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM;

namespace PX.Objects.GL.DAC.Standalone
{
	[PXCacheName(CS.Messages.Company)]
	[Serializable]
	public partial class OrganizationAlias : GL.DAC.Organization
	{
		public new abstract class organizationID : PX.Data.BQL.BqlInt.Field<organizationID> { }
		public new abstract class organizationCD : PX.Data.BQL.BqlString.Field<organizationCD> { }
		public new abstract class baseCuryID : PX.Data.BQL.BqlString.Field<baseCuryID> { }
	}
}