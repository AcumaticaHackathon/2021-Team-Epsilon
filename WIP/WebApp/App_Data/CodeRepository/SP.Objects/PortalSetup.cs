using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Common;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.CR.MassProcess;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.GL.DAC;
using PX.Objects.IN;
using PX.Objects.IN.DAC;
using PX.Objects.SO;
using PX.SM;
using SP.Objects;
using SP.Objects.IN;
using SP.Objects.IN.Descriptor;
using Messages = PX.Objects.SO.Messages;
using SPMessages = SP.Objects.Messages;
using SP.Objects.SP;
using SP.Objects.IN.DAC;
using Branch = PX.Objects.GL.Branch;
using MSG = SP.Objects;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Data.BQL.Fluent;
using PX.Data.SQLTree;
using PX.Objects.CR.Extensions;

namespace PX.Objects.SP.DAC
{
	[SerializableAttribute()]
	[PXPrimaryGraph(typeof(PortalSetupMaint))]
	[PXCacheName("Portal Setup")]
	public class PortalSetup : IBqlTable
	{
		public class PK : PrimaryKeyOf<PortalSetup>.By<portalSetupID>
		{
			public static PortalSetup Find(PXGraph graph, string portalSetupID) => FindBy(graph, portalSetupID);
		}

		#region PortalSiteID
		public sealed class portalSiteID : BqlString.Constant<portalSiteID>
		{
			public portalSiteID() : base(WebConfig.PortalSiteID) { }
		}

		public sealed class IsCurrentPortal : portalSetupID.Is<Equal<portalSiteID>> { }
		#endregion

