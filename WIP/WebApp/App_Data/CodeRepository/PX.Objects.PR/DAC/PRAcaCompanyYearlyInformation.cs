using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CR;
using PX.Objects.GL.Attributes;
using PX.Objects.GL.DAC;
using PX.Objects.TX.DAC.ReportParameters;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PRAcaCompanyYearlyInformation)]
	public class PRAcaCompanyYearlyInformation : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<PRAcaCompanyYearlyInformation>.By<orgBAccountID, year>
		{
			public static PRAcaCompanyYearlyInformation Find(PXGraph graph, int? orgBAccountID, string year) =>
				FindBy(graph, orgBAccountID, year);
		}

		public static class FK
		{
			public class Organization : GL.DAC.Organization.PK.ForeignKeyOf<PRAcaCompanyYearlyInformation>.By<orgBAccountID> { }
		}
		#endregion

		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		[Obsolete]
		[PXDBInt]
		public virtual int? BranchID { get; set; }
		#endregion

		#region OrgBAccountID
		public abstract class orgBAccountID : PX.Data.BQL.BqlInt.Field<orgBAccountID> { }
		[OrganizationTree(
			treeDataMember: typeof(TaxTreeSelect),
			onlyActive: true,
			SelectionMode = OrganizationTreeAttribute.SelectionModes.Branches,
			IsKey = true)]
		public virtual int? OrgBAccountID { get; set; }
		#endregion
		#region Year
		public abstract class year : PX.Data.BQL.BqlString.Field<year> { }
		[PXDBString(4, IsKey = true)]
		[PXDefault]
		[PXUIField(DisplayName = Messages.Year, Required = true)]
		[PXSelector(typeof(SelectFrom<PRPayGroupYear>
			.AggregateTo<GroupBy<PRPayGroupYear.year>>
			.OrderBy<PRPayGroupYear.year.Desc>
			.SearchFor<PRPayGroupYear.year>))]
		public virtual string Year { get; set; }
		#endregion
		#region IsPartOfAggregateGroup
		public abstract class isPartOfAggregateGroup : PX.Data.BQL.BqlBool.Field<isPartOfAggregateGroup> { }
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Part of an Aggregate Group")]
		public virtual bool? IsPartOfAggregateGroup { get; set; }
		#endregion
		#region IsAuthoritativeTransmittal
		public abstract class isAuthoritativeTransmittal : PX.Data.BQL.BqlBool.Field<isAuthoritativeTransmittal> { }
		[PXDBBool]
		[PXUIField(DisplayName = "Authoritative Transmittal")]
		[PXUIEnabled(typeof(Where<PRAcaCompanyYearlyInformation.isPartOfAggregateGroup.IsEqual<True>>))]
		[PXFormula(typeof(
			PRAcaCompanyYearlyInformation.isAuthoritativeTransmittal.When<PRAcaCompanyYearlyInformation.isPartOfAggregateGroup.IsEqual<True>>
				.Else<False>))]
		public virtual bool? IsAuthoritativeTransmittal { get; set; }
		#endregion

		#region Ein
		public abstract class ein : PX.Data.BQL.BqlString.Field<ein> { }
		[PXString(9, InputMask = "##-#######")]
		[PXUIField(DisplayName = "Tax Registration Number", Enabled = false)]
		[PXUnboundDefault(typeof(SearchFor<BAccount.taxRegistrationID>.Where<BAccount.bAccountID.IsEqual<orgBAccountID.FromCurrent>>))]
		public string Ein { get; set; }
		#endregion
		#region HeaderDescription
		public abstract class headerDescription : PX.Data.BQL.BqlString.Field<headerDescription> { }
		[PXString]
		[PXFormula(typeof(Selector<orgBAccountID, BranchItem.acctName>))]
		public string HeaderDescription { get; set; }
		#endregion

		#region System Columns
		#region TStamp
		public class tStamp : IBqlField { }
		[PXDBTimestamp()]
		public byte[] TStamp { get; set; }
		#endregion
		#region CreatedByID
		public class createdByID : IBqlField { }
		[PXDBCreatedByID()]
		public Guid? CreatedByID { get; set; }
		#endregion
		#region CreatedByScreenID
		public class createdByScreenID : IBqlField { }
		[PXDBCreatedByScreenID()]
		public string CreatedByScreenID { get; set; }
		#endregion
		#region CreatedDateTime
		public class createdDateTime : IBqlField { }
		[PXDBCreatedDateTime()]
		public DateTime? CreatedDateTime { get; set; }
		#endregion
		#region LastModifiedByID
		public class lastModifiedByID : IBqlField { }
		[PXDBLastModifiedByID()]
		public Guid? LastModifiedByID { get; set; }
		#endregion
		#region LastModifiedByScreenID
		public class lastModifiedByScreenID : IBqlField { }
		[PXDBLastModifiedByScreenID()]
		public string LastModifiedByScreenID { get; set; }
		#endregion
		#region LastModifiedDateTime
		public class lastModifiedDateTime : IBqlField { }
		[PXDBLastModifiedDateTime()]
		public DateTime? LastModifiedDateTime { get; set; }
		#endregion
		#endregion
	}
}
