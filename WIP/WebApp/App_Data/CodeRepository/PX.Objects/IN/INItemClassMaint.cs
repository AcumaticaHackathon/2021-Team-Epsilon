using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.DependencyInjection;
using PX.Data.Maintenance;
using PX.Data.WorkflowAPI;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.IN.Matrix.Interfaces;
using PX.Objects.IN.Matrix.Utility;
using PX.SM;

namespace PX.Objects.IN
{
	public class INItemClassMaint : PXGraph<INItemClassMaint, INItemClass>, IGraphWithInitialization, ICreateMatrixHelperFactory
	{
		#region DAC's overrides
		#region INItemClass
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[INParentItemClass(defaultStkItemFromParent: true)]
		protected virtual void _(Events.CacheAttached<INItemClass.parentItemClassID> e) { }
		#endregion

		#region RelationGroup
		[PXRemoveBaseAttribute(typeof(RelationGroup.ModuleAllAttribute))]
		[PXMergeAttributes]
		[PXStringList(
			new string[] { "PX.Objects.CS.SegmentValue", "PX.Objects.IN.InventoryItem" },
			new string[] { "Subitem", "Inventory Item Restriction" })]
		protected virtual void _(Events.CacheAttached<RelationGroup.specificType> e) { }
		#endregion

		#region InventoryItem
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDefault]
		[PXDBString(30, IsUnicode = true, IsKey = true, InputMask = "")]
		[PXUIField(DisplayName = "Inventory ID", Visibility = PXUIVisibility.SelectorVisible)]
		[PXDimensionSelector(InventoryAttribute.DimensionName, typeof(InventoryItem.inventoryCD), typeof(InventoryItem.inventoryCD))]
		protected virtual void _(Events.CacheAttached<InventoryItem.inventoryCD> e) { }

		[PXRemoveBaseAttribute(typeof(PXDefaultAttribute))]
		[PXRemoveBaseAttribute(typeof(PXUIRequiredAttribute))]
		protected virtual void _(Events.CacheAttached<InventoryItem.itemClassID> e) { }

		[PXRemoveBaseAttribute(typeof(PXSelectorAttribute))]
		protected virtual void _(Events.CacheAttached<InventoryItem.lotSerClassID> e) { }
		#endregion

		#region CSAttributeGroup
		[PXMergeAttributes]
		[PXParent(typeof(Select<INItemClass, Where<INItemClass.itemClassID, Equal<Current<CSAttributeGroup.entityClassID>>>>), LeaveChildren = true)]
		[PXDBDefault(typeof(INItemClass.itemClassStrID))]
		protected virtual void _(Events.CacheAttached<CSAttributeGroup.entityClassID> e) { }
		#endregion

