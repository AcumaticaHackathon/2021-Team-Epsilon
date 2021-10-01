using System;
using System.Collections;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.EP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CR.MassProcess;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.CT;
using PX.Objects.SP.DAC;
using PX.TM;
using SP.Objects.CR;
using BAccount2 = PX.Objects.CR.BAccount2;
using PX.SM;

namespace SP.Objects.SP
{
	#region Filter

	[Serializable]
	public class OwnerFilter : IBqlTable
	{
		public abstract class contractID : BqlInt.Field<contractID> { }

		[PXInt]
		[PXSelector(typeof(
			SelectFrom<Contract>
				.Where<
					Brackets<
						Contract.isTemplate.IsNull.Or<Contract.isTemplate.IsEqual<False>>
							.And<Contract.baseType.IsEqual<Contract.ContractBaseType>>
					>
					.And<Brackets<MatchWithBAccountNotNull<Contact.bAccountID>>>
				>
				.OrderBy<Contract.contractCD.Asc>
				.SearchFor<Contract.contractID>
			),
			typeof(Contract.contractCD), typeof(Contract.description),
			SubstituteKey = typeof(Contract.contractCD),
			DescriptionField = typeof(Contract.description))]
		[PXUIField(DisplayName = "Contract")]
		public virtual Int32? ContractID { get; set; }

		public abstract class currentOwnerID : BqlInt.Field<currentOwnerID> { }

		protected Int32? _CurrentOwnerID;
		[PXInt]
		[PXUIField(DisplayName = "Created By")]
		[PXSelector(typeof(Search2<Contact.contactID,
			LeftJoin<CRContactClass, On<Contact.classID, Equal<CRContactClass.classID>>>,
			Where2<
			MatchWithBAccountNotNull<Contact.bAccountID>,
			And<Contact.contactType, Equal<ContactTypesAttribute.person>,
				And<Where<CRContactClass.isInternal, Equal<False>, Or<CRContactClass.isInternal, IsNull>>>>>>),
			typeof(Contact.displayName), typeof(Contact.eMail),
			DescriptionField = typeof(Contact.displayName))]
		public virtual Int32? CurrentOwnerID
		{
			get
			{
				return _CurrentOwnerID;
			}
			set
			{
				_CurrentOwnerID = value;
			}
		}

		public abstract class myOwner : BqlBool.Field<myOwner> { }

		protected Boolean? _MyOwner;
		[PXBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Me")]
		public virtual Boolean? MyOwner
		{
			get { return _MyOwner; }
			set { _MyOwner = value; }
		}
	}
	#endregion

	[DashboardType((int)DashboardTypeAttribute.Type.Default, TableAndChartDashboardTypeAttribute._AMCHARTS_DASHBOART_TYPE)]
	public class SPCaseOpenInquiry : PXGraph<SPCaseOpenInquiry>
	{
		#region Constructor
		public SPCaseOpenInquiry()
		{
			if (PortalSetup.Current == null)
				throw new PXSetupNotEnteredException<PortalSetup>(ErrorMessages.SetupNotEntered);

			var setting = PortalSetup.Current;

			FilteredItems.Cache.AllowUpdate = false;
			ViewCase.SetEnabled(true);
			OwnerFilter current = this.Caches[typeof(OwnerFilter)].Current as OwnerFilter;
			if (Filter.Current.MyOwner == true)
			{
				Contact _contact = ReadBAccount.ReadCurrentContactWithoutCheck();
				if (_contact != null)
					current.CurrentOwnerID = _contact.ContactID;
			}
			PXUIFieldAttribute.SetEnabled(this.Caches[typeof(OwnerFilter)], null, typeof(OwnerFilter.currentOwnerID).Name, Filter.Current.MyOwner == false);
			PXUIFieldAttribute.SetDisplayName<Contact2.displayName>(Caches[typeof(Contact2)], "Assigned To");
		}
		#endregion