		#region PortalSetupID
		public abstract class portalSetupID : BqlString.Field<portalSetupID> { }
		[PXDBString(256, IsKey = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Portal ID", Enabled = false)]
		public virtual string PortalSetupID { get; set; }
		#endregion

		#region PortalSetupName
		public abstract class portalSetupName : PX.Data.IBqlField { }
		[PXDBString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Portal Name")]
		public virtual string PortalSetupName { get; set; }
		#endregion

		#region Display Financial Documents
		public abstract class displayFinancialDocuments : IBqlField { }
		[PXDBString(1, IsFixed = true)]
		[PXDefault(FinancialDocumentsFilterAttribute.ALL)]
		[PXUIField(DisplayName = "Display Financial Documents")]
		[FinancialDocumentsFilter]
		public virtual String DisplayFinancialDocuments { get; set; }
		#endregion

		#region OrganizationID
		public abstract class restrictByOrganizationID : PX.Data.IBqlField { }
		[PXDBInt]
		[PXSelector(
			typeof(GL.DAC.Organization.organizationID),
			typeof(GL.DAC.Organization.organizationCD),
			typeof(GL.DAC.Organization.organizationName),
			SubstituteKey = typeof(GL.DAC.Organization.organizationCD),
			DescriptionField = typeof(GL.DAC.Organization.organizationName))]
		[PXRestrictor(typeof(Where<GL.DAC.Organization.active, Equal<boolTrue>>), MSG.Messages.CompanyIsNotActive, typeof(GL.DAC.Organization.organizationName))]
		[PXUIField(DisplayName = "Portal Site Company", Required = true)]
		public virtual int? RestrictByOrganizationID { get; set; }
		#endregion

		#region BranchID
		public abstract class restrictByBranchID : PX.Data.IBqlField
		{
		}

		[PXDBInt]
		[PXSelector(
			typeof(Branch.branchID),
			typeof(Branch.branchCD),
			typeof(Branch.acctName),
			typeof(Branch.organizationID),
			SubstituteKey = typeof(Branch.branchCD),
			DescriptionField = typeof(Branch.acctName))]
		[PXRestrictor(typeof(Where<Branch.active, Equal<boolTrue>>), MSG.Messages.BranchIsNotActive, typeof(Branch.acctName))]
		[PXUIField(DisplayName = "Portal Site Branch", Required = true)]
		public virtual int? RestrictByBranchID { get; set; }
		#endregion

		#region SellingBranchID
		public abstract class sellingBranchID : PX.Data.IBqlField
		{
		}

		[PXDBInt]
		[PXSelector(
			typeof(Search2<Branch.branchID,
					LeftJoin<Organization, On<Organization.organizationID, Equal<Branch.organizationID>, Or<Organization.organizationID, IsNull>>>,
					Where<Current<PortalSetup.restrictByOrganizationID>, IsNull,
								Or<Organization.organizationID, Equal<Current<PortalSetup.restrictByOrganizationID>>>>>
					),
			typeof(Branch.branchCD),
			typeof(Branch.acctName),
			typeof(Branch.organizationID),
			SubstituteKey = typeof(Branch.branchCD),
			DescriptionField = typeof(Branch.acctName), Filterable = true)]
		[PXRestrictor(typeof(Where<Branch.active, Equal<boolTrue>>), MSG.Messages.BranchIsNotActive, typeof(Branch.acctName))]
		[PXUIField(DisplayName = "Default Branch for New Orders", Required = true)]
		public virtual int? SellingBranchID { get; set; }

		#endregion

		#region DefaultCaseClassID
		public abstract class defaultCaseClassID : IBqlField { }
		protected String _DefaultCaseClassID;
		[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
		[PXUIField(DisplayName = "Default Case Class")]
		[PXSelector(typeof(Search<CRCaseClass.caseClassID,
			Where<CRCaseClass.isInternal, Equal<False>>>),
			DescriptionField = typeof(CRCaseClass.description),
			CacheGlobal = true)]
		public virtual String DefaultCaseClassID
		{
			get
			{
				return this._DefaultCaseClassID;
			}
			set
			{
				this._DefaultCaseClassID = value;
			}
		}
		#endregion

		#region Priority
		public abstract class defaultCasePriority : IBqlField { }

		[PXDBString(1, IsFixed = true)]
		[PXDefault("M")]
		[PXUIField(DisplayName = "Priority")]
		[PXStringList(new string[] { "L", "M", "H" },
			new string[] { "Low", "Medium", "High" })]
		public virtual String DefaultCasePriority { get; set; }
		#endregion

		#region CaseActivityNotificationTemplateID

		public abstract class caseActivityNotificationTemplateID : IBqlField
		{
		}

		[PXDBInt]
		[PXUIField(DisplayName = "Case Activity Notification Template")]
		[PXSelector(typeof(Notification.notificationID), SubstituteKey = typeof(Notification.name))]
		public virtual int? CaseActivityNotificationTemplateID { get; set; }
		#endregion

		#region DefaultContactClassID
		public abstract class defaultContactClassID : IBqlField { }
		[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
		[PXUIField(DisplayName = "Default Contact Class")]
		[PXSelector(typeof(CRContactClass.classID))]
		public virtual String DefaultContactClassID { get; set; }
		#endregion

		#region _DefaultStockItemWareHouse
		public abstract class defaultStockItemWareHouse : PX.Data.IBqlField
		{
		}
		protected Int32? _DefaultStockItemWareHouse;
		[PXDBInt]
		[PXSelector(typeof(Search2<INSite.siteID,
			InnerJoin<WarehouseReference, On<WarehouseReference.siteID, Equal<INSite.siteID>>>,
			Where<INSite.active, Equal<True>,
			And<INSite.siteID, NotEqual<SiteAttribute.transitSiteID>,
			And<WarehouseReference.portalSetupID, Equal<PortalSetup.portalSiteID>,
			And<Match<Current<AccessInfo.userName>>>>>>>), DescriptionField = typeof(INSite.descr), SubstituteKey = typeof(INSite.siteCD))]
		[PXUIField(DisplayName = "Default Stock Item Warehouse")]
		public virtual Int32? DefaultStockItemWareHouse
		{
			get
			{
				return this._DefaultStockItemWareHouse;
			}
			set
			{
				this._DefaultStockItemWareHouse = value;
			}
		}
		#endregion

		#region _DefaultNoNStockItemWareHouse
		public abstract class defaultnonStockItemWareHouse : PX.Data.IBqlField
		{
		}
		protected Int32? _DefaultNonStockItemWareHouse;
		[PXDBInt]
		[PXSelector(typeof(Search2<INSite.siteID,
			InnerJoin<WarehouseReference, On<WarehouseReference.siteID, Equal<INSite.siteID>>>,
			Where<INSite.active, Equal<True>,
			And<INSite.siteID, NotEqual<SiteAttribute.transitSiteID>,
			And<WarehouseReference.portalSetupID, Equal<PortalSetup.portalSiteID>,
			And<Match<Current<AccessInfo.userName>>>>>>>), DescriptionField = typeof(INSite.descr), SubstituteKey = typeof(INSite.siteCD))]
		[PXUIField(DisplayName = "Default Non-Stock Item Warehouse")]
		public virtual Int32? DefaultNonStockItemWareHouse
		{
			get
			{
				return this._DefaultNonStockItemWareHouse;
			}
			set
			{
				this._DefaultNonStockItemWareHouse = value;
			}
		}
		#endregion

		#region DefaultOrderType
		public abstract class defaultOrderType : PX.Data.IBqlField
		{
		}
		protected String _DefaultOrderType;
		[PXDBString(2, IsFixed = true, InputMask = ">aa")]
		[PXDefault(SOOrderTypeConstants.SalesOrder)]
		[PXSelector(typeof(Search2<SOOrderType.orderType, InnerJoin<SOOrderTypeOperation, On<SOOrderTypeOperation.orderType, Equal<SOOrderType.orderType>, And<SOOrderTypeOperation.operation, Equal<SOOrderType.defaultOperation>>>>>))]
		[PXRestrictor(typeof(Where<SOOrderType.active, Equal<True>>), Messages.OrderTypeInactive)]
		[PXRestrictor(typeof(Where<SOOrderTypeOperation.iNDocType, NotEqual<INTranType.transfer>, Or<FeatureInstalled<FeaturesSet.warehouse>>>), Messages.OrderTypeInactive)]
		[PXRestrictor(typeof(Where<SOOrderType.requireShipping, Equal<boolFalse>, Or<FeatureInstalled<FeaturesSet.inventory>>>), null)]
		[PXUIField(DisplayName = "Sales Order Type", Visibility = PXUIVisibility.SelectorVisible)]
		[Data.EP.PXFieldDescription]
		public virtual String DefaultOrderType
		{
			get
			{
				return this._DefaultOrderType;
			}
			set
			{
				this._DefaultOrderType = value;
			}
		}
		#endregion

		#region _BaseUOM
		public abstract class baseUOM : PX.Data.IBqlField
		{
		}
		protected Boolean? _BaseUOM;
		[PXDBBool()]
		[PXUIField(DisplayName = "Allow Only Sales Unit for Purchase")]
		[PXDefault(true, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Boolean? BaseUOM
		{
			get
			{
				return this._BaseUOM;
			}
			set
			{
				this._BaseUOM = value;
			}
		}
		#endregion

		#region AvailableQty
		public abstract class availableQty : PX.Data.IBqlField
		{
		}
		protected Boolean? _AvailableQty;
		[PXDBBool()]
		[PXUIField(DisplayName = "Show Available Quantities")]
		[PXDefault(true, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Boolean? AvailableQty
		{
			get
			{
				return this._AvailableQty;
			}
			set
			{
				this._AvailableQty = value;
			}
		}
		#endregion

		#region NoteID
		public abstract class noteID : BqlGuid.Field<noteID>
		{
		}
		protected Guid? _NoteID;
		[PXNote()]
		public virtual Guid? NoteID
		{
			get
			{
				return this._NoteID;
			}
			set
			{
				this._NoteID = value;
			}
		}
		#endregion

		#region ImageUrl
		public abstract class imageUrl : PX.Data.IBqlField
		{
		}
		protected String _ImageUrl;
		[PXDBString(255)]
		[PXUIField()]
		public virtual String ImageUrl
		{
			get
			{
				return this._ImageUrl;
			}
			set
			{
				this._ImageUrl = value;
			}
		}
		#endregion

		#region DefaultSubItemID
		public abstract class defaultSubItemID : BqlInt.Field<defaultSubItemID>
		{
		}
		protected Int32? _DefaultSubItemID;
		[SubItem(DisplayName = "Default SubItem")]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIRequired(typeof(Where<FeatureInstalled<FeaturesSet.subItem>>))]
		public virtual Int32? DefaultSubItemID
		{
			get
			{
				return _DefaultSubItemID;
			}
			set
			{
				_DefaultSubItemID = value;
			}
		}
		#endregion

		#region AddressLookupPluginID
		/// <value>
		/// <see cref="PX.Objects.CS.AddressValidatorPlugin.addressValidatorPluginID"/> of a Address Lookup which will be used. 
		/// </value>
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Address Lookup Plug-In", FieldClass = FeaturesSet.addressLookup.FieldClass)]
		[PXSelector(typeof(PX.Objects.CS.AddressValidatorPlugin.addressValidatorPluginID), DescriptionField = typeof(PX.Objects.CS.AddressValidatorPlugin.description))]
		[PXRestrictor(typeof(Where<AddressValidatorPlugin.isActive, Equal<True>, And<AddressValidatorPlugin.pluginTypeName, Contains<AddressLookupNamespaceName>>>), PX.Objects.CS.Messages.AddressLookupPluginIsNotActive)]
		public virtual String AddressLookupPluginID { get; set; }
		#endregion

		#region CreatedByID
		public abstract class createdByID : PX.Data.IBqlField
		{
		}
		protected Guid? _CreatedByID;
		[PXDBCreatedByID()]
		public virtual Guid? CreatedByID
		{
			get
			{
				return this._CreatedByID;
			}
			set
			{
				this._CreatedByID = value;
			}
		}
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.IBqlField
		{
		}
		protected String _CreatedByScreenID;
		[PXDBCreatedByScreenID()]
		public virtual String CreatedByScreenID
		{
			get
			{
				return this._CreatedByScreenID;
			}
			set
			{
				this._CreatedByScreenID = value;
			}
		}
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.IBqlField
		{
		}
		protected DateTime? _CreatedDateTime;
		[PXDBCreatedDateTime()]
		public virtual DateTime? CreatedDateTime
		{
			get
			{
				return this._CreatedDateTime;
			}
			set
			{
				this._CreatedDateTime = value;
			}
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.IBqlField
		{
		}
		protected Guid? _LastModifiedByID;
		[PXDBLastModifiedByID()]
		public virtual Guid? LastModifiedByID
		{
			get
			{
				return this._LastModifiedByID;
			}
			set
			{
				this._LastModifiedByID = value;
			}
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.IBqlField
		{
		}
		protected String _LastModifiedByScreenID;
		[PXDBLastModifiedByScreenID()]
		public virtual String LastModifiedByScreenID
		{
			get
			{
				return this._LastModifiedByScreenID;
			}
			set
			{
				this._LastModifiedByScreenID = value;
			}
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.IBqlField
		{
		}
		protected DateTime? _LastModifiedDateTime;
		[PXDBLastModifiedDateTime()]
		public virtual DateTime? LastModifiedDateTime
		{
			get
			{
				return this._LastModifiedDateTime;
			}
			set
			{
				this._LastModifiedDateTime = value;
			}
		}
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.IBqlField
		{
		}
		protected Byte[] _tstamp;
		[PXDBTimestamp()]
		public virtual Byte[] tstamp
		{
			get
			{
				return this._tstamp;
			}
			set
			{
				this._tstamp = value;
			}
		}
		#endregion

		#region Current Portal Setup Cache
		public static PortalSetup Current { get { return Definitions.CurrentSetup; } }
		internal const string PortalSetup_Prefetch_Failed = "PortalSetupID$NotConfigured";
		private static Definition Definitions
		{
			get
			{
				Definition defs = PXContext.GetSlot<Definition>();
				if (defs == null)
				{
					defs = PXContext.SetSlot<Definition>(PXDatabase.GetSlot<Definition>(typeof(Definition).FullName, typeof(PortalSetup)));
				}
				return defs;
			}
		}
		private class Definition : IPrefetchable
		{
			public PortalSetup CurrentSetup;

			public void Prefetch()
			{
				string portalSiteID = WebConfig.PortalSiteID;
				if (portalSiteID == null)
				{
					PXTrace.WriteError(SPMessages.PortalSiteIdIsNotSpecified);
					return;
				}

				using (new PXConnectionScope())
				{
					// Acuminator disable once PX1003 NonSpecificPXGraphCreateInstance [graph for select]
					var graph = new PXGraph();
					CurrentSetup = PortalSetup.PK.Find(graph, portalSiteID);

					if (CurrentSetup == null)
					{
						string legacyId = PX.Common.WebsiteID.Key;
						PortalSetup oldSetup = PortalSetup.PK.Find(graph, legacyId);

						if (oldSetup != null)
						{
							// backward compatibility before AC-142287
							PXUpdate<
								Set<PortalSetup.portalSetupID, PortalSetup.portalSiteID>,
								PortalSetup,
								Where<PortalSetup.portalSetupID.IsEqual<@P.AsString>>>
								.Update(graph, legacyId);

							PXUpdate<
								Set<WarehouseReference.portalSetupID, PortalSetup.portalSiteID>,
								WarehouseReference,
								Where<WarehouseReference.portalSetupID.IsEqual<@P.AsString>>>
								.Update(graph, legacyId);

						}
						else if((oldSetup = SelectFrom<PortalSetup>.Where<PortalSetup.portalSetupID.IsEqual<Empty>>.View.Select(graph)) != null)
						{
							// backward compatibility before AC-86860
							int? defaultSellingBranchId = null;
							switch (oldSetup.DisplayFinancialDocuments)
							{
								case FinancialDocumentsFilterAttribute.BY_COMPANY:
									if (oldSetup.RestrictByOrganizationID.HasValue)
									{
										var resultSet = PXSelectJoin<Organization,
											InnerJoin<Branch, On<Branch.organizationID, Equal<Organization.organizationID>>>,
											Where<Organization.organizationID, Equal<Required<PortalSetup.restrictByOrganizationID>>>>
											.SelectSingleBound(graph, null, oldSetup.RestrictByOrganizationID);
										if (resultSet.Count > 0)
										{
											var organization = PXResult.Unwrap<Organization>(resultSet[0]);

											if (organization != null && organization.OrganizationType.Equals(OrganizationTypes.WithoutBranches))
											{
												var branch = PXResult.Unwrap<Branch>(resultSet[0]);
												defaultSellingBranchId = branch.BranchID;
											}
										}
									}
									break;
								case FinancialDocumentsFilterAttribute.BY_BRANCH:
									defaultSellingBranchId = oldSetup.RestrictByBranchID;
									break;
							}

							PXDatabase.Update<PortalSetup>(
								new PXDataFieldAssign(typeof(PortalSetup.portalSetupID).Name, WebConfig.PortalSiteID),
								new PXDataFieldAssign(typeof(PortalSetup.sellingBranchID).Name, defaultSellingBranchId),
								new PXDataFieldRestrict(typeof(PortalSetup.portalSetupID).Name, string.Empty)
							);

							PXDatabase.Update<WarehouseReference>(
								new PXDataFieldAssign(typeof(WarehouseReference.portalSetupID).Name, WebConfig.PortalSiteID),
								new PXDataFieldRestrict(typeof(WarehouseReference.portalSetupID).Name, string.Empty)
							);

							PXDatabase.Update<PortalCardLines>(
								new PXDataFieldAssign(typeof(PortalCardLines.portalNoteID).Name, oldSetup.NoteID),
								new PXDataFieldRestrict(typeof(PortalCardLines.portalNoteID).Name, PXDbType.UniqueIdentifier, null, null, PXComp.ISNULL)
							);
						}

						// Acuminator disable once PX1003 NonSpecificPXGraphCreateInstance [graph for select]
						CurrentSetup = PortalSetup.PK.Find(new PXGraph(), portalSiteID);
					}
				}
			}
		}
		#endregion
	}
}
