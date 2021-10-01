﻿using System;
using System.Collections.Generic;
using System.EnterpriseServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.GL.DAC;

namespace PX.Objects.CS.DAC
{
	[PXCacheName(Messages.Company)]
	[Serializable]
	[PXProjection(typeof(Select2<BAccount,
		InnerJoin<Organization, On<Organization.bAccountID, Equal<BAccount.bAccountID>>>, Where<True, Equal<True>>>), new Type[] { typeof(BAccount) })]
	public partial class OrganizationBAccount : BAccount
	{
		public new abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }
		public new abstract class defContactID : PX.Data.BQL.BqlInt.Field<defContactID> { }
		public new abstract class defAddressID : PX.Data.BQL.BqlInt.Field<defAddressID> { }
		public new abstract class defLocationID : PX.Data.BQL.BqlInt.Field<defLocationID> { }

		#region OrganizationType
		public new abstract class organizationType : PX.Data.BQL.BqlString.Field<organizationType> { }
		[PXDBString(30, BqlField = typeof(Organization.organizationType))]
		public virtual String OrganizationType { get; set; }
		#endregion
		#region AcctCD
		public new abstract class acctCD : PX.Data.BQL.BqlString.Field<acctCD> { }

		[PXDimensionSelector("COMPANY", 
			typeof(Search2<BAccount.acctCD, 
				InnerJoin<Organization, 
					On<Organization.bAccountID, Equal<BAccount.bAccountID>>>, 
				Where<Match<Organization, Current<AccessInfo.userName>>
					.And<Organization.organizationType.IsNotEqual<OrganizationTypes.group>>>>), 
			typeof(BAccount.acctCD),
			typeof(BAccount.acctCD), typeof(BAccount.acctName))]
		[PXDBString(30, IsUnicode = true, IsKey = true, InputMask = "")]
		[PXDefault()]
		[PXUIField(DisplayName = "Company ID", Visibility = PXUIVisibility.SelectorVisible)]
		public override String AcctCD
		{
			get
			{
				return base._AcctCD;
			}
			set
			{
				base._AcctCD = value;
			}
		}
		#endregion
		#region AcctName
		public new abstract class acctName : PX.Data.BQL.BqlString.Field<acctName> { }

		[PXDBString(60, IsUnicode = true)]
		[PXDefault()]
		[PXUIField(DisplayName = "Company Name", Visibility = PXUIVisibility.SelectorVisible)]
		public override String AcctName
		{
			get
			{
				return this._AcctName;
			}
			set
			{
				this._AcctName = value;
			}
		}
		#endregion
	}
}
