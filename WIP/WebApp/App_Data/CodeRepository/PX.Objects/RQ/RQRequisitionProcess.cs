using System;
using System.Collections;

using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.Automation;
using PX.Objects.GL;
using PX.Objects.AR;
using PX.Objects.AP;
using PX.Objects.EP;
using PX.SM;
using PX.TM;

namespace PX.Objects.RQ
{
	[TableAndChartDashboardType]
	public class RQRequisitionProcess : PXGraph<RQRequisitionProcess>
	{
		#region DACs
		public partial class RQRequisitionSelection : IBqlTable
		{
			#region CurrentOwnerID
			[PXDBInt]
			[CR.CRCurrentOwnerID]
			public virtual int? CurrentOwnerID { get; set; }
			public abstract class currentOwnerID : BqlInt.Field<currentOwnerID> { }
			#endregion
			#region OwnerID
			protected int? _OwnerID;
			[SubordinateOwner(DisplayName = "Assigned To")]
			public virtual int? OwnerID
			{
				get => (MyOwner == true) ? CurrentOwnerID : _OwnerID;
				set => _OwnerID = value;
			}
			public abstract class ownerID : BqlInt.Field<ownerID> { }
			#endregion
			#region MyOwner
			[PXDBBool]
			[PXDefault(true)]
			[PXUIField(DisplayName = "Me")]
			public virtual Boolean? MyOwner { get; set; }
			public abstract class myOwner : BqlBool.Field<myOwner> { }
			#endregion
			#region WorkGroupID
			protected Int32? _WorkGroupID;
			[PXDBInt]
			[PXUIField(DisplayName = "Workgroup")]
			[PXSelector(typeof(Search<EPCompanyTree.workGroupID,
				Where<EPCompanyTree.workGroupID, IsWorkgroupOrSubgroupOfContact<Current<AccessInfo.contactID>>>>),
				SubstituteKey = typeof(EPCompanyTree.description))]
			public virtual Int32? WorkGroupID
			{
				get => (MyWorkGroup == true) ? null : _WorkGroupID;
				set => _WorkGroupID = value;
			}
			public abstract class workGroupID : BqlInt.Field<workGroupID> { }
			#endregion
			#region MyWorkGroup
			[PXDBBool]
			[PXDefault(false)]
			[PXUIField(DisplayName = "My", Visibility = PXUIVisibility.Visible)]
			public virtual Boolean? MyWorkGroup { get; set; }
			public abstract class myWorkGroup : BqlBool.Field<myWorkGroup> { }
			#endregion
			#region MyEscalated
			[PXDBBool]
			[PXDefault(true)]
			[PXUIField(DisplayName = "Display Escalated", Visibility = PXUIVisibility.Visible)]
			public virtual Boolean? MyEscalated { get; set; }
			public abstract class myEscalated : BqlBool.Field<myEscalated> { }
			#endregion
			#region FilterSet
			[PXDBBool]
			[PXDefault(false)]
			public virtual bool? FilterSet
			{
				get
				{
					return
						OwnerID != null ||
						WorkGroupID != null ||
						MyWorkGroup == true ||
						MyEscalated == true;
				}
			}
			public abstract class filterSet : BqlBool.Field<filterSet> { }
			#endregion
			#region Action
			[PXWorkflowMassProcessing]
			public virtual string Action { get; set; }
			public abstract class action : BqlString.Field<action> { }
			#endregion
			#region SelectedPriority
			[PXDBInt]
			[PXDefault(-1)]
			[PXIntList(new int[] { -1, 0, 1, 2 },
				new string[] { "All", "Low", "Normal", "High" })]
			[PXUIField(DisplayName = "Priority")]
			public virtual Int32? SelectedPriority { get; set; }
			public abstract class selectedPriority : BqlInt.Field<selectedPriority> { }
			#endregion
			#region VendorID
			[VendorNonEmployeeActive(Visibility = PXUIVisibility.SelectorVisible, DescriptionField = typeof(Vendor.acctName), CacheGlobal = true, Filterable = true)]
			public virtual Int32? VendorID { get; set; }
			public abstract class vendorID : BqlInt.Field<vendorID> { }
			#endregion
			#region EmployeeID
			[PXDBInt()]
			[PXSubordinateSelector]
			[PXUIField(DisplayName = "Creator", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual Int32? EmployeeID { get; set; }
			public abstract class employeeID : BqlInt.Field<employeeID> { }
			#endregion
			#region Description
			[PXDBString(60, IsUnicode = true)]
			[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
			public virtual String Description { get; set; }
			public abstract class description : BqlString.Field<description> { }
			#endregion
			#region DescriptionWildcard
			[PXDBString(60, IsUnicode = true)]
			public virtual String DescriptionWildcard => Description != null ? "%" + Description + "%" : null;
			public abstract class descriptionWildcard : BqlString.Field<descriptionWildcard> { }
			#endregion
		}

		[PXCacheName(Messages.RQRequisition)]
		[OwnedEscalatedFilter.Projection(
			typeof(RQRequisitionSelection),
			typeof(RQRequisition.workgroupID),
			typeof(RQRequisition.ownerID),
			typeof(RQRequisition.orderDate))]
		public partial class RQRequisitionOwned : RQRequisition
		{
			#region ReqNbr
			public new abstract class reqNbr : BqlString.Field<reqNbr> { }
			#endregion
			#region OrderDate
			public new abstract class orderDate : BqlDateTime.Field<orderDate> { }
			#endregion
			#region Priority
			public new abstract class priority : BqlInt.Field<priority> { }
			#endregion
			#region Status
			public new abstract class status : BqlString.Field<status> { }
			#endregion
			#region Description
			public new abstract class description : BqlString.Field<description> { }
			#endregion
			#region WorkgroupID
			public new abstract class workgroupID : BqlInt.Field<workgroupID> { }
			#endregion
			#region OwnerID
			public new abstract class ownerID : BqlInt.Field<ownerID> { }
			#endregion
			#region CustomerID
			public new abstract class customerID : BqlInt.Field<customerID> { }
			#endregion
			#region VendorID
			public new abstract class vendorID : BqlInt.Field<vendorID> { }
			#endregion
			#region VendorLocationID
			public new abstract class vendorLocationID : BqlInt.Field<vendorLocationID> { }
			#endregion
			#region VendorRefNbr
			public new abstract class vendorRefNbr : BqlString.Field<vendorRefNbr> { }
			#endregion
		}
		#endregion

		#region Custom Views
		public class RQRequisitionProcessing :
			SelectFrom<RQRequisitionOwned>.
			LeftJoin<Customer>.On<Customer.bAccountID.IsEqual<RQRequisitionOwned.customerID>>.SingleTableOnly.
			LeftJoin<Vendor>.On<Vendor.bAccountID.IsEqual<RQRequisitionOwned.vendorID>>.SingleTableOnly.
			Where<
				Brackets<
					RQRequisitionSelection.selectedPriority.FromCurrent.IsEqual<AllPriority>.
					Or<RQRequisitionSelection.selectedPriority.FromCurrent.IsEqual<RQRequisitionOwned.priority>>>.
				And<
					Customer.bAccountID.IsNull.
					Or<Match<Customer, AccessInfo.userName.FromCurrent>>>.
				And<Vendor.bAccountID.IsNull.
					Or<Match<Vendor, AccessInfo.userName.FromCurrent>>>.
				And<WhereWorkflowActionEnabled<RQRequisitionOwned, RQRequisitionSelection.action>>>.
			ProcessingView.FilteredBy<RQRequisitionSelection>
		{
			public RQRequisitionProcessing(PXGraph graph) : base(graph) => InitView();
			public RQRequisitionProcessing(PXGraph graph, Delegate handler) : base(graph, handler) => InitView();

			protected virtual void InitView() => _OuterView.WhereAndCurrent<RQRequisitionSelection>(typeof(RQRequisitionSelection.ownerID).Name, typeof(RQRequisitionSelection.workGroupID).Name);
		}
		#endregion

		#region Initialization
		public RQRequisitionProcess()
		{
			Records.SetSelected<RQRequisitionLine.selected>();
			Records.SetProcessCaption(IN.Messages.Process);
			Records.SetProcessAllCaption(IN.Messages.ProcessAll);
		}
		#endregion

		#region Views
		public PXFilter<RQRequisitionSelection> Filter;
		public PXFilter<Vendor> Vendor;

		[PXFilterable]
		public RQRequisitionProcessing Records;
		#endregion

		#region Actions
		public PXCancel<RQRequisitionSelection> Cancel;

		public PXAction<RQRequisitionSelection> details;
		[PXEditDetailButton, PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		public virtual IEnumerable Details(PXAdapter adapter)
		{
			if (Records.Current != null && Filter.Current != null)
			{
				RQRequisitionEntry graph = PXGraph.CreateInstance<RQRequisitionEntry>();
				graph.Document.Current = graph.Document.Search<RQRequisition.reqNbr>(Records.Current.ReqNbr);
				throw new PXRedirectRequiredException(graph, true, AR.Messages.ViewDocument) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
			}
			return adapter.Get();
		}
		#endregion

		#region Event Handlers
		protected virtual void _(Events.RowSelected<RQRequisitionSelection> e)
		{
			if (!String.IsNullOrEmpty(e.Row?.Action))
			{
				var parameters = Filter.Cache.ToDictionary(e.Row);
				Records.SetProcessWorkflowAction(e.Row.Action, parameters);
			}
		}
		#endregion
	}
}