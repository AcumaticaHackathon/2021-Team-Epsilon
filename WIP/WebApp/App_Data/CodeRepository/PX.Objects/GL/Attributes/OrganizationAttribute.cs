using System;
using System.Collections;
using PX.Data;
using PX.Objects.GL.DAC;
using PX.Objects.CR;
using System.Collections.Generic;
using System.Linq;
using PX.Objects.PM;

namespace PX.Objects.GL.Attributes
{
	[PXDBInt]
	[PXInt]
	[PXUIField(DisplayName = Messages.Company, FieldClass = "BRANCH")]
	public class OrganizationAttribute : AcctSubAttribute 
		{
		public const string _DimensionName = "COMPANY";

		protected static readonly Type _selectorSource;
		protected static readonly Type _defaultingSource;

		static OrganizationAttribute() {
			_selectorSource = typeof(Search<Organization.organizationID,
				Where<Organization.organizationType, NotEqual<OrganizationTypes.group>,
				And<MatchWithOrganization<Organization.organizationID>>>>);

			_defaultingSource = typeof(Search2<Organization.organizationID,
						InnerJoin<Branch,
							On<Organization.organizationID, Equal<Branch.organizationID>>>,
						Where<Branch.branchID, Equal<Current<AccessInfo.branchID>>,
							And<MatchWithBranch<Branch.branchID>>>>);
		}

		public OrganizationAttribute(bool onlyActive = true)
		    : this(onlyActive, _selectorSource, _defaultingSource)
		{

		}

		public OrganizationAttribute(bool onlyActive, Type defaultingSource)
			: this(onlyActive, _selectorSource, defaultingSource)
		{

		}

		public OrganizationAttribute(bool onlyActive, Type selectorSource, Type defaultingSource)
		{
			PXDimensionSelectorAttribute attr =
				new PXDimensionSelectorAttribute(_DimensionName,
					selectorSource,
					typeof(Organization.organizationCD),
					typeof(Organization.organizationCD), typeof(Organization.organizationName))
				{
					ValidComboRequired = true,
					DescriptionField = typeof(Organization.organizationName)
				};

			_Attributes.Add(attr);

			_Attributes.Add(defaultingSource != null ? new PXDefaultAttribute(defaultingSource) : new PXDefaultAttribute());

			if (onlyActive)
			{
				_Attributes.Add(new PXRestrictorAttribute(typeof(Where<Organization.active, Equal<True>>), Messages.TheCompanyIsInactive));
			}

			Initialize();
		}


	}	

	[PXDBInt]
	[PXInt]
	[PXUIField(DisplayName = Messages.Group, FieldClass = "BRANCH")]
	public class OrganizationGroupAttribute : AcctSubAttribute
	{
		public const string _DimensionName = "COMPANY";

		protected static readonly Type _selectorSource;
		protected static readonly Type _defaultingSource;

		static OrganizationGroupAttribute()
		{
			_selectorSource = typeof(Search<Organization.organizationID, 
				Where<Organization.organizationType, Equal<OrganizationTypes.group>/*,
				  And<MatchWithOrganization<Organization.organizationID>>*/>>);

			_defaultingSource = typeof(Search2<Organization.organizationGroupID,
				InnerJoin<Branch,
					On<Organization.organizationID, Equal<Branch.organizationID>>>,
				Where<Branch.branchID, Equal<Current<AccessInfo.branchID>>/*,
					And<MatchWithBranch<Branch.branchID>>*/>>);
		}

		public OrganizationGroupAttribute(bool onlyActive = true)
			: this(onlyActive, _selectorSource, _defaultingSource)
		{

		}

		public OrganizationGroupAttribute(bool onlyActive, Type defaultingSource)
			: this(onlyActive, _selectorSource, defaultingSource)
		{

		}

		public OrganizationGroupAttribute(bool onlyActive, Type selectorSource, Type defaultingSource)
		{
			PXDimensionSelectorAttribute attr =
				new PXDimensionSelectorAttribute(_DimensionName,
					selectorSource,
					typeof(Organization.organizationCD),
					typeof(Organization.organizationCD), typeof(Organization.organizationName))
				{
					ValidComboRequired = true,
					DescriptionField = typeof(Organization.organizationName)
				};

			_Attributes.Add(attr);

			_Attributes.Add(defaultingSource != null ? new PXDefaultAttribute(defaultingSource) : new PXDefaultAttribute());

			if (onlyActive)
			{
				_Attributes.Add(new PXRestrictorAttribute(typeof(Where<Organization.active, Equal<True>>), Messages.TheCompanyIsInactive));
			}

			Initialize();
		}


	}
	public class PXSelectOrganizationTree : PXSelectBase<BranchItem>
	{
		// ReSharper disable InconsistentNaming
		[InjectDependency]
		private ICurrentUserInformationProvider _currentUserInformationProvider { get; set; }
		// ReSharper restore InconsistentNaming

	    public bool OnlyActive = true;
		public PXSelectOrganizationTree(PXGraph graph)
		{
			_Graph = graph;
			View = CreateView(_Graph, new PXSelectDelegate<string>(tree));
		}
		public PXSelectOrganizationTree(PXGraph graph, Delegate handler)
		{
			_Graph = graph;
			View = CreateView(_Graph, handler);
		}

		private static PXView CreateView(PXGraph graph, Delegate handler)
		{
			return new PXView(graph, false,
				new Search<BranchItem.bAccountID, Where<BranchItem.acctCD, Equal<Argument<string>>>, OrderBy<Asc<BranchItem.acctCD>>>(),
				handler);
		}

		public virtual IEnumerable tree
		(
			[PXString]
			string AcctCD
		)
		{
			var result = new List<BranchItem>();
			AddOrganizations(result, null,
				_currentUserInformationProvider.GetOrganizations(OnlyActive, false).Where(_ => _.DeletedDatabaseRecord == false));

			PXDelegateResult delegateResult = new PXDelegateResult();
			delegateResult.AddRange(result);
			delegateResult.IsResultFiltered = true;
			delegateResult.IsResultSorted = PXView.Searches.Length == 0 || PXView.Searches[0] == null;
			delegateResult.IsResultTruncated = false;
			return delegateResult;
		}

		private void AddOrganizations(List<BranchItem> result,
			PXAccess.MasterCollection.Organization masterOrganization,
			IEnumerable<PXAccess.MasterCollection.Organization> organizations)
		{
			foreach (var organization in organizations.Where(_ => _.DeletedDatabaseRecord == false))
			{
				var o = CreateItem(organization.BAccountID, 
					organization.OrganizationCD, 
					organization.OrganizationName,
					organization.IsGroup
						? BranchItem.type.Group 
						: BranchItem.type.Organization,
						masterOrganization?.BAccountID,
					true,
					organization.PrimaryParent?.BAccountID);

				result.Add(o);
				if (organization.IsGroup)
				{
					AddOrganizations(result, organization, organization.ChildOrganizations);
				}
				else
				{
					if (organization.IsSingle) continue;
					result.AddRange(organization.ChildBranches
						.Where(_ => _.DeletedDatabaseRecord == false)
						.Select(branch => CreateItem(branch.BAccountID, branch.BranchCD, branch.BranchName, 
							BranchItem.type.Branch,
								organization.BAccountID)));
				}
			}
		}
		private BranchItem CreateItem(int? id, string cd, string name, string type, int? parentId = null, bool? canSelect = true, int? primaryGroupID = null)
		{
			var o = new BranchItem()
			{
				BAccountID = id,
				AcctName = name,
				ParentBAccountID = parentId,
				PrimaryGroupID = primaryGroupID,
				Type = type,
				CanSelect = canSelect
			};

			object value = cd;
			this._Graph.Caches[typeof(BranchItem)].RaiseFieldUpdating<BranchItem.acctCD>(o, ref value);
			o.AcctCD = value.ToString();
			return o;
		}
	}

	public class BaseOrganizationTreeAttribute : AcctSubAttribute
	{
		public Type SourceOrganizationID;
		public Type SourceBranchID;

		public enum SelectionModes
		{
			All, Branches, Organizations
		}

		public SelectionModes SelectionMode
		{
			get => (SelectionModes)Enum.Parse(typeof(SelectionModes), _attr.SelectionMode, true);
			set => _attr.SelectionMode = Enum.GetName(typeof(SelectionModes), value)?.ToLower();
		}
		protected TreeSelectorAttribute _attr;
		
		protected BaseOrganizationTreeAttribute(Type treeDataMember = null, bool onlyActive = true, bool nullable = true, bool branchMode = false)
		{
			_attr = new TreeSelectorAttribute(treeDataMember)
			 {				
				OnlyActive = onlyActive,
				Nullable = nullable,
				BranchMode = branchMode
            };

	        _Attributes.Add(_attr);
        }
		protected class TreeSelectorAttribute : PXSelectorAttribute
		{
			public bool OnlyActive = true;
			public bool Nullable = true;
			public bool BranchMode = false;
			public string SelectionMode;


			protected readonly Type TreeDataMemberType;
			
			public TreeSelectorAttribute(Type treeDataMemberType = null)
				: this(
					typeof(Search3<BranchItem.bAccountID, OrderBy<Asc<BranchItem.acctCD>>>),
					treeDataMemberType,
					typeof(BranchItem.acctCD),
					typeof(BranchItem.acctName))
			{
			}

			public TreeSelectorAttribute(Type selectorSearch, Type treeDataMemberType, params Type[] fieldList) : base(selectorSearch, fieldList)
			{
				DescriptionField = typeof(BranchItem.acctName);
				SubstituteKey = typeof(BranchItem.acctCD);
				if (treeDataMemberType != null && !typeof(PXSelectOrganizationTree).IsAssignableFrom(treeDataMemberType))
				{
					throw new ArgumentException($"{treeDataMemberType.Name} argument must be of PXSelectOrganizationTree type");
				}
				TreeDataMemberType = treeDataMemberType ?? typeof(PXSelectOrganizationTree);
			}

			public override void CacheAttached(PXCache sender)
			{
				base.CacheAttached(sender);
				TreeViewName = "_OrganizationTree_" + sender.GetItemType().FullName + this._FieldName;
				if (!sender.Graph.Views.ContainsKey(TreeViewName))
				{
					PXSelectOrganizationTree select = (PXSelectOrganizationTree)Activator.CreateInstance(TreeDataMemberType, sender.Graph);
					select.OnlyActive = OnlyActive;					
					sender.Graph.Views.Add(TreeViewName, select.View);
				}

				if (sender.Graph.Caches[typeof(BAccount)].GetItemType() == typeof(BranchItem))
				{
					sender.Graph.Caches.Remove(typeof(BranchItem));
				}
			}

			public string TreeViewName
			{
				get;
				private set;
			}

			public override void FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
			{
			}

			public override void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
			{
				base.FieldSelecting(sender, e);
				PXBranchSelectorState state = new PXBranchSelectorState(e.ReturnState as PXFieldState);
				state.ViewName = TreeViewName;
				state.DACName = sender.GetItemType().FullName;
				state.SelectionMode = SelectionMode;
				e.ReturnState = state;
			}

			protected override void DescriptionFieldSelecting(PXCache sender, PXFieldSelectingEventArgs e, object key, ref bool deleted)
			{
				if (BranchMode)
					key = PXAccess.GetBranch((int?)key)?.BAccountID;
				base.DescriptionFieldSelecting(sender, e, key, ref deleted);
			}

			public override void SubstituteKeyFieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
			{
				if (e.ReturnValue != null && !Nullable && (int?)e.ReturnValue == 0)
				{
					e.ReturnValue = null;
				}
				if (BranchMode && e.ReturnValue != null)
				{
					e.ReturnValue = PXAccess.GetBranch((int?)e.ReturnValue)?.BAccountID ?? null;
				}
				base.SubstituteKeyFieldSelecting(sender, e);

			}

			public override void SubstituteKeyFieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
			{
				object newValue = e.NewValue;
				if (!Nullable && object.Equals(newValue, 0))
				{
					e.NewValue = newValue = null;
				}
				base.SubstituteKeyFieldUpdating(sender, e);
				if (BranchMode && e.NewValue != null && e.NewValue != newValue)
				{
					e.NewValue = PXAccess.GetBranchByBAccountID((int?)e.NewValue)?.BranchID ?? null;
				}

				if (e.NewValue == null && !Nullable)
				{
					e.NewValue = 0;
				}
			}			
		}
	}

	[PXDBInt]
	[PXDefault]
	[PXUIField(DisplayName = "Company/Branch", FieldClass = "BRANCH")]
	public class OrganizationTreeAttribute : BaseOrganizationTreeAttribute, IPXFieldUpdatingSubscriber, IPXFieldDefaultingSubscriber
	{
		// ReSharper disable InconsistentNaming
		[InjectDependencyOnTypeLevel]
		private ICurrentUserInformationProvider _currentUserInformationProvider { get; set; }
		// ReSharper restore InconsistentNaming

		public OrganizationTreeAttribute(Type treeDataMember = null, bool onlyActive = true)
			:this(null,null, treeDataMember, onlyActive)
		{
		}

		public OrganizationTreeAttribute(Type sourceOrganizationID, Type sourceBranchID, Type treeDataMember = null, bool onlyActive = true)
			:base(treeDataMember, onlyActive)
		{
			SourceOrganizationID = sourceOrganizationID;
			SourceBranchID = sourceBranchID;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			if (SourceOrganizationID != null)
			{
				PXUIFieldAttribute.SetEnabled(sender, null, SourceOrganizationID.Name, false);
			}
			if (SourceBranchID != null)
			{
				PXUIFieldAttribute.SetEnabled(sender, null, SourceBranchID.Name, false);
			}
		}
		public void FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (!_attr.Nullable)
			{
				e.NewValue = null;
				return;
			}
			else
			{
				var currentBranch = PXAccess.GetBranch(sender.Graph.Accessinfo.BranchID);
				if (currentBranch != null && e.NewValue == null)
				{
					var state = sender.GetStateExt(e.Row, _FieldName) as PXFieldState;
					foreach (BranchItem branch in sender.Graph.Views[state.ViewName].SelectMulti())
					{
						if (branch.AcctCD.Trim() == currentBranch.BranchCD.Trim())
						{
							e.NewValue = branch.AcctCD;
							break;
						}

						if (branch.AcctCD.Trim() == currentBranch.Organization.OrganizationCD.Trim())
						{
							e.NewValue = branch.AcctCD;
						}

					}
				}
			}
		}

		public void FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			if (e.Row == null) return;
			if (SourceOrganizationID == null && SourceBranchID == null) return;

			int? bAccountID = (int?)e.NewValue;

			if (bAccountID == null)
			{
				SetValue(sender, e.Row, SourceOrganizationID, null);
				SetValue(sender, e.Row, SourceBranchID, null);
			}
			else
			{
				foreach (PXAccess.MasterCollection.Organization organization in _currentUserInformationProvider.GetOrganizations(_attr.OnlyActive)
					.Where(_ => _.DeletedDatabaseRecord == false))
				{
					if (organization.BAccountID == bAccountID)
					{
						SetValue(sender, e.Row, SourceOrganizationID, organization.OrganizationID);
						SetValue(sender, e.Row, SourceBranchID, null);
						return;
					}

					foreach (PXAccess.MasterCollection.Branch branch in organization.ChildBranches
						.Where(_ => _.DeletedDatabaseRecord == false))
					{
						if (branch.BAccountID == bAccountID)
						{
							SetValue(sender, e.Row, SourceOrganizationID, organization.OrganizationID);
							SetValue(sender, e.Row, SourceBranchID, branch.BranchID);
							return;
						}
					}
				}
				SetValue(sender, e.Row, SourceOrganizationID, null);
				SetValue(sender, e.Row, SourceBranchID, null);
				//throw new PXException(ErrorMessages.ElementDoesntExist, bAccountID);
			}

		}
		private void SetValue(PXCache cache, object row, Type fieldType, object value)
		{
			if (fieldType != null)
			{
				cache.SetValue(row, fieldType.Name, value);
			}
		}
	}

	[PXDBInt]
	[PXDefault]
	public class OrganizationBAccountIDAttribute : PXAggregateAttribute, IPXRowSelectedSubscriber
	{
		protected Type SourceOrganizationID;
		protected Type SourceBranchID;

		public OrganizationBAccountIDAttribute(Type sourceOrganizationID, Type sourceBranchID)
		{
			SourceOrganizationID = sourceOrganizationID;
			SourceBranchID = sourceBranchID;
		}

		public void RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			if (e.Row == null) return;

			if (SourceOrganizationID == null && SourceBranchID == null) return;
			
			int? bAccountID = null;

			if (SourceOrganizationID != null)
			{
				var orgID = (int?)sender.GetValue(e.Row, SourceOrganizationID.Name);
				var org = PXAccess.GetOrganizationByID(orgID);
				bAccountID = org?.BAccountID;
			}

			if (SourceBranchID != null && bAccountID == null)
			{
				var branchID = (int?)sender.GetValue(e.Row, SourceBranchID.Name);
				var branch = PXAccess.GetBranch(branchID);
				bAccountID = branch?.BAccountID;
			}

			// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs [Justification]
			sender.SetValue(e.Row, _FieldName, bAccountID);
		}
	}

	[PXDBInt]
	[PXDefault]
	[PXUIField(DisplayName = "Branch", FieldClass = "BRANCH", Required = false)]	
	public class BranchTreeAttribute : BaseOrganizationTreeAttribute
	{		
		public BranchTreeAttribute(Type treeDataMember = null, bool onlyActive = true)			
			:base(treeDataMember, onlyActive, true, true)
		{
			this.SelectionMode = SelectionModes.Branches;
		}
	}

	[PXDBInt]
	[PXVirtualSelector(typeof(Branch.branchID))]
	public class MatchOrganizationAttribute : PXAggregateAttribute
		
	{
		// ReSharper disable InconsistentNaming
		[InjectDependencyOnTypeLevel]
		private ICurrentUserInformationProvider _currentUserInformationProvider { get; set; }
		// ReSharper restore InconsistentNaming

		public override void CacheAttached(PXCache sender)
		{
			sender.Graph.FieldSelecting.AddHandler(sender.GetItemType(), _FieldName, FieldSelecting);
			sender.Graph.FieldUpdating.AddHandler(sender.GetItemType(), _FieldName, FieldUpdating);
		}
		protected virtual void FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			if (e.NewValue is string value)
			{
				value = value.Trim();
				foreach (var org in _currentUserInformationProvider.GetOrganizations())
				{
					foreach (var branch in org.ChildBranches.Where(branch => branch.BranchCD == value))
					{
						e.NewValue = branch.BranchID;
						return;
					}

					if (org.OrganizationCD == value)
					{
						e.NewValue = 0;
						return;
					}

				}
			}
		}

		protected virtual void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			if (e.ReturnValue is int value)
			{
				foreach (var org in _currentUserInformationProvider.GetOrganizations())
				{
					foreach (var branch in org.ChildBranches.Where(branch => branch.BAccountID == (int?)value))
					{
						e.ReturnValue = branch.BranchCD;
						return;
					}

					if (org.BAccountID == value)
					{
						e.ReturnValue = org.OrganizationCD;
						return;
					}
				}
			}
		}
	}




	public class CustomerVendorRestrictorAttribute : PXRestrictorAttribute
	{
		public CustomerVendorRestrictorAttribute() : 
			base(
				typeof(Where<BAccountR.type, NotEqual<BAccountType.branchType>,
					And<BAccountR.type, NotEqual<BAccountType.organizationType>,
					And<BAccountR.type, NotEqual<BAccountType.prospectType>>>>),
				Messages.CustomerVendor)
		{
		}
	}

}