		#region Selects
		public PXFilter<OwnerFilter> Filter;
		[PXFilterable]
		public SelectFrom<CRCase>
			.LeftJoin<CRCaseClass>
				.On<CRCaseClass.caseClassID.IsEqual<CRCase.caseClassID>>
			.LeftJoin<Contract>
				.On<Contract.contractID.IsEqual<CRCase.contractID>>
			.LeftJoin<BAccount>
				.On<BAccount.bAccountID.IsEqual<CRCase.customerID>>
			.LeftJoin<CRCustomerClass>
				.On<CRCustomerClass.cRCustomerClassID.IsEqual<BAccount.classID>>
			.LeftJoin<Contact2>
				.On<CRCase.ownerID.IsEqual<Contact2.contactID>>
			.LeftJoin<Contact>
				.On<CRCase.contactID.IsEqual<Contact.contactID>>
			.LeftJoin<CRContactClass>
				.On<CRContactClass.classID.IsEqual<Contact.classID>>
			.Where<Brackets<MatchWithBAccountNotNull<CRCase.customerID>>
				.And<CRCase.isActive.IsEqual<True>>
				.And<Brackets<CRCaseClass.isInternal.IsEqual<False>
					.Or<CRCaseClass.isInternal.IsNull>>>
				.And<Brackets<CRCustomerClass.isInternal.IsEqual<False>
					.Or<CRCustomerClass.isInternal.IsNull>>>
				.And<Brackets<CRContactClass.isInternal.IsEqual<False>
					.Or<CRContactClass.isInternal.IsNull>>>
				.And<Brackets<OwnerFilter.contractID.FromCurrent.IsNull
					.Or<CRCase.contractID.IsEqual<OwnerFilter.contractID.FromCurrent>>>>
				.And<Brackets<OwnerFilter.currentOwnerID.FromCurrent.IsNull
					.Or<CRCase.contactID.IsEqual<OwnerFilter.currentOwnerID.FromCurrent>>>>>
			.View
			FilteredItems;

		[PXHidden]
		public PXSelect<Contract> Contract;
		#endregion



		#region Case Cache Attached
		[Owner(typeof(CRCase.workgroupID), DisplayName = "Owner Name")]
		[PXChildUpdatable(AutoRefresh = true, TextField = "AcctName", ShowHint = false)]
		[PXMassUpdatableField]
		[PXMassMergableField]
		protected virtual void CRCase_OwnerID_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDBDatetimeScalar(typeof(Search<CRActivityStatistics.lastActivityDate, Where<CRActivityStatistics.noteID, Equal<CRCase.noteID>>>), PreserveTime = true, UseTimeZone = true)]
		[PXDate]
		protected virtual void CRCase_LastActivity_CacheAttached(PXCache sender)
		{
		}

		[Customer(DescriptionField = typeof(Customer.acctName), Visibility = PXUIVisibility.SelectorVisible, DisplayName = "Contract Customer")]
		protected virtual void Contract_CustomerID_CacheAttached(PXCache sender)
		{
		}
		#endregion

		#region Actions
		public PXAction<OwnerFilter> ViewCase;
		[PXUIField(Visible = false)]
		public virtual IEnumerable viewCase(PXAdapter adapter)
		{
			if (FilteredItems.Current != null)
			{
				PXRedirectHelper.TryRedirect(this, FilteredItems.Current, PXRedirectHelper.WindowMode.InlineWindow);
			}
			return adapter.Get();
		}

		public PXCancel<OwnerFilter> Cancel;
		#endregion

		#region Event Handler
		protected virtual void OwnerFilter_myOwner_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			var row = e.Row as OwnerFilter;
			if (row == null)
				return;

			if (row.MyOwner == true)
			{
				Contact _contact = ReadBAccount.ReadCurrentContactWithoutCheck();
				if (_contact != null)
					row.CurrentOwnerID = _contact.ContactID;
			}
			else
			{
				row.CurrentOwnerID = null;
			}
			PXUIFieldAttribute.SetEnabled(sender, e.Row, typeof(OwnerFilter.currentOwnerID).Name, row.MyOwner == false);
		}

		protected virtual void OwnerFilter_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			var row = e.Row as OwnerFilter;
			if (row == null)
				return;

			if (row.MyOwner == true)
			{
				Contact _contact = ReadBAccount.ReadCurrentContactWithoutCheck();
				if (_contact != null)
					row.CurrentOwnerID = _contact.ContactID;
			}
			else
			{
				row.CurrentOwnerID = null;
			}
			PXUIFieldAttribute.SetEnabled(sender, e.Row, typeof(OwnerFilter.currentOwnerID).Name, row.MyOwner == false);
		}

		protected virtual void OwnerFilter_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			var row = e.Row as OwnerFilter;
			if (row == null)
				return;

			PXUIFieldAttribute.SetEnabled(sender, e.Row, typeof(OwnerFilter.currentOwnerID).Name, row.MyOwner == false);
		}
		#endregion

	}
}
