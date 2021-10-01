﻿using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.GL.Attributes;

namespace PX.Objects.GL.DAC
{
	/// <summary>
	/// Represents many-to-many relation link between <see cref="Organization"/> and <see cref="Ledger"/> records. 
	/// </summary>
	[Serializable]
	public class OrganizationLedgerLink: IBqlTable
    {
		#region Keys
		public class PK : PrimaryKeyOf<OrganizationLedgerLink>.By<organizationID, ledgerID>
		{
			public static OrganizationLedgerLink Find(PXGraph graph, int? organizationID, int? ledgerID) => FindBy(graph, organizationID, ledgerID);
		}
		public static class FK
		{
			public class Organization : DAC.Organization.PK.ForeignKeyOf<OrganizationLedgerLink>.By<organizationID> { }
			public class Ledger : GL.Ledger.PK.ForeignKeyOf<OrganizationLedgerLink>.By<ledgerID> { }
		}
		#endregion

		#region OrganizationID
		public abstract class organizationID : PX.Data.BQL.BqlInt.Field<organizationID> { }

		// <summary>
		/// The reference to the associated <see cref="Organization"/> record.
		/// </summary>
		[PXParent(typeof(Select<Organization, Where<Organization.organizationID, Equal<Current<OrganizationLedgerLink.organizationID>>>>))]
		[Organization(true, typeof(Search<Organization.organizationID, Where<Organization.organizationType.IsNotEqual<OrganizationTypes.group>>>), null, IsKey = true, FieldClass = null)]
		public virtual int? OrganizationID { get; set; }
        #endregion

        #region LedgerID
        public abstract class ledgerID : PX.Data.BQL.BqlInt.Field<ledgerID> { }

		/// <summary>
		/// The reference to the associated <see cref="Ledger"/> record.
		/// </summary>
		[PXDBInt(IsKey = true)]
        [PXUIField(DisplayName = Messages.Ledger)]
        [PXSelector(
			typeof(Search<Ledger.ledgerID>),
			SubstituteKey = typeof(Ledger.ledgerCD),
			DescriptionField = typeof(Ledger.descr),
			DirtyRead =true)]
		[PXDBDefault(typeof(Ledger.ledgerID))]
		public virtual int? LedgerID { get; set; } 
        #endregion
    }
}