		#region INUnit
		[PXCustomizeBaseAttribute(typeof(INUnitAttribute), nameof(INUnitAttribute.Visible), false)]
		[PXCustomizeBaseAttribute(typeof(INUnitAttribute), nameof(INUnitAttribute.Enabled), false)]
		protected virtual void _(Events.CacheAttached<INUnit.toUnit> e) { }

		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.Visible), true)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.Enabled), false)]
		protected virtual void _(Events.CacheAttached<INUnit.sampleToUnit> e) { }
		#endregion
		#endregion

		#region Selects
		[PXHidden]
		public
			SelectFrom<INLotSerClass>.
			View lotSerClass;

		public PXSetup<INSetup> inSetup;

		public
			SelectFrom<INItemClass>.
			View itemclass;

		public
			SelectFrom<INItemClass>.
			Where<INItemClass.itemClassID.IsEqual<INItemClass.itemClassID.FromCurrent>>.
			View itemclasssettings;

		public
			SelectFrom<INItemClass>.
			Where<INItemClass.itemClassID.IsEqual<INItemClass.itemClassID.FromCurrent>>.
			View TreeViewAndPrimaryViewSynchronizationHelper;

		public
			PXSetup<INLotSerClass>.
			Where<INLotSerClass.lotSerClassID.IsEqual<INItemClass.lotSerClassID.FromCurrent>>
			lotserclass;

		public
			INUnitSelect2<INUnit, INItemClass.itemClassID, INItemClass.salesUnit, INItemClass.purchaseUnit, INItemClass.baseUnit, INItemClass.lotSerClassID>
			classunits;

		public
			SelectFrom<INItemClassSubItemSegment>.
			Where<INItemClassSubItemSegment.FK.ItemClass.SameAsCurrent>.
			OrderBy<INItemClassSubItemSegment.segmentID.Asc>.
			View segments;
		protected virtual IEnumerable Segments() => GetSegments();

		public
			CSAttributeGroupList<INItemClass.itemClassID, InventoryItem>
			Mapping;

		public
			SelectFrom<INItemClassRep>.
			Where<INItemClassRep.itemClassID.IsEqual<INItemClass.itemClassID.AsOptional>>.
			View replenishment;

		public
			SelectFrom<SegmentValue>.
			View segmentvalue;

		public
			SelectFrom<RelationGroup>.
			View Groups;
		protected IEnumerable groups() => GetRelationGroups();

		[PXCopyPasteHiddenView]
		public
			SelectFrom<ItemClassTree.INItemClass>.
			OrderBy<ItemClassTree.INItemClass.itemClassCD.Asc>.
			View.ReadOnly ItemClassNodes;
		protected virtual IEnumerable itemClassNodes([PXInt] int? itemClassID) => ItemClassTree.EnrollNodes(itemClassID);

		public
			SelectFrom<InventoryItem>.
			Where<InventoryItem.FK.ItemClass>.
			View Items;

		public PXFilter<GoTo> goTo;
		#endregion

		#region Actions
		public ImmediatelyChangeID ChangeID;

		public PXAction<INItemClass> action;
		[PXUIField(DisplayName = "Actions", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(CommitChanges = true, SpecialType = PXSpecialButtonType.ActionsFolder, MenuAutoOpen = true)]
		protected virtual IEnumerable Action(PXAdapter adapter) => adapter.Get();

		public PXAction<INItemClass> viewGroupDetails;
		[PXButton(CommitChanges = true)]
		[PXUIField(DisplayName = Messages.ViewRestrictionGroup, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable ViewGroupDetails(PXAdapter adapter)
		{
			if (Groups.Current != null)
			{
				RelationGroups graph = CreateInstance<RelationGroups>();
				graph.HeaderGroup.Current = graph.HeaderGroup.Search<RelationHeader.groupName>(Groups.Current.GroupName);
				throw new PXRedirectRequiredException(graph, false, Messages.ViewRestrictionGroup);
			}
			return adapter.Get();
		}

		public PXAction<INItemClass> viewRestrictionGroups;
		[PXLookupButton(CommitChanges = true)]
		[PXUIField(DisplayName = GL.Messages.ViewRestrictionGroups, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable ViewRestrictionGroups(PXAdapter adapter)
		{
			if (itemclass.Current != null)
			{
				INAccessDetailByClass graph = CreateInstance<INAccessDetailByClass>();
				graph.Class.Current = graph.Class.Search<INItemClass.itemClassID>(itemclass.Current.ItemClassID);
				throw new PXRedirectRequiredException(graph, false, "Restricted Groups");
			}
			return adapter.Get();
		}

		public PXAction<INItemClass> resetGroup;
		[PXProcessButton(CommitChanges = true)]
		[PXUIField(DisplayName = Messages.ApplyRestrictionSettings)]
		protected virtual IEnumerable ResetGroup(PXAdapter adapter)
		{
			if (itemclass.Ask(Messages.Warning, CS.Messages.GroupUpdateConfirm, MessageButtons.OKCancel) == WebDialogResult.OK)
			{
				Save.Press();
				int? classID = itemclass.Current.ItemClassID;
				PXLongOperation.StartOperation(this, () => Reset(classID));
			}
			return adapter.Get();
		}
		protected static void Reset(int? classID)
		{
			INItemClassMaint graph = PXGraph.CreateInstance<INItemClassMaint>();
			INItemClass itemclass = graph.itemclass.Current = graph.itemclass.Search<INItemClass.itemClassID>(classID);
			if (itemclass != null)
			{
				PXDatabase.Update<InventoryItem>(
					new PXDataFieldRestrict<InventoryItem.itemClassID>(itemclass.ItemClassID),
					new PXDataFieldAssign<InventoryItem.groupMask>(itemclass.GroupMask));
			}
		}

		public PXAction<INItemClass> applyToChildren;
		[PXButton(CommitChanges = true)]
		[PXUIField(DisplayName = Messages.ApplyToChildren, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		protected virtual IEnumerable ApplyToChildren(PXAdapter adapter)
		{
			if (itemclass.Current != null &&
				itemclass.Ask(Messages.Confirmation, Messages.ConfirmItemClassApplyToChildren, MessageButtons.OKCancel) == WebDialogResult.OK)
			{
				Actions.PressSave();
				int? itemclassID = itemclass.Current.ItemClassID;
				PXLongOperation.StartOperation(this, () => UpdateChildren(itemclassID));
			}
			return adapter.Get();
		}

		[PXDeleteButton, PXUIField]
		protected virtual IEnumerable delete(PXAdapter adapter)
		{
			if (itemclass.Current != null)
			{
				var tree = ItemClassTree.Instance;
				var parent = tree.GetParentsOf(itemclass.Current.ItemClassID.Value).FirstOrDefault();
				IEnumerable<INItemClass> children = tree.GetAllChildrenOf(itemclass.Current.ItemClassID.Value);
				bool deleteChildren = children.Any() && itemclass.Ask(Messages.Confirmation, Messages.ConfirmItemClassDeleteKeepChildren, MessageButtons.YesNo) == WebDialogResult.No;
				using (PXTransactionScope ts = new PXTransactionScope())
				{
					if (deleteChildren)
					{
						var graph = PXGraph.CreateInstance<INItemClassMaint>();
						foreach (INItemClass child in children)
						{
							graph.itemclass.Current = graph.itemclass.Search<INItemClass.itemClassID>(child.ItemClassID);
							graph.itemclass.Delete(graph.itemclass.Current);
						}
						graph.Actions.PressSave();
					}
					itemclass.Delete(itemclass.Current);
					Actions.PressSave();

					ts.Complete();
				}

				if (parent == null)
				{
					if (itemclass.AllowInsert)
					{
						itemclass.Insert();
						itemclass.Cache.IsDirty = false;
					}
				}
				else
				{
					itemclass.Current = parent;
				}

				SelectTimeStamp();
				yield return itemclass.Current;
			}
			else
			{
				foreach (var row in adapter.Get())
					yield return row;
			}
		}

		public PXAction<INItemClass> GoToNodeSelectedInTree;
		[PXButton(CommitChanges = true), PXUIField(Visible = false, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		protected virtual IEnumerable goToNodeSelectedInTree(PXAdapter adapter)
		{
			if (itemclass.Cache.IsDirty && itemclass.View.Answer == WebDialogResult.None)
				goTo.Current.ItemClassID = ItemClassNodes.Current?.ItemClassID;

			if (itemclass.Cache.IsDirty == false || itemclass.AskExt() == WebDialogResult.OK)
			{
				Int32? goToItemClassID = itemclass.Cache.IsDirty
					? goTo.Current.ItemClassID
					: ItemClassNodes.Current?.ItemClassID;
				Actions.PressCancel();

				_forbidToSyncTreeCurrentWithPrimaryViewCurrent = true;
				itemclass.Current = SelectFrom<INItemClass>.View.Search<INItemClass.itemClassID>(this, goToItemClassID);
				SetTreeCurrent(goToItemClassID);
			}
			else
			{
				SetTreeCurrent(itemclass.Current.ItemClassID);
			}

			return new[] { itemclass.Current };
		}
		#endregion

		private readonly Lazy<bool> _timestampSelected = new Lazy<bool>(() => { PXDatabase.SelectTimeStamp(); return true; });

		#region Initialization
		public INItemClassMaint()
		{
			PXUIFieldAttribute.SetEnabled(Groups.Cache, null, false);
			PXUIFieldAttribute.SetEnabled<RelationGroup.included>(Groups.Cache, null, true);

			if (!PXAccess.FeatureInstalled<FeaturesSet.distributionModule>())
			{
				action.SetVisible(false);
				PXUIFieldAttribute.SetVisible(replenishment.Cache, null, false);
				PXUIFieldAttribute.SetVisible(itemclass.Cache, null, false);
				PXUIFieldAttribute.SetVisible(Groups.Cache, null, false);
				PXUIFieldAttribute.SetVisible<INItemClass.itemClassCD>(itemclass.Cache, null, true);
				PXUIFieldAttribute.SetVisible<INItemClass.descr>(itemclass.Cache, null, true);
				PXUIFieldAttribute.SetVisible<INItemClass.itemType>(itemclass.Cache, null, true);
				PXUIFieldAttribute.SetVisible<INItemClass.taxCategoryID>(itemclass.Cache, null, true);
				PXUIFieldAttribute.SetVisible<INItemClass.baseUnit>(itemclass.Cache, null, true);
				PXUIFieldAttribute.SetVisible<INItemClass.priceClassID>(itemclass.Cache, null, true);
			}
		}

		public virtual void Initialize()
		{
			OnBeforeCommit += graph => ((INItemClassMaint)graph).Apply(g => g.ValidateINUnit(g.itemclass.Current));
		}

		public override void Configure(PXScreenConfiguration config)
		{
			var context = config.GetScreenConfigurationContext<INItemClassMaint, INItemClass>();
			context.AddScreenConfigurationFor(screen =>
			{
				return screen
					.WithActions(actions =>
					{
						actions.Add(g => g.viewRestrictionGroups, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.applyToChildren, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.ChangeID, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.resetGroup, a => a.InFolder(FolderType.ActionsFolder));
					});
			});
		}
		#endregion

		#region Tree and Panel synchronization hack
		private bool _allowToSyncTreeCurrentWithPrimaryViewCurrent;
		private bool _forbidToSyncTreeCurrentWithPrimaryViewCurrent;
		public override IEnumerable ExecuteSelect(String viewName, Object[] parameters, Object[] searches, String[] sortcolumns, Boolean[] descendings, PXFilterRow[] filters, ref Int32 startRow, Int32 maximumRows, ref Int32 totalRows)
		{
			if (viewName == nameof(TreeViewAndPrimaryViewSynchronizationHelper))
				_allowToSyncTreeCurrentWithPrimaryViewCurrent = true;
			return base.ExecuteSelect(viewName, parameters, searches, sortcolumns, descendings, filters, ref startRow, maximumRows, ref totalRows);
		} 
		#endregion

		#region Event Handlers
		#region INItemClass
		protected virtual void _(Events.RowSelected<INItemClass> e)
		{
			bool StockItem = e.Row == null || e.Row.StkItem == true;

			INItemTypes.CustomListAttribute stringlist = StockItem
				? new INItemTypes.StockListAttribute()
				: (INItemTypes.CustomListAttribute)new INItemTypes.NonStockListAttribute();

			PXUIFieldAttribute.SetVisible<INItemClass.taxCalcMode>(itemclass.Cache, e.Row, e.Row.ItemType == INItemTypes.ExpenseItem);
			_timestampSelected.Init();

			PXStringListAttribute.SetList<INItemClass.itemType>(itemclass.Cache, e.Row, stringlist.AllowedValues, stringlist.AllowedLabels);

			PXUIFieldAttribute.SetEnabled<INItemClass.stkItem>(itemclass.Cache, e.Row, !IsDefaultItemClass(e.Row));

			PXDefaultAttribute.SetPersistingCheck<INItemClass.availabilitySchemeID>(itemclass.Cache, e.Row, StockItem ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXUIFieldAttribute.SetRequired<INItemClass.availabilitySchemeID>(itemclass.Cache, StockItem);
			PXUIFieldAttribute.SetEnabled<INItemClass.availabilitySchemeID>(itemclass.Cache, e.Row, StockItem);
			PXUIFieldAttribute.SetEnabled<INItemClass.lotSerClassID>(itemclass.Cache, e.Row, StockItem);

			SyncTreeCurrentWithPrimaryViewCurrent(e.Row);
		}

		protected virtual void _(Events.RowInserting<INItemClass> e)
		{
			if (e.Row != null && (!PXAccess.FeatureInstalled<FeaturesSet.distributionModule>() || !PXAccess.FeatureInstalled<FeaturesSet.inventory>()))
				e.Cache.SetValueExt<INItemClass.stkItem>(e.Row, false);

			ItemClassNodes.Current = null;
			ItemClassNodes.Cache.ActiveRow = null;
		}

		protected virtual void _(Events.FieldVerifying<INItemClass, INItemClass.stkItem> e)
		{
			if (e.Row == null)
				return;

			bool? newValue = (bool?)e.NewValue;

			if (e.Row.StkItem != newValue)
			{
				var inventoryItem = GetFirstItem(e.Row);
				if (inventoryItem != null)
					throw new PXSetPropertyException<INItemClass.stkItem>(Messages.StkItemValueCanNotBeChangedBecauseItIsUsedInInventoryItem, inventoryItem.InventoryCD);
			}
		}

		protected virtual void _(Events.FieldUpdated<INItemClass, INItemClass.stkItem> e)
		{
			e.Cache.SetDefaultExt<INItemClass.parentItemClassID>(e.Row);
			e.Cache.SetDefaultExt<INItemClass.itemType>(e.Row);
			e.Cache.SetDefaultExt<INItemClass.valMethod>(e.Row);
			e.Cache.SetDefaultExt<INItemClass.accrueCost>(e.Row);

			if (e.Row.StkItem != true)
			{
				e.Cache.SetValueExt<INItemClass.availabilitySchemeID>(e.Row, null);
				e.Cache.SetValueExt<INItemClass.lotSerClassID>(e.Row, null);
			}

			if (e.Cache.GetStatus(e.Row) == PXEntryStatus.Inserted)
			{
				e.Cache.SetDefaultExt<INItemClass.negQty>(e.Row);
				if (e.Row.StkItem == true)
					e.Cache.SetDefaultExt<INItemClass.availabilitySchemeID>(e.Row);
				//sales and purchase units must be cleared not to be added to item unit conversions on base unit change
				e.Cache.SetValueExt<INItemClass.baseUnit>(e.Row, null);
				e.Cache.SetValue<INItemClass.salesUnit>(e.Row, null);
				e.Cache.SetValue<INItemClass.purchaseUnit>(e.Row, null);
				e.Cache.SetDefaultExt<INItemClass.baseUnit>(e.Row);
				e.Cache.SetDefaultExt<INItemClass.salesUnit>(e.Row);
				e.Cache.SetDefaultExt<INItemClass.purchaseUnit>(e.Row);
				e.Cache.SetDefaultExt<INItemClass.postClassID>(e.Row);
				if (e.Row.StkItem == true)
					e.Cache.SetDefaultExt<INItemClass.lotSerClassID>(e.Row);
				e.Cache.SetDefaultExt<INItemClass.taxCategoryID>(e.Row);
				e.Cache.SetDefaultExt<INItemClass.deferredCode>(e.Row);
				e.Cache.SetDefaultExt<INItemClass.priceClassID>(e.Row);
				e.Cache.SetDefaultExt<INItemClass.priceWorkgroupID>(e.Row);
				e.Cache.SetDefaultExt<INItemClass.priceManagerID>(e.Row);
				e.Cache.SetDefaultExt<INItemClass.dfltSiteID>(e.Row);
				e.Cache.SetDefaultExt<INItemClass.minGrossProfitPct>(e.Row);
				e.Cache.SetDefaultExt<INItemClass.markupPct>(e.Row);
				e.Cache.SetDefaultExt<INItemClass.demandCalculation>(e.Row);
				e.Cache.SetDefaultExt<INItemClass.groupMask>(e.Row);
				InitDetailsFromParentItemClass(e.Row);
			}
		}

		protected virtual void _(Events.RowInserted<INItemClass> e)
		{
			InitDetailsFromParentItemClass(e.Row);
		}

		protected virtual void _(Events.RowDeleting<INItemClass> e)
		{
			if (IsDefaultItemClass(e.Row))
				throw new PXException(Messages.ThisItemClassCanNotBeDeletedBecauseItIsUsedInInventorySetup);

			InventoryItem inventoryItemRec = GetFirstItem(e.Row);
			if (inventoryItemRec != null)
				throw new PXException(PXMessages.LocalizeFormatNoPrefixNLA(Messages.ThisItemClassCanNotBeDeletedBecauseItIsUsedInInventoryItem, inventoryItemRec.InventoryCD));

			INLocation iNLocationRec = SelectFrom<INLocation>.Where<INLocation.FK.PrimaryItemClass.SameAsCurrent>.View.SelectWindowed(this, 0, 1);
			if (iNLocationRec != null)
			{
				INSite iNSiteRec = INSite.PK.Find(this, iNLocationRec.SiteID);
				throw new PXException(PXMessages.LocalizeFormatNoPrefixNLA(Messages.ThisItemClassCanNotBeDeletedBecauseItIsUsedInWarehouseLocation, iNSiteRec.SiteCD ?? "", iNLocationRec.LocationCD));
			}
		}

		protected virtual void _(Events.RowPersisting<INItemClass> e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				if (e.Row.ValMethod == INValMethod.Specific && lotserclass.Current != null && (lotserclass.Current.LotSerTrack == INLotSerTrack.NotNumbered || lotserclass.Current.LotSerAssign != INLotSerAssign.WhenReceived))
				{
					if (e.Cache.RaiseExceptionHandling<INItemClass.valMethod>(e.Row, INValMethod.Specific, new PXSetPropertyException(Messages.SpecificOnlyNumbered)))
					{
						throw new PXRowPersistingException(typeof(INItemClass.valMethod).Name, INValMethod.Specific, Messages.SpecificOnlyNumbered, typeof(INItemClass.valMethod).Name);
					}
				}
			}
		}

		protected virtual void _(Events.RowPersisted<INItemClass> e)
		{
			if (e.TranStatus == PXTranStatus.Completed && e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Delete, PXDBOperation.Update))
				SelectTimeStamp(); // needed to reload slot with item class tree
		}
		#endregion

		#region INItemClassRep
		protected virtual void _(Events.RowSelected<INItemClassRep> e)
		{
			if (e.Row != null)
			{
				bool isTransfer = e.Row.ReplenishmentSource == INReplenishmentSource.Transfer;
				bool isFixedReorder = e.Row.ReplenishmentMethod == INReplenishmentMethod.FixedReorder;

				PXUIFieldAttribute.SetEnabled<INItemClassRep.replenishmentMethod>(e.Cache, e.Row,
					e.Row.ReplenishmentSource.IsNotIn(INReplenishmentSource.PurchaseToOrder, INReplenishmentSource.DropShipToOrder));
				PXUIFieldAttribute.SetEnabled<INItemClassRep.replenishmentSourceSiteID>(e.Cache, e.Row,
					e.Row.ReplenishmentSource.IsIn(INReplenishmentSource.PurchaseToOrder, INReplenishmentSource.DropShipToOrder, INReplenishmentSource.Transfer, INReplenishmentSource.Purchased));
				PXUIFieldAttribute.SetEnabled<INItemClassRep.transferLeadTime>(e.Cache, e.Row, isTransfer);
				PXUIFieldAttribute.SetEnabled<INItemClassRep.transferERQ>(e.Cache, e.Row, isTransfer && isFixedReorder && e.Row.ReplenishmentMethod != INReplenishmentMethod.None);
				PXUIFieldAttribute.SetEnabled<INItemClassRep.forecastModelType>(e.Cache, e.Row, e.Row.ReplenishmentMethod != INReplenishmentMethod.None);
				PXUIFieldAttribute.SetEnabled<INItemClassRep.forecastPeriodType>(e.Cache, e.Row, e.Row.ReplenishmentMethod != INReplenishmentMethod.None);
				PXUIFieldAttribute.SetEnabled<INItemClassRep.historyDepth>(e.Cache, e.Row, e.Row.ReplenishmentMethod != INReplenishmentMethod.None);
				PXUIFieldAttribute.SetEnabled<INItemClassRep.launchDate>(e.Cache, e.Row, e.Row.ReplenishmentMethod != INReplenishmentMethod.None);
				PXUIFieldAttribute.SetEnabled<INItemClassRep.terminationDate>(e.Cache, e.Row, e.Row.ReplenishmentMethod != INReplenishmentMethod.None);
				PXUIFieldAttribute.SetEnabled<INItemClassRep.serviceLevelPct>(e.Cache, e.Row, e.Row.ReplenishmentMethod != INReplenishmentMethod.None);
				PXDefaultAttribute.SetPersistingCheck<INItemClassRep.transferLeadTime>(e.Cache, e.Row, isTransfer ? PXPersistingCheck.Null : PXPersistingCheck.Nothing);
				PXDefaultAttribute.SetPersistingCheck<INItemClassRep.transferERQ>(e.Cache, e.Row, (isTransfer && isFixedReorder) ? PXPersistingCheck.Null : PXPersistingCheck.Nothing);
			}
		}

		protected virtual void _(Events.FieldUpdated<INItemClassRep, INItemClassRep.replenishmentSource> e)
		{
			if (e.Row == null) return;
			if (e.Row.ReplenishmentSource.IsIn(INReplenishmentSource.PurchaseToOrder, INReplenishmentSource.DropShipToOrder))
				e.Cache.SetValueExt<INItemClassRep.replenishmentMethod>(e.Row, INReplenishmentMethod.None);

			if (e.Row.ReplenishmentSource.IsNotIn(INReplenishmentSource.PurchaseToOrder, INReplenishmentSource.DropShipToOrder, INReplenishmentSource.Transfer))
				e.Cache.SetDefaultExt<INItemClassRep.replenishmentSourceSiteID>(e.Row);

			e.Cache.SetDefaultExt<INItemClassRep.transferLeadTime>(e.Row);

		}
		#endregion

		#region RelationGroup
		protected virtual void _(Events.RowSelected<RelationGroup> e)
		{
			if (itemclass.Current != null && e.Row != null && Groups.Cache.GetStatus(e.Row) == PXEntryStatus.Notchanged)
			{
				// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs [Justification: virtual field]
				e.Row.Included = UserAccess.IsIncluded(itemclass.Current.GroupMask, e.Row);
			}
		}

		protected virtual void _(Events.RowPersisting<RelationGroup> e) => e.Cancel = true;
		#endregion

		#region INItemClassSubItemSegment
		protected virtual void _(Events.RowPersisting<INItemClassSubItemSegment> e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				if (e.Row.IsActive == false && string.IsNullOrEmpty(e.Row.DefaultValue))
					if (e.Cache.RaiseExceptionHandling<INItemClassSubItemSegment.defaultValue>(e.Row, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, $"[{nameof(INItemClassSubItemSegment.defaultValue)}]")))
						throw new PXRowPersistingException(typeof(INItemClassSubItemSegment.defaultValue).Name, null, ErrorMessages.FieldIsEmpty, nameof(INItemClassSubItemSegment.defaultValue));

				SegmentValue val =
					SelectFrom<SegmentValue>.
					Where<
						SegmentValue.dimensionID.IsEqual<SubItemAttribute.dimensionName>.
						And<SegmentValue.segmentID.IsEqual<@P.AsShort>>.
						And<SegmentValue.isConsolidatedValue.IsEqual<True>>>.
					View.Select(this, e.Row.SegmentID);

				if (val == null)
				{
					val =
						SelectFrom<SegmentValue>.
						Where<
							SegmentValue.dimensionID.IsEqual<SubItemAttribute.dimensionName>.
							And<SegmentValue.segmentID.IsEqual<@P.AsShort>>.
							And<SegmentValue.value.IsEqual<@P.AsString>>>.
						View.Select(this, e.Row.SegmentID, e.Row.DefaultValue);

					if (val != null)
					{
						val.IsConsolidatedValue = true;
						segmentvalue.Cache.SetStatus(val, PXEntryStatus.Updated);
						segmentvalue.Cache.IsDirty = true;
					}
				}
			}
		}
		#endregion
		#endregion

		public override void Persist()
		{
			if (itemclass.Current != null && itemclass.Current.StkItem == true && string.IsNullOrEmpty(itemclass.Current.LotSerClassID) && !PXAccess.FeatureInstalled<FeaturesSet.lotSerialTracking>())
			{
				itemclass.Current.LotSerClassID = INLotSerClass.GetDefaultLotSerClass(this);
			}

			if (itemclass.Current != null && Groups.Cache.IsDirty)
			{
				UserAccess.PopulateNeighbours(itemclass, Groups, typeof(SegmentValue));
				PXSelectorAttribute.ClearGlobalCache<INItemClass>();
			}

			base.Persist();

			Groups.Cache.Clear();
			GroupHelper.Clear();
		}

		public override int Persist(Type cacheType, PXDBOperation operation)
		{
			if (cacheType == typeof(INUnit) && operation == PXDBOperation.Update)
				base.Persist(cacheType, PXDBOperation.Insert);

			return base.Persist(cacheType, operation);
		}

		protected virtual InventoryItem GetFirstItem(INItemClass itemClass)
		{
			if (itemClass == null)
				return null;

			return SelectFrom<InventoryItem>.Where<InventoryItem.itemClassID.IsEqual<@P.AsInt>>.View.SelectWindowed(this, 0, 1, itemClass.ItemClassID);
		}

		protected virtual bool IsDefaultItemClass(INItemClass itemClass)
		{
			INSetup inSetupRec = inSetup.Select();
			return itemClass != null && inSetupRec != null && itemClass.ItemClassID.IsIn(inSetupRec.DfltNonStkItemClassID, inSetupRec.DfltStkItemClassID);
		}

		protected virtual void InitDetailsFromParentItemClass(INItemClass itemClass)
		{
			if (itemClass == null || itemClass.ParentItemClassID == null) return;

			using (new ReadOnlyScope(classunits.Cache, replenishment.Cache, Mapping.Cache))
			{
				classunits.Cache.ClearQueryCache();
				classunits.Cache.Clear();
				foreach (INUnit conv in classunits.Select(itemClass.ParentItemClassID))
				{
					var convCopy = classunits.Cache.CreateCopy(conv);
					classunits.Cache.SetValue<INUnit.recordID>(convCopy, null);
					classunits.Cache.SetValue<INUnit.itemClassID>(convCopy, itemClass.ItemClassID);
					convCopy = classunits.Cache.Insert(convCopy);
					if (convCopy == null)
						throw new PXException(Messages.CopyingSettingsFailed);
				}
				replenishment.Cache.ClearQueryCache();
				replenishment.Cache.Clear();
				foreach (INItemClassRep rep in replenishment.Select(itemClass.ParentItemClassID))
				{
					var repCopy = replenishment.Cache.CreateCopy(rep);
					replenishment.Cache.SetValue<INItemClassRep.itemClassID>(repCopy, itemClass.ItemClassID);
					repCopy = replenishment.Cache.Insert(repCopy);
					if (repCopy == null)
						throw new PXException(Messages.CopyingSettingsFailed);
				}
				Mapping.Cache.ClearQueryCache();
				Mapping.Cache.Clear();
				foreach (CSAttributeGroup attr in SelectCSAttributeGroupRecords(itemClass.ParentItemClassID.ToString()))
				{
					var attrCopy = Mapping.Cache.CreateCopy(attr);
					Mapping.Cache.SetValue<CSAttributeGroup.entityClassID>(attrCopy, itemClass.ItemClassStrID);
					attrCopy = Mapping.Cache.Insert(attrCopy);
					if (attrCopy == null)
						throw new PXException(Messages.CopyingSettingsFailed);
				}
			}
		}

		protected static void UpdateChildren(int? itemClassID)
		{
			var graph = PXGraph.CreateInstance<INItemClassMaint>();
			graph.itemclass.Current = graph.itemclass.Search<INItemClass.itemClassID>(itemClassID);
			if (graph.itemclass.Current != null)
			{
				var tree = ItemClassTree.Instance;
				IEnumerable<INItemClass> children = tree.GetAllChildrenOf(graph.itemclass.Current.ItemClassID.Value);
				if (children.Any())
				{
					using (PXTransactionScope ts = new PXTransactionScope())
					{
						var replenishmentTemplate = graph.replenishment.Select().RowCast<INItemClassRep>();
						var unitConversionsTemplate = graph.classunits.Select().RowCast<INUnit>();
						var attributesTemplate = graph.Mapping.Select().RowCast<CSAttributeGroup>();
						foreach (INItemClass child in children)
						{
							graph.UpdateChildItemClass(child);
							graph.MergeReplenishment(child, replenishmentTemplate);
							graph.MergeUnitConversions(child, unitConversionsTemplate);
							graph.MergeAttributes(child, attributesTemplate);
						}
						ts.Complete();
					}
				}
			}
		}

		protected virtual void UpdateChildItemClass(INItemClass child)
		{
			if (child.StkItem != itemclass.Current.StkItem)
			{
				var inventoryItem = GetFirstItem(child);
				if (inventoryItem != null)
				{
					throw new PXSetPropertyException<INItemClass.stkItem>(Messages.ChildStkItemValueCanNotBeChangedBecauseItIsUsedInInventoryItem,
						child.ItemClassCD, inventoryItem.InventoryCD);
				}
			}

			if (child.BaseUnit != itemclass.Current.BaseUnit)
			{
				DeleteConversionsExceptBaseConversion(child);
				UpdateBaseConversion(child, itemclass.Current.BaseUnit);
			}

			PXDatabase.Update<INItemClass>(
				new PXDataFieldRestrict<INItemClass.itemClassID>(child.ItemClassID),
				new PXDataFieldAssign<INItemClass.exportToExternal>(itemclass.Current.ExportToExternal),
				new PXDataFieldAssign<INItemClass.stkItem>(itemclass.Current.StkItem),
				new PXDataFieldAssign<INItemClass.negQty>(itemclass.Current.NegQty),
				new PXDataFieldAssign<INItemClass.accrueCost>(itemclass.Current.AccrueCost),
				new PXDataFieldAssign<INItemClass.availabilitySchemeID>(itemclass.Current.AvailabilitySchemeID),
				new PXDataFieldAssign<INItemClass.valMethod>(itemclass.Current.ValMethod),
				new PXDataFieldAssign<INItemClass.baseUnit>(itemclass.Current.BaseUnit),
				new PXDataFieldAssign<INItemClass.salesUnit>(itemclass.Current.SalesUnit),
				new PXDataFieldAssign<INItemClass.purchaseUnit>(itemclass.Current.PurchaseUnit),
				new PXDataFieldAssign<INItemClass.postClassID>(itemclass.Current.PostClassID),
				new PXDataFieldAssign<INItemClass.lotSerClassID>(itemclass.Current.LotSerClassID),
				new PXDataFieldAssign<INItemClass.taxCategoryID>(itemclass.Current.TaxCategoryID),
				new PXDataFieldAssign<INItemClass.deferredCode>(itemclass.Current.DeferredCode),
				new PXDataFieldAssign<INItemClass.itemType>(itemclass.Current.ItemType),
				new PXDataFieldAssign<INItemClass.priceClassID>(itemclass.Current.PriceClassID),
				new PXDataFieldAssign<INItemClass.priceWorkgroupID>(itemclass.Current.PriceWorkgroupID),
				new PXDataFieldAssign<INItemClass.priceManagerID>(itemclass.Current.PriceManagerID),
				new PXDataFieldAssign<INItemClass.dfltSiteID>(itemclass.Current.DfltSiteID),
				new PXDataFieldAssign<INItemClass.minGrossProfitPct>(itemclass.Current.MinGrossProfitPct),
				new PXDataFieldAssign<INItemClass.markupPct>(itemclass.Current.MarkupPct),
				new PXDataFieldAssign<INItemClass.demandCalculation>(itemclass.Current.DemandCalculation)
			);
		}

		protected virtual void DeleteConversionsExceptBaseConversion(INItemClass child)
		{
			PXDatabase.Delete<INUnit>(
				new PXDataFieldRestrict<INUnit.itemClassID>(child.ItemClassID),
				new PXDataFieldRestrict<INUnit.fromUnit>(PXDbType.NVarChar, 6, child.BaseUnit, PXComp.NE));
		}

		protected virtual void UpdateBaseConversion(INItemClass child, string newBaseUnit)
		{
			PXDatabase.Update<INUnit>(
				new PXDataFieldAssign<INUnit.toUnit>(newBaseUnit),
				new PXDataFieldAssign<INUnit.fromUnit>(newBaseUnit),
				new PXDataFieldRestrict<INUnit.itemClassID>(child.ItemClassID),
				new PXDataFieldRestrict<INUnit.toUnit>(child.BaseUnit),
				new PXDataFieldRestrict<INUnit.fromUnit>(child.BaseUnit));
		}

		protected virtual void MergeReplenishment(INItemClass child, IEnumerable<INItemClassRep> replenishmentTemplate)
		{
			if (!replenishmentTemplate.Any()) return;

			var replenishmentExisting = replenishment.Select(child.ItemClassID).RowCast<INItemClassRep>();
			foreach (INItemClassRep rep in replenishmentTemplate)
			{
				var crudParamsTemplate = new PXDataFieldAssign[]
				{
					new PXDataFieldAssign<INItemClassRep.replenishmentPolicyID>(rep.ReplenishmentPolicyID),
					new PXDataFieldAssign<INItemClassRep.replenishmentMethod>(rep.ReplenishmentMethod),
					new PXDataFieldAssign<INItemClassRep.replenishmentSource>(rep.ReplenishmentSource),
					new PXDataFieldAssign<INItemClassRep.replenishmentSourceSiteID>(rep.ReplenishmentSourceSiteID),
					new PXDataFieldAssign<INItemClassRep.launchDate>(rep.LaunchDate),
					new PXDataFieldAssign<INItemClassRep.terminationDate>(rep.TerminationDate),
					new PXDataFieldAssign<INItemClassRep.forecastModelType>(rep.ForecastModelType),
					new PXDataFieldAssign<INItemClassRep.forecastPeriodType>(rep.ForecastPeriodType),
					new PXDataFieldAssign<INItemClassRep.historyDepth>(rep.HistoryDepth),
					new PXDataFieldAssign<INItemClassRep.transferLeadTime>(rep.TransferLeadTime),
					new PXDataFieldAssign<INItemClassRep.transferERQ>(rep.TransferERQ),
					new PXDataFieldAssign<INItemClassRep.serviceLevel>(rep.ServiceLevel),
					new PXDataFieldAssign<INItemClassRep.eSSmoothingConstantL>(rep.ESSmoothingConstantL),
					new PXDataFieldAssign<INItemClassRep.eSSmoothingConstantT>(rep.ESSmoothingConstantT),
					new PXDataFieldAssign<INItemClassRep.eSSmoothingConstantS>(rep.ESSmoothingConstantS),
					new PXDataFieldAssign<INItemClassRep.autoFitModel>(rep.AutoFitModel),
					new PXDataFieldAssign<INItemClassRep.createdByID>(rep.CreatedByID),
					new PXDataFieldAssign<INItemClassRep.createdByScreenID>(rep.CreatedByScreenID),
					new PXDataFieldAssign<INItemClassRep.createdDateTime>(rep.CreatedDateTime),
					new PXDataFieldAssign<INItemClassRep.lastModifiedByID>(rep.LastModifiedByID),
					new PXDataFieldAssign<INItemClassRep.lastModifiedByScreenID>(rep.LastModifiedByScreenID),
					new PXDataFieldAssign<INItemClassRep.lastModifiedDateTime>(rep.LastModifiedDateTime),
				};

				if (replenishmentExisting.Any(r => r.ReplenishmentClassID == rep.ReplenishmentClassID))
				{
					var updateParams = new List<PXDataFieldParam>(crudParamsTemplate)
					{
						new PXDataFieldRestrict<INItemClassRep.itemClassID>(child.ItemClassID),
						new PXDataFieldRestrict<INItemClassRep.replenishmentClassID>(rep.ReplenishmentClassID)
					};

					PXDatabase.Update<INItemClassRep>(updateParams.ToArray());
				}
				else
				{
					var insertParams = new List<PXDataFieldAssign>(crudParamsTemplate)
					{
						new PXDataFieldAssign<INItemClassRep.itemClassID>(child.ItemClassID),
						new PXDataFieldAssign<INItemClassRep.replenishmentClassID>(rep.ReplenishmentClassID)
					};

					PXDatabase.Insert<INItemClassRep>(insertParams.ToArray());
				}
			}
		}

		protected virtual void MergeUnitConversions(INItemClass child, IEnumerable<INUnit> unitConversionsTemplate)
		{
			if (!unitConversionsTemplate.Any()) return;

			var unitConversionsExisting = classunits.Select(child.ItemClassID).RowCast<INUnit>();
			foreach (INUnit conv in unitConversionsTemplate)
			{
				var crudParamsTemplate = new PXDataFieldAssign[]
				{
					new PXDataFieldAssign<INUnit.unitMultDiv>(conv.UnitMultDiv),
					new PXDataFieldAssign<INUnit.unitRate>(conv.UnitRate),
					new PXDataFieldAssign<INUnit.priceAdjustmentMultiplier>(conv.PriceAdjustmentMultiplier),
					new PXDataFieldAssign<INUnit.createdByID>(conv.CreatedByID),
					new PXDataFieldAssign<INUnit.createdByScreenID>(conv.CreatedByScreenID),
					new PXDataFieldAssign<INUnit.createdDateTime>(conv.CreatedDateTime),
					new PXDataFieldAssign<INUnit.lastModifiedByID>(conv.LastModifiedByID),
					new PXDataFieldAssign<INUnit.lastModifiedByScreenID>(conv.LastModifiedByScreenID),
					new PXDataFieldAssign<INUnit.lastModifiedDateTime>(conv.LastModifiedDateTime),
				};

				if (unitConversionsExisting.Any(r => r.UnitType == conv.UnitType && r.FromUnit == conv.FromUnit && r.ToUnit == conv.ToUnit))
				{
					var updateParams = new List<PXDataFieldParam>(crudParamsTemplate)
					{
						new PXDataFieldRestrict<INUnit.itemClassID>(child.ItemClassID),
						new PXDataFieldRestrict<INUnit.inventoryID>(conv.InventoryID),
						new PXDataFieldRestrict<INUnit.unitType>(conv.UnitType),
						new PXDataFieldRestrict<INUnit.fromUnit>(conv.FromUnit),
						new PXDataFieldRestrict<INUnit.toUnit>(conv.ToUnit)
					};

					PXDatabase.Update<INUnit>(updateParams.ToArray());
				}
				else
				{
					var insertParams = new List<PXDataFieldAssign>(crudParamsTemplate)
					{
						new PXDataFieldAssign<INUnit.itemClassID>(child.ItemClassID),
						new PXDataFieldAssign<INUnit.inventoryID>(conv.InventoryID),
						new PXDataFieldAssign<INUnit.unitType>(conv.UnitType),
						new PXDataFieldAssign<INUnit.fromUnit>(conv.FromUnit),
						new PXDataFieldAssign<INUnit.toUnit>(conv.ToUnit)
					};

					PXDatabase.Insert<INUnit>(insertParams.ToArray());
				}
			}
		}

		protected virtual void MergeAttributes(INItemClass child, IEnumerable<CSAttributeGroup> attributesTemplate)
		{
			if (!attributesTemplate.Any()) return;

			var attributesExisting = SelectCSAttributeGroupRecords(child.ItemClassStrID);
			foreach (CSAttributeGroup attr in attributesTemplate)
			{
				var existingAttribute = attributesExisting.FirstOrDefault(
					r => r.AttributeID == attr.AttributeID && r.EntityType == attr.EntityType);

				MergeAttribute(child, existingAttribute, attr);
			}
		}

		protected virtual void MergeAttribute(INItemClass child, CSAttributeGroup existingAttribute, CSAttributeGroup parentAttribute)
		{
			var crudParamsTemplate = new PXDataFieldAssign[]
			{
				new PXDataFieldAssign<CSAttributeGroup.isActive>(parentAttribute.IsActive),
				new PXDataFieldAssign<CSAttributeGroup.sortOrder>(parentAttribute.SortOrder),
				new PXDataFieldAssign<CSAttributeGroup.required>(parentAttribute.Required),
				new PXDataFieldAssign<CSAttributeGroup.defaultValue>(parentAttribute.DefaultValue),
				new PXDataFieldAssign<CSAttributeGroup.attributeCategory>(parentAttribute.AttributeCategory),
				new PXDataFieldAssign<CSAttributeGroup.createdByID>(parentAttribute.CreatedByID),
				new PXDataFieldAssign<CSAttributeGroup.createdByScreenID>(parentAttribute.CreatedByScreenID),
				new PXDataFieldAssign<CSAttributeGroup.createdDateTime>(parentAttribute.CreatedDateTime),
				new PXDataFieldAssign<CSAttributeGroup.lastModifiedByID>(parentAttribute.LastModifiedByID),
				new PXDataFieldAssign<CSAttributeGroup.lastModifiedByScreenID>(parentAttribute.LastModifiedByScreenID),
				new PXDataFieldAssign<CSAttributeGroup.lastModifiedDateTime>(parentAttribute.LastModifiedDateTime),
			};

			if (existingAttribute != null)
			{
				var updateParams = new List<PXDataFieldParam>(crudParamsTemplate)
				{
					new PXDataFieldRestrict<CSAttributeGroup.entityClassID>(child.ItemClassStrID),
					new PXDataFieldRestrict<CSAttributeGroup.attributeID>(parentAttribute.AttributeID),
					new PXDataFieldRestrict<CSAttributeGroup.entityType>(parentAttribute.EntityType)
				};

				PXDatabase.Update<CSAttributeGroup>(updateParams.ToArray());
			}
			else
			{
				var insertParams = new List<PXDataFieldAssign>(crudParamsTemplate)
				{
					new PXDataFieldAssign<CSAttributeGroup.entityClassID>(child.ItemClassStrID),
					new PXDataFieldAssign<CSAttributeGroup.attributeID>(parentAttribute.AttributeID),
					new PXDataFieldAssign<CSAttributeGroup.entityType>(parentAttribute.EntityType)
				};

				PXDatabase.Insert<CSAttributeGroup>(insertParams.ToArray());
			}
		}

		protected virtual void ValidateINUnit(INItemClass itemClass)
		{
			if (itemClass == null)
				return;

			using (PXDataRecord record = PXDatabase.SelectSingle<INUnit>(
				new PXDataField<INUnit.toUnit>(),
				new PXDataFieldValue<INUnit.unitType>(INUnitType.ItemClass),
				new PXDataFieldValue<INUnit.itemClassID>(itemClass.ItemClassID),
				new PXDataFieldValue<INUnit.toUnit>(PXDbType.NVarChar, 6, itemClass.BaseUnit, PXComp.NE)))
			{
				if (record != null)
					throw new PXException(Messages.WrongItemClassToUnitValue, record.GetString(0), itemClass.ItemClassCD, itemClass.BaseUnit);
			}
		}

		private IEnumerable<CSAttributeGroup> SelectCSAttributeGroupRecords(string itemClassID)
		{
			var resultSet =
				SelectFrom<CSAttributeGroup>.
				InnerJoin<CSAttribute>.On<CSAttributeGroup.attributeID.IsEqual<CSAttribute.attributeID>>.
				Where<
					CSAttributeGroup.entityClassID.IsEqual<@P.AsString>.
					And<CSAttributeGroup.entityType.IsEqual<@P.AsString.ASCII>>>.
				View.Select(this, itemClassID, typeof(InventoryItem).FullName).AsEnumerable();

			return resultSet
				.Select(r => PXResult.Unwrap<CSAttributeGroup>(r))
				.Where(r => r != null);
		}

		private IEnumerable<RelationGroup> GetRelationGroups()
		{
			foreach (RelationGroup group in SelectFrom<RelationGroup>.View.Select(this))
			{
				if (group.SpecificModule == null || group.SpecificModule == typeof(InventoryItem).Namespace || itemclass.Current != null && UserAccess.IsIncluded(itemclass.Current.GroupMask, group))
				{
					Groups.Current = group;
					yield return group;
				}
			}
		}

		private IEnumerable<INItemClassSubItemSegment> GetSegments()
		{
			foreach (INItemClassSubItemSegment seg in segments.Cache.Updated)
				yield return seg;

			if (itemclass.Current == null || itemclass.Current.ItemClassID.GetValueOrDefault() == 0)
				yield break;

			var segs =
				SelectFrom<Segment>.
				LeftJoin<SegmentValue>.On<
					SegmentValue.dimensionID.IsEqual<Segment.dimensionID>.
					And<SegmentValue.segmentID.IsEqual<Segment.segmentID>>.
					And<SegmentValue.isConsolidatedValue.IsEqual<True>>>.
				LeftJoin<INItemClassSubItemSegment>.On<
					INItemClassSubItemSegment.itemClassID.IsEqual<INItemClass.itemClassID.FromCurrent>.
					And<INItemClassSubItemSegment.segmentID.IsEqual<Segment.segmentID>>>.
				Where<Segment.dimensionID.IsEqual<SubItemAttribute.dimensionName>>.
				View.Select(this);

			foreach (PXResult<Segment, SegmentValue, INItemClassSubItemSegment> res in segs)
			{
				INItemClassSubItemSegment seg = res;
				if (seg.SegmentID == null)
				{
					seg.SegmentID = ((Segment)res).SegmentID;
					seg.ItemClassID = itemclass.Current.ItemClassID;
					seg.IsActive = true;
				}

				seg.DefaultValue = ((SegmentValue)res).Value;
				PXUIFieldAttribute.SetEnabled<INItemClassSubItemSegment.defaultValue>(segments.Cache, seg, string.IsNullOrEmpty(seg.DefaultValue));

				bool existsNotUpdated = segments.Cache.Locate(seg) is INItemClassSubItemSegment cached && segments.Cache.GetStatus(cached) != PXEntryStatus.Notchanged;
				if (!existsNotUpdated)
					yield return seg;
			}

			segments.Cache.IsDirty = false;
		}

		private void SyncTreeCurrentWithPrimaryViewCurrent(INItemClass primaryViewCurrent)
		{
			if (_allowToSyncTreeCurrentWithPrimaryViewCurrent && !_forbidToSyncTreeCurrentWithPrimaryViewCurrent
				&& primaryViewCurrent != null && (ItemClassNodes.Current == null || ItemClassNodes.Current.ItemClassID != primaryViewCurrent.ItemClassID))
			{
				SetTreeCurrent(primaryViewCurrent.ItemClassID);
			}
		}

		private void SetTreeCurrent(Int32? itemClassID)
		{
			ItemClassTree.INItemClass current = ItemClassTree.Instance.GetNodeByID(itemClassID ?? 0);
			ItemClassNodes.Current = current;
			ItemClassNodes.Cache.ActiveRow = current;
		}

		public virtual CreateMatrixItemsHelper GetCreateMatrixItemsHelper()
			=> new CreateMatrixItemsHelper(this);


		public class ImmediatelyChangeID : PXImmediatelyChangeID<INItemClass, INItemClass.itemClassCD>
		{
			public ImmediatelyChangeID(PXGraph graph, String name) : base(graph, name) { }
			public ImmediatelyChangeID(PXGraph graph, Delegate handler) : base(graph, handler) { }

			protected override void Initialize()
			{
				base.Initialize();
				DuplicatedKeyMessage = Messages.DuplicateItemClassID;
			}
		}

		public class GoTo : IBqlTable
		{
			[PXInt]
			public Int32? ItemClassID { get; set; }
			public abstract class itemClassID : BqlInt.Field<itemClassID> { }
		}
	}

	public class ItemClassTree
		: DimensionTree<
			ItemClassTree,
			ItemClassTree.INItemClass,
			INItemClass.dimension,
			ItemClassTree.INItemClass.itemClassCD,
			ItemClassTree.INItemClass.itemClassID>
	{
		public class INItemClass : IN.INItemClass
		{
			public new abstract class itemClassID : BqlInt.Field<itemClassID> { }
			public new abstract class itemClassCD : BqlString.Field<itemClassCD> { }
			public new abstract class stkItem : BqlBool.Field<stkItem> { }

			public virtual string SegmentedClassCD { get; set; }
		}

		public string GetFullItemClassDescription(string itemClassCD)
		{
			if (itemClassCD == null) return null;
			itemClassCD = itemClassCD.TrimEnd();
			var itemClassNode = GetNodeByCD(itemClassCD);
			return itemClassNode != null
				? String.Join(" / ", GetParentsOf(itemClassCD).Reverse().Append(itemClassNode).Select(node => node.Descr ?? " "))
				: null;
		}

		protected override void PrepareElement(INItemClass original)
		{
			int length = 0;
			Segment[] segments = Segments;
			string segmentedClassCD = PadKey(original.ItemClassCD.Replace(' ', '*'), segments.Sum(s => s.Length.Value));
			foreach (Segment segment in segments.Take(segments.Length - 1))
			{
				segmentedClassCD = segmentedClassCD.Insert(length + segment.Length.Value, segment.Separator);
				length += segment.Length.Value + segment.Separator.Length;
			}
			original.SegmentedClassCD = segmentedClassCD + " " + original.Descr;
		}
	}
}