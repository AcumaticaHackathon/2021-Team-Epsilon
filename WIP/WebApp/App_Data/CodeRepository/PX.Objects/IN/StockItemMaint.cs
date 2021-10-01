using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using PX.Api;
using PX.Api.Models;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;

using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.DR;
using PX.Objects.GL;
using PX.Objects.PO;
using PX.Objects.SO;
using PX.Objects.RUTROT;
using PX.Objects.Common.Discount;
using PX.SM;

using CRLocation = PX.Objects.CR.Standalone.Location;
using ItemStats = PX.Objects.IN.Overrides.INDocumentRelease.ItemStats;
using SiteStatus = PX.Objects.IN.Overrides.INDocumentRelease.SiteStatus;

namespace PX.Objects.IN
{
	public class InventoryItemMaint : InventoryItemMaintBase
	{
		public override bool IsStockItemFlag => true;

		private const string lotSerNumValueFieldName = nameof(InventoryItemLotSerNumVal.LotSerNumVal);

		#region Initialization
		public InventoryItemMaint()
		{
			Item.View = new PXView(this, false, new
				SelectFrom<InventoryItem>.
				Where<
					InventoryItem.stkItem.IsEqual<True>.
					And<InventoryItem.itemStatus.IsNotEqual<InventoryItemStatus.unknown>>.
					And<InventoryItem.isTemplate.IsEqual<False>>.
					And<MatchUser>>());

			Views[nameof(Item)] = Item.View;

			PXUIFieldAttribute.SetVisible<Vendor.curyID>(this.Caches[typeof(Vendor)], null, PXAccess.FeatureInstalled<FeaturesSet.multicurrency>());
			PXUIFieldAttribute.SetVisible<InventoryItem.pPVAcctID>(Item.Cache, null, true);
			PXUIFieldAttribute.SetVisible<InventoryItem.pPVSubID>(Item.Cache, null, true);

			itemsiterecords.Cache.AllowInsert = false;
			itemsiterecords.Cache.AllowDelete = false;

			PXUIFieldAttribute.SetEnabled(itemsiterecords.Cache, null, false);
			PXUIFieldAttribute.SetEnabled<INItemSite.isDefault>(itemsiterecords.Cache, null, true);
			PXUIFieldAttribute.SetEnabled<INItemSite.siteStatus>(itemsiterecords.Cache, null, true);

			PXUIFieldAttribute.SetEnabled(Groups.Cache, null, false);
			PXUIFieldAttribute.SetEnabled<RelationGroup.included>(Groups.Cache, null, true);

			bool enableSubItemReplenishment = PXAccess.FeatureInstalled<FeaturesSet.replenishment>() && PXAccess.FeatureInstalled<FeaturesSet.subItem>();
			subreplenishment.AllowSelect = enableSubItemReplenishment;

			PXDBDefaultAttribute.SetDefaultForInsert<INItemXRef.inventoryID>(itemxrefrecords.Cache, null, true);
			FieldDefaulting.AddHandler<BAccountR.type>((sender, e) =>
			{
				if (e.Row != null)
					e.NewValue = BAccountType.VendorType;
			});

			Item.Cache.Fields.Add(lotSerNumValueFieldName);
			FieldSelecting.AddHandler(typeof(InventoryItem), lotSerNumValueFieldName, LotSerNumValueFieldSelecting);
			FieldUpdating.AddHandler(typeof(InventoryItem), lotSerNumValueFieldName, LotSerNumValueFieldUpdating);
		}

		public override void Configure(PXScreenConfiguration config)
		{
			var context = config.GetScreenConfigurationContext<InventoryItemMaint, InventoryItem>();
			context.AddScreenConfigurationFor(screen =>
			{
				return screen
					.WithActions(actions =>
					{
						actions.Add(g => g.ChangeID, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.updateCost, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.viewRestrictionGroups, a => a.InFolder(FolderType.ActionsFolder));

						actions.Add(g => g.viewSummary, a => a.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.viewAllocationDetails, a => a.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.viewTransactionSummary, a => a.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.viewTransactionDetails, a => a.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.viewTransactionHistory, a => a.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.viewSalesPrices, a => a.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.viewVendorPrices, a => a.InFolder(FolderType.InquiriesFolder));
					});
			});
		}
		#endregion

		#region DAC overrides
		#region INItemClass
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(true)]
		protected virtual void _(Events.CacheAttached<INItemClass.stkItem> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(INItemTypes.FinishedGood, typeof(
			SearchFor<INItemClass.itemType>.
			Where<
				INItemClass.itemClassID.IsEqual<INItemClass.parentItemClassID.FromCurrent>.
				And<INItemClass.stkItem.IsEqual<True>>>),
			PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<INItemClass.itemType> e) { }
		#endregion
		#region RelationGroup
		[PXDBString(255)]
		[PXUIField(DisplayName = "Specific Type")]
		[PXStringList(
			new string[] { "PX.Objects.CS.SegmentValue", "PX.Objects.IN.InventoryItem" },
			new string[] { "Subitem", "Inventory Item Restriction" })]
		protected virtual void _(Events.CacheAttached<RelationGroup.specificType> e) { }
		#endregion
		#region InventoryItem
		[PXDefault]
		[InventoryRaw(typeof(Where<InventoryItem.stkItem.IsEqual<True>>), IsKey = true, DisplayName = "Inventory ID", Filterable = true)]
		protected virtual void _(Events.CacheAttached<InventoryItem.inventoryCD> e) { }

		[PXDBString(1, IsFixed = true, BqlField = typeof(InventoryItem.itemType))]
		[PXDefault(INItemTypes.FinishedGood, typeof(SelectFrom<INItemClass>.Where<INItemClass.itemClassID.IsEqual<InventoryItem.itemClassID.FromCurrent>>), SourceField = typeof(INItemClass.itemType), CacheGlobal = true)]
		[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible)]
		[INItemTypes.StockList]
		protected virtual void _(Events.CacheAttached<InventoryItem.itemType> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXDefault(typeof(SelectFrom<INItemClass>.Where<INItemClass.itemClassID.IsEqual<InventoryItem.itemClassID.FromCurrent>>), SourceField = typeof(INItemClass.lotSerClassID), CacheGlobal = true)]
		protected virtual void _(Events.CacheAttached<InventoryItem.lotSerClassID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXSelector(typeof(INPostClass.postClassID), DescriptionField = typeof(INPostClass.descr))]
		[PXDefault(typeof(SelectFrom<INItemClass>.Where<INItemClass.itemClassID.IsEqual<InventoryItem.itemClassID.FromCurrent>>), SourceField = typeof(INItemClass.postClassID), CacheGlobal = true)]
		protected virtual void _(Events.CacheAttached<InventoryItem.postClassID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXRestrictor(typeof(Where<INItemClass.stkItem, Equal<True>>), Messages.ItemClassIsNonStock)]
		protected virtual void _(Events.CacheAttached<InventoryItem.itemClassID> e) { }

		[PXCustomizeBaseAttribute(typeof(SubItemAttribute), nameof(SubItemAttribute.ValidateValueOnPersisting), true)]
		protected virtual void _(Events.CacheAttached<InventoryItem.defaultSubItemID> e) { }

		[PXDBBool]
		[PXDefault(false)]
		protected virtual void _(Events.CacheAttached<InventoryItem.accrueCost> e) { }
		#endregion
		#region SiteStatus
		[StockItem(IsKey = true, DirtyRead = true)]
		[PXDefault]
		protected virtual void _(Events.CacheAttached<SiteStatus.inventoryID> e) { }
		#endregion
		#region POVendorInventory
		[PXCustomizeBaseAttribute(typeof(SubItemAttribute), nameof(SubItemAttribute.ValidateValueOnPersisting), true)]
		protected virtual void _(Events.CacheAttached<POVendorInventory.subItemID> e) { }
		#endregion
		#region INItemSite
		[StockItem(IsKey = true, DirtyRead = true, CacheGlobal = false, TabOrder = 1)]
		[PXParent(typeof(INItemSite.FK.InventoryItem))]
		[PXDefault]
		protected virtual void _(Events.CacheAttached<INItemSite.inventoryID> e) { }
		[PXDBInt(IsKey = true)]
		[PXDefault]
		[ItemSite]
		[PXUIField(DisplayName = "Warehouse", Enabled = false, TabOrder = 2)]
		protected virtual void _(Events.CacheAttached<INItemSite.siteID> e) { }
		#endregion
		#region INItemCategory
		[StockItem(IsKey = true, DirtyRead = true)]
		[PXParent(typeof(INItemCategory.FK.InventoryItem))]
		[PXDBDefault(typeof(InventoryItem.inventoryID))]
		protected virtual void _(Events.CacheAttached<INItemCategory.inventoryID> e) { }
		#endregion
		#region INItemXRef
		[PXCustomizeBaseAttribute(typeof(SubItemAttribute), nameof(SubItemAttribute.ValidateValueOnPersisting), true)]
		protected virtual void _(Events.CacheAttached<INItemXRef.subItemID> e) { }
		#endregion
		#endregion

		#region Selects
		[PXHidden]
		public
			SelectFrom<INLotSerClass>.
			View lotSerClass;

		[PXHidden]
		public
			SelectFrom<Location>.
			View location; // it's needed to let Location lookup

		public
			SelectFrom<BAccount>.
			View baccount; // it's needed to let Customer lookup (Cross-reference tab) to work properly in case AlternateType = [Customer Part Number] 

		public
			SelectFrom<Vendor>.
			View vendor;

		public
			SelectFrom<Customer>.
			View customer;

		public
			SelectFrom<INItemCost>.
			Where<INItemCost.inventoryID.IsEqual<InventoryItem.inventoryID.FromCurrent>>.
			View ItemCosts;

		[PXCopyPasteHiddenView]
		public
			SelectFrom<INItemSite>.
			InnerJoin<INSite>.On<
				INItemSite.FK.Site.
				And<CurrentMatch<INSite, AccessInfo.userName>>>.
			LeftJoin<INSiteStatusSummary>.On<INSiteStatusSummary.FK.ItemSite>.
			Where<INItemSite.FK.InventoryItem.SameAsCurrent>.
			View itemsiterecords;

		public
			PXSetup<INPostClass>.
			Where<INPostClass.postClassID.IsEqual<InventoryItem.postClassID.FromCurrent>>
			postclass;

		public
			PXSetup<INLotSerClass>.
			Where<INLotSerClass.lotSerClassID.IsEqual<InventoryItem.lotSerClassID.FromCurrent>>
			lotserclass;

		public
			SelectFrom<InventoryItemLotSerNumVal>.
			Where<InventoryItemLotSerNumVal.FK.InventoryItem.SameAsCurrent>.
			View lotSerNumVal;

		public
			SelectFrom<INItemRep>.
			Where<INItemRep.FK.InventoryItem.SameAsCurrent>.
			View replenishment;

		[PXCopyPasteHiddenView]
		public
			SelectFrom<INSubItemRep>.
			Where<INSubItemRep.FK.ItemRep.SameAsCurrent>.
			View subreplenishment;

		public
			SelectFrom<INItemSiteReplenishment>.
			Where<INItemSiteReplenishment.FK.InventoryItem.SameAsCurrent>.
			View itemsitereplenihments;

		public
			SelectFrom<INItemBoxEx>.
			Where<INItemBoxEx.FK.InventoryItem.SameAsCurrent>.
			View Boxes;

		public
			SelectFrom<INSiteStatus>.
			View insitestatus;

		public
			SelectFrom<SiteStatus>.
			View sitestatus;

		public
			SelectFrom<ItemStats>.
			View itemstats;

		[PXCopyPasteHiddenView]
		public
			SelectFrom<INItemPlan>.
			Where<INItemPlan.FK.InventoryItem.SameAsCurrent>.
			View itemplans;

		[PXCopyPasteHiddenView]
		public
			SelectFrom<INSiteStatus>.
			Where<
				INSiteStatus.FK.InventoryItem.SameAsCurrent.
				And<
					INSiteStatus.qtyOnHand.IsNotEqual<decimal0>.
					Or<INSiteStatus.qtyAvail.IsNotEqual<decimal0>>>>.
			View nonemptysitestatuses;

		public
			SelectFrom<INPIClassItem>.
			View inpiclassitems;

		public
			SelectFrom<PM.PMBudget>.
			View projectBudget;

		/// <summary>
		/// This view is a workaround for Kensium tests.
		/// This view will be removed in Acumatica 7.0.
		/// </summary>
		[PXReadOnlyView]
		public
			SelectFrom<HiddenInventoryItem>.
			Where<HiddenInventoryItem.inventoryID.IsEqual<InventoryItem.inventoryID.FromCurrent>>.
			View RUTROTItemSettings;

		[PXDependToCache(typeof(InventoryItem))]
		public
			SelectFrom<RelationGroup>.
			View Groups;
		protected IEnumerable groups() => GetGroups();
		#endregion

		#region Event handlers
		#region LotSerNumVal
		protected virtual void LotSerNumValueFieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			var currentNumVal = lotSerNumVal.Current = (InventoryItemLotSerNumVal)lotSerNumVal.View.SelectSingleBound(new object[] { e.Row });
			e.ReturnState = lotSerNumVal.Cache.GetStateExt<InventoryItemLotSerNumVal.lotSerNumVal>(currentNumVal);
			INLotSerClass lotSerClass = (INLotSerClass)PXSelectorAttribute.Select<InventoryItem.lotSerClassID>(sender, e.Row);
			if (lotSerClass != null && lotSerClass.LotSerTrack != INLotSerTrack.NotNumbered)
			{
				e.ReturnValue = lotSerClass.LotSerNumShared == true
						? INLotSerClassLotSerNumVal.PK.Find(sender.Graph, lotSerClass.LotSerClassID)?.LotSerNumVal
						: currentNumVal?.LotSerNumVal;
			}
		}

		protected virtual void LotSerNumValueFieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			var inventory = (InventoryItem)e.Row;
			if (inventory == null)
				return;
			var newNumValue = (string)e.NewValue;
			var currentNumVal = (InventoryItemLotSerNumVal)lotSerNumVal.View.SelectSingleBound(new object[] { e.Row });
			var oldNumValue = currentNumVal?.LotSerNumVal;
			if (!sender.ObjectsEqual(oldNumValue, newNumValue))
			{
				var lsClass = (INLotSerClass)PXSelectorAttribute.Select<InventoryItem.lotSerClassID>(sender, e.Row);
				if (lsClass == null)
					return;
				SetLotSerNumber(currentNumVal, newNumValue);
			}
		}

		private void SetLotSerNumber(InventoryItemLotSerNumVal inventoryNumVal, string newNumber)
		{
			if (inventoryNumVal == null)
			{
				if (!string.IsNullOrEmpty(newNumber))
					lotSerNumVal.Insert(new InventoryItemLotSerNumVal
					{
						LotSerNumVal = newNumber
					});
			}
			else
			{
				if (string.IsNullOrWhiteSpace(newNumber))
					lotSerNumVal.Delete(inventoryNumVal);
				else
				{
					var copy = (InventoryItemLotSerNumVal)lotSerNumVal.Cache.CreateCopy(inventoryNumVal);
					copy.LotSerNumVal = newNumber;
					lotSerNumVal.Cache.Update(copy);
				}
			}
		}
		#endregion
		#region InventoryItem
		protected override void _(Events.RowSelected<InventoryItem> e)
		{
			base._(e);

			if (e.Row == null) return;

			INLotSerClass lotSerClass = (INLotSerClass)PXSelectorAttribute.Select<InventoryItem.lotSerClassID>(e.Cache, e.Row);
			if (lotSerClass == null)
				PXUIFieldAttribute.SetEnabled<InventoryItemLotSerNumVal.lotSerNumVal>(lotSerNumVal.Cache, null, false);
			else
				PXUIFieldAttribute.SetEnabled<InventoryItemLotSerNumVal.lotSerNumVal>(lotSerNumVal.Cache, lotSerNumVal.Current,
					!(lotSerClass.LotSerNumShared == true) && lotSerClass.LotSerTrack != INLotSerTrack.NotNumbered);

			e.Cache.AdjustUI(e.Row)
				.For<InventoryItem.valMethod>(fa => fa.Enabled = (e.Row.TemplateItemID == null))
				.SameFor<InventoryItem.kitItem>();

			PXUIFieldAttribute.SetEnabled<InventoryItem.cOGSSubID>(e.Cache, e.Row, postclass.Current != null && postclass.Current.COGSSubFromSales == false);
			PXUIFieldAttribute.SetEnabled<InventoryItem.stdCstVarAcctID>(e.Cache, e.Row, e.Row != null && e.Row.ValMethod == INValMethod.Standard);
			PXUIFieldAttribute.SetEnabled<InventoryItem.stdCstVarSubID>(e.Cache, e.Row, e.Row != null && e.Row.ValMethod == INValMethod.Standard);
			PXUIFieldAttribute.SetEnabled<InventoryItem.stdCstRevAcctID>(e.Cache, e.Row, e.Row != null && e.Row.ValMethod == INValMethod.Standard);
			PXUIFieldAttribute.SetEnabled<InventoryItem.stdCstRevSubID>(e.Cache, e.Row, e.Row != null && e.Row.ValMethod == INValMethod.Standard);
			PXUIFieldAttribute.SetEnabled<InventoryItem.pendingStdCost>(e.Cache, e.Row, e.Row != null && e.Row.ValMethod == INValMethod.Standard);
			PXUIFieldAttribute.SetEnabled<InventoryItem.pendingStdCostDate>(e.Cache, e.Row, e.Row != null && e.Row.ValMethod == INValMethod.Standard);
			PXUIFieldAttribute.SetVisible<InventoryItem.defaultSubItemOnEntry>(e.Cache, null, insetup.Current.UseInventorySubItem == true);
			PXUIFieldAttribute.SetEnabled<InventoryItem.defaultSubItemID>(e.Cache, e.Row, insetup.Current.UseInventorySubItem == true);
			INAcctSubDefault.Required(e.Cache, e.Args);
			bool hasremainder = nonemptysitestatuses.SelectSingle() != null;
			PXUIFieldAttribute.SetEnabled<InventoryItem.baseUnit>(e.Cache, e.Row, !hasremainder && e.Row.TemplateItemID == null);

			Boxes.Cache.AllowInsert = e.Row.PackageOption != INPackageOption.Manual && PXAccess.FeatureInstalled<FeaturesSet.autoPackaging>();
			Boxes.Cache.AllowUpdate = e.Row.PackageOption != INPackageOption.Manual && PXAccess.FeatureInstalled<FeaturesSet.autoPackaging>();
			Boxes.Cache.AllowSelect = e.Row.PackageOption != INPackageOption.Manual && PXAccess.FeatureInstalled<FeaturesSet.autoPackaging>();

			PXUIFieldAttribute.SetEnabled<InventoryItem.packSeparately>(Item.Cache, Item.Current, e.Row.PackageOption == INPackageOption.Weight);
			PXUIFieldAttribute.SetVisible<INItemBoxEx.qty>(Boxes.Cache, null, e.Row.PackageOption == INPackageOption.Quantity);
			PXUIFieldAttribute.SetVisible<INItemBoxEx.uOM>(Boxes.Cache, null, e.Row.PackageOption == INPackageOption.Quantity);
			PXUIFieldAttribute.SetVisible<INItemBoxEx.maxQty>(Boxes.Cache, null, e.Row.PackageOption.IsIn(INPackageOption.Weight, INPackageOption.WeightAndVolume));
			PXUIFieldAttribute.SetVisible<INItemBoxEx.maxWeight>(Boxes.Cache, null, e.Row.PackageOption.IsIn(INPackageOption.Weight, INPackageOption.WeightAndVolume));
			PXUIFieldAttribute.SetVisible<INItemBoxEx.maxVolume>(Boxes.Cache, null, e.Row.PackageOption == INPackageOption.WeightAndVolume);

			if (PXAccess.FeatureInstalled<FeaturesSet.autoPackaging>())
				ValidatePackaging(e.Row);
		}

		protected virtual void _(Events.FieldVerifying<InventoryItem, InventoryItem.lotSerClassID> e)
		{
			if (IsQtyStillPresent(this, e.Row.InventoryID))
			{
				INLotSerClass oldClass = (INLotSerClass)PXSelectorAttribute.Select<InventoryItem.lotSerClassID>(e.Cache, e.Row);
				INLotSerClass newClass = (INLotSerClass)PXSelectorAttribute.Select<InventoryItem.lotSerClassID>(e.Cache, e.Row, e.NewValue);
				if (newClass == null ||
					oldClass.LotSerTrack != newClass.LotSerTrack ||
					oldClass.LotSerTrackExpiration != newClass.LotSerTrackExpiration ||
					oldClass.LotSerAssign != newClass.LotSerAssign)
				{
					throw new PXSetPropertyException(Messages.ItemLotSerClassVerifying);
				}
			}
		}

		public static bool IsQtyStillPresent(PXGraph graph, int? inventoryID)
		{
			INItemLotSerial status =
				SelectFrom<INItemLotSerial>.
				Where<
					INItemLotSerial.inventoryID.IsEqual<@P.AsInt>.
					And<INItemLotSerial.qtyOnHand.IsNotEqual<decimal0>>>.
				View.SelectWindowed(graph, 0, 1, inventoryID);

			INSiteStatus sitestatus =
				SelectFrom<INSiteStatus>.
				Where<
					INSiteStatus.inventoryID.IsEqual<@P.AsInt>.
					And<
						INSiteStatus.qtyOnHand.IsNotEqual<decimal0>.
						Or<INSiteStatus.qtyINReceipts.IsNotEqual<decimal0>>.
						Or<INSiteStatus.qtyInTransit.IsNotEqual<decimal0>>.
						Or<INSiteStatus.qtyINIssues.IsNotEqual<decimal0>>.
						Or<INSiteStatus.qtyINAssemblyDemand.IsNotEqual<decimal0>>.
						Or<INSiteStatus.qtyINAssemblySupply.IsNotEqual<decimal0>>>>.
				View.SelectWindowed(graph, 0, 1, inventoryID);

			return status != null || sitestatus != null;
		}

		protected virtual void _(Events.FieldVerifying<InventoryItem, InventoryItem.defaultSubItemID> e)
		{
			if (IsImport)
				e.Cancel = true;
		}

		protected virtual void _(Events.RowUpdated<InventoryItem> e)
		{
			foreach (PXResult<INItemSite, INSite, INSiteStatusSummary> res in itemsiterecords.Select())
			{
				INItemSite itemsite = res;
				INSite site = res;
				INPostClass pclass = postclass.Current;
				bool hasChanges = false;
				bool IsInserted = itemsiterecords.Cache.GetStatus(itemsite) == PXEntryStatus.Inserted;
				e.Cache.RaiseRowSelected(e.Row);

				if (string.Equals(e.Row.ValMethod, e.OldRow.ValMethod) == false || itemsiterecords.Cache.GetStatus(itemsite) == PXEntryStatus.Inserted)
				{
					itemsite.ValMethod = e.Row.ValMethod;
					hasChanges = true;
				}

				if (itemsite.ValMethod == INValMethod.Standard && itemsite.StdCostOverride == false)
				{
					if (e.Row.PendingStdCostDate != null)
					{
						itemsite.PendingStdCost = e.Row.PendingStdCost;
						itemsite.PendingStdCostDate = e.Row.PendingStdCostDate;
						itemsite.PendingStdCostReset = false;
					}
					else
					{
						bool isSameCost = e.Row.StdCost == itemsite.StdCost;
						itemsite.PendingStdCost = isSameCost ? e.Row.PendingStdCost : e.Row.StdCost;
						itemsite.PendingStdCostDate = null;
						itemsite.PendingStdCostReset = !isSameCost;
					}

					hasChanges = true;
				}

				if (itemsite.BasePriceOverride == false || itemsiterecords.Cache.GetStatus(itemsite) == PXEntryStatus.Inserted)
				{
					itemsite.BasePrice = e.Row.BasePrice;
					hasChanges = true;
				}

				if (itemsite.MarkupPctOverride == false || itemsiterecords.Cache.GetStatus(itemsite) == PXEntryStatus.Inserted)
				{
					itemsite.MarkupPct = e.Row.MarkupPct;
					hasChanges = true;
				}

				if (itemsite.RecPriceOverride == false || itemsiterecords.Cache.GetStatus(itemsite) == PXEntryStatus.Inserted)
				{
					itemsite.RecPrice = e.Row.RecPrice;
					hasChanges = true;
				}

				if (itemsite.ABCCodeOverride == false || itemsiterecords.Cache.GetStatus(itemsite) == PXEntryStatus.Inserted)
				{
					itemsite.ABCCodeID = e.Row.ABCCodeID;
					itemsite.ABCCodeIsFixed = e.Row.ABCCodeIsFixed;
					hasChanges = true;
				}

				if (itemsite.MovementClassOverride == false || itemsiterecords.Cache.GetStatus(itemsite) == PXEntryStatus.Inserted)
				{
					itemsite.MovementClassID = e.Row.MovementClassID;
					itemsite.MovementClassIsFixed = e.Row.MovementClassIsFixed;
					hasChanges = true;
				}

				if (itemsite.PreferredVendorOverride == false || itemsiterecords.Cache.GetStatus(itemsite) == PXEntryStatus.Inserted)
				{
					itemsite.PreferredVendorID = e.Row.PreferredVendorID;
					itemsite.PreferredVendorLocationID = e.Row.PreferredVendorLocationID;
					hasChanges = true;
				}

				if (string.Equals(e.Row.SalesUnit, e.OldRow.SalesUnit) == false || itemsiterecords.Cache.GetStatus(itemsite) == PXEntryStatus.Inserted)
				{
					itemsite.DfltSalesUnit = e.Row.SalesUnit;
					hasChanges = true;
				}

				if (string.Equals(e.Row.PurchaseUnit, e.OldRow.PurchaseUnit) == false || itemsiterecords.Cache.GetStatus(itemsite) == PXEntryStatus.Inserted)
				{
					itemsite.DfltPurchaseUnit = e.Row.PurchaseUnit;
					hasChanges = true;
				}

				if (INItemSiteMaint.DefaultItemReplenishment(this, itemsite))
					hasChanges = true;

				if (itemsite.ProductManagerOverride != true && (itemsite.ProductManagerID != e.Row.ProductManagerID || itemsite.ProductWorkgroupID != e.Row.ProductWorkgroupID))
				{
					itemsite.ProductManagerID = e.Row.ProductManagerID;
					itemsite.ProductWorkgroupID = e.Row.ProductWorkgroupID;
					hasChanges = true;
				}

				if (hasChanges)
					itemsiterecords.Cache.MarkUpdated(itemsite);
			}

			if (!e.Cache.ObjectsEqual<InventoryItem.lotSerClassID>(e.Row, e.OldRow))
			{
				INLotSerClass lsClass = (INLotSerClass)PXSelectorAttribute.Select<InventoryItem.lotSerClassID>(e.Cache, e.Row);
				if (lsClass != null)
				{
					var current = lotSerNumVal.Current ?? (InventoryItemLotSerNumVal)lotSerNumVal.View.SelectSingleBound(new object[] { lsClass });
					if (lsClass.LotSerTrack == INLotSerTrack.NotNumbered)
					{
						SetLotSerNumber(current, null);
					}
					else
					{
						if (current == null)
						{
							InventoryItemLotSerNumVal previous = lotSerNumVal.Cache.Deleted.OfType<InventoryItemLotSerNumVal>().FirstOrDefault();
							if (previous != null)
							{
								SetLotSerNumber(current, previous.LotSerNumVal);
								return;
							}
						}
						else if (!string.IsNullOrEmpty(current.LotSerNumVal))
							return;

						SetLotSerNumber(current, "000000");
					}
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.dfltSiteID> e)
		{
			INItemSite itemsite =
				SelectFrom<INItemSite>.
				Where<
					INItemSite.inventoryID.IsEqual<InventoryItem.inventoryID.FromCurrent>.
					And<INItemSite.siteID.IsEqual<@P.AsInt>>>.
				View.Select(this, e.Row.DfltSiteID);

			INSite site = INSite.PK.Find(this, e.Row.DfltSiteID);

			if (itemsite != null)
			{
				itemsite = PXCache<INItemSite>.CreateCopy(itemsite);
				itemsite.IsDefault = true;
				itemsiterecords.Update(itemsite);

				//DfltSiteID should follow locations in DAC
				e.Row.DfltShipLocationID = itemsite.DfltShipLocationID;
				e.Row.DfltReceiptLocationID = itemsite.DfltReceiptLocationID;
			}
			else if (site != null)
			{
				itemsite = new INItemSite();
				itemsite.InventoryID = e.Row.InventoryID;
				itemsite.SiteID = e.Row.DfltSiteID;
				INItemSiteMaint.DefaultItemSiteByItem(this, itemsite, e.Row, site, postclass.Current);
				itemsite.IsDefault = true;
				itemsite.StdCostOverride = false;
				itemsite.DfltReceiptLocationID = site.ReceiptLocationID;
				itemsite.DfltShipLocationID = site.ShipLocationID;
				itemsiterecords.Insert(itemsite);

				//default item locations in this case too
				e.Row.DfltShipLocationID = itemsite.DfltShipLocationID; // already set from site
				e.Row.DfltReceiptLocationID = itemsite.DfltReceiptLocationID;
			}
			else
			{
				e.Row.DfltShipLocationID = null;
				e.Row.DfltReceiptLocationID = null;

				foreach (INItemSite rec in itemsiterecords.Select())
				{
					if (rec.IsDefault == true)
					{
						rec.IsDefault = false;
						itemsiterecords.Cache.MarkUpdated(rec);
					}
				}
			}
		}

		protected virtual void _(Events.RowPersisting<InventoryItem> e)
		{
			base._(e);
			INAcctSubDefault.Required(e.Cache, e.Args);

			if (e.Row.IsSplitted == true)
			{
				if (string.IsNullOrEmpty(e.Row.DeferredCode))
					if (e.Cache.RaiseExceptionHandling<InventoryItem.deferredCode>(e.Row, e.Row.DeferredCode, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, $"[{nameof(InventoryItem.deferredCode)}]")))
						throw new PXRowPersistingException(nameof(InventoryItem.deferredCode), e.Row.DeferredCode, ErrorMessages.FieldIsEmpty, nameof(InventoryItem.deferredCode));

				var components = Components.Select().RowCast<INComponent>().ToList();

				VerifyComponentPercentages(e.Cache, e.Row, components);
				VerifyOnlyOneResidualComponent(e.Cache, e.Row, components);
				CheckSameTermOnAllComponents(Components.Cache, components);
			}

			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
				if (e.Row.ValMethod == INValMethod.Specific && lotserclass.Current != null && (lotserclass.Current.LotSerTrack == INLotSerTrack.NotNumbered || lotserclass.Current.LotSerAssign != INLotSerAssign.WhenReceived))
					if (e.Cache.RaiseExceptionHandling<InventoryItem.valMethod>(e.Row, INValMethod.Specific, new PXSetPropertyException(Messages.SpecificOnlyNumbered)))
						throw new PXRowPersistingException(typeof(InventoryItem.valMethod).Name, INValMethod.Specific, Messages.SpecificOnlyNumbered, typeof(InventoryItem.valMethod).Name);

			if (e.Operation.Command() == PXDBOperation.Delete)
			{
				// Acuminator disable once PX1043 SavingChangesInEventHandlers [Justification]
				PXDatabase.Delete<INSiteStatus>(
					new PXDataFieldRestrict<INSiteStatus.inventoryID>(PXDbType.Int, e.Row.InventoryID),
					new PXDataFieldRestrict<INSiteStatus.qtyOnHand>(PXDbType.Decimal, 8, 0m, PXComp.EQ),
					new PXDataFieldRestrict<INSiteStatus.qtyAvail>(PXDbType.Decimal, 8, 0m, PXComp.EQ)
					);

				// Acuminator disable once PX1043 SavingChangesInEventHandlers [Justification]
				PXDatabase.Delete<INLocationStatus>(
					new PXDataFieldRestrict<INLocationStatus.inventoryID>(PXDbType.Int, e.Row.InventoryID),
					new PXDataFieldRestrict<INLocationStatus.qtyOnHand>(PXDbType.Decimal, 8, 0m, PXComp.EQ),
					new PXDataFieldRestrict<INLocationStatus.qtyAvail>(PXDbType.Decimal, 8, 0m, PXComp.EQ)
					);

				// Acuminator disable once PX1043 SavingChangesInEventHandlers [Justification]
				PXDatabase.Delete<INLotSerialStatus>(
					new PXDataFieldRestrict<INLotSerialStatus.inventoryID>(PXDbType.Int, e.Row.InventoryID),
					new PXDataFieldRestrict<INLotSerialStatus.qtyOnHand>(PXDbType.Decimal, 8, 0m, PXComp.EQ),
					new PXDataFieldRestrict<INLotSerialStatus.qtyAvail>(PXDbType.Decimal, 8, 0m, PXComp.EQ)
					);

				// Acuminator disable once PX1043 SavingChangesInEventHandlers [Justification]
				PXDatabase.Delete<INCostStatus>(
					new PXDataFieldRestrict<INCostStatus.inventoryID>(PXDbType.Int, e.Row.InventoryID),
					new PXDataFieldRestrict<INCostStatus.qtyOnHand>(PXDbType.Decimal, 8, 0m, PXComp.EQ)
					);

				// Acuminator disable once PX1043 SavingChangesInEventHandlers [Justification]
				PXDatabase.Delete<INItemCostHist>(
					new PXDataFieldRestrict<INItemCostHist.inventoryID>(PXDbType.Int, e.Row.InventoryID),
					new PXDataFieldRestrict<INItemCostHist.finYtdQty>(PXDbType.Decimal, 8, 0m, PXComp.EQ),
					new PXDataFieldRestrict<INItemCostHist.finYtdCost>(PXDbType.Decimal, 8, 0m, PXComp.EQ)
					);

				// Acuminator disable once PX1043 SavingChangesInEventHandlers [Justification]
				PXDatabase.Delete<INItemSiteHist>(
					new PXDataFieldRestrict<INItemSiteHist.inventoryID>(PXDbType.Int, e.Row.InventoryID),
					new PXDataFieldRestrict<INItemSiteHist.finYtdQty>(PXDbType.Decimal, 8, 0m, PXComp.EQ)
					);

				// Acuminator disable once PX1043 SavingChangesInEventHandlers [Justification]
				PXDatabase.Delete<CSAnswers>(new PXDataFieldRestrict<CSAnswers.refNoteID>(PXDbType.UniqueIdentifier, e.Row.NoteID));
			}

			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				INLotSerClass cls = lotserclass.Current;
				if (cls != null && cls.LotSerTrack != INLotSerTrack.NotNumbered && cls.LotSerNumShared == false)
				{
					var fieldState = (PXStringState)e.Cache.GetValueExt(e.Row, lotSerNumValueFieldName);
					if (fieldState == null || fieldState.Value == null)
					{
						INLotSerSegment lsSegment =
							SelectFrom<INLotSerSegment>.
							Where<
								INLotSerSegment.lotSerClassID.IsEqual<@P.AsString>.
								And<INLotSerSegment.segmentType.IsEqual<@P.AsString.ASCII>>>.
							View.ReadOnly.Select(this, cls.LotSerClassID, INLotSerSegmentType.NumericVal);

						if (lsSegment != null)
						{
							var exception = new PXSetPropertyException(ErrorMessages.FieldIsEmpty, fieldState.DisplayName);
							PXUIFieldAttribute.SetError<InventoryItemLotSerNumVal.lotSerNumVal>(lotSerNumVal.Cache, null, exception.Message);
						}
					}
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.dfltReceiptLocationID> e)
		{
			INItemSite itemsite =
				SelectFrom<INItemSite>.
				Where<
					INItemSite.inventoryID.IsEqual<InventoryItem.inventoryID.FromCurrent>.
					And<INItemSite.siteID.IsEqual<InventoryItem.dfltSiteID.FromCurrent>>>.
				View.Select(this);

			if (itemsite != null)
			{
				itemsite.DfltReceiptLocationID = e.Row.DfltReceiptLocationID;
				itemsiterecords.Cache.MarkUpdated(itemsite);
			}
		}

		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.dfltShipLocationID> e)
		{
			INItemSite itemsite =
				SelectFrom<INItemSite>.
				Where<
					INItemSite.inventoryID.IsEqual<InventoryItem.inventoryID.FromCurrent>.
					And<INItemSite.siteID.IsEqual<InventoryItem.dfltSiteID.FromCurrent>>>.
				View.Select(this);

			if (itemsite != null)
			{
				itemsite.DfltShipLocationID = e.Row.DfltShipLocationID;
				itemsiterecords.Cache.MarkUpdated(itemsite);
			}
		}

		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.defaultSubItemID> e) => AddVendorDetail(e.Row);

		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.preferredVendorLocationID> e) => AddVendorDetail(e.Row);

		private POVendorInventory AddVendorDetail(InventoryItem row)
		{
			if (row == null || row.PreferredVendorID == null || row.DefaultSubItemID == null)
				return null;

			POVendorInventory item =
				SelectFrom<POVendorInventory>.
				Where<
					POVendorInventory.inventoryID.IsEqual<@P.AsInt>.
					And<POVendorInventory.subItemID.IsEqual<@P.AsInt>>.
					And<POVendorInventory.vendorID.IsEqual<@P.AsInt>>.
					And<
						POVendorInventory.vendorLocationID.IsEqual<@P.AsInt>.
						Or<POVendorInventory.vendorLocationID.IsNull>>>.
				View.SelectWindowed(this, 0, 1, row.InventoryID, row.DefaultSubItemID, row.PreferredVendorID, row.PreferredVendorLocationID);
			if (item == null)
			{
				item = new POVendorInventory
				{
					InventoryID = row.InventoryID,
					SubItemID = row.DefaultSubItemID,
					PurchaseUnit = row.PurchaseUnit,
					VendorID = row.PreferredVendorID,
					VendorLocationID = row.PreferredVendorLocationID
				};
				item = (POVendorInventory)VendorItems.Cache.Insert(item);
			}
			return item;
		}

		protected override void _(Events.FieldUpdated<InventoryItem, InventoryItem.itemClassID> e)
		{
			base._(e);

			if (doResetDefaultsOnItemClassChange)
			{
				e.Cache.SetDefaultExt<InventoryItem.lotSerClassID>(e.Row);
				e.Cache.SetDefaultExt<InventoryItem.valMethod>(e.Row);
			}

			AppendGroupMask(e.Row.ItemClassID, e.Cache.GetStatus(e.Row) == PXEntryStatus.Inserted);

			if (e.Row != null && e.Row.ItemClassID != null && e.ExternalCall)
				Answers.Cache.Clear();

			if (e.Cache.GetStatus(e.Row) == PXEntryStatus.Inserted)
			{
				foreach (INItemRep r in replenishment.Select(e.Row.InventoryID))
					replenishment.Delete(r);

				foreach (INItemClassRep r in
					SelectFrom<INItemClassRep>.
					Where<INItemClassRep.itemClassID.IsEqual<@P.AsInt>>.
					View.Select(this, e.Row.ParentItemClassID))
				{
					var ri = new INItemRep
					{
						ReplenishmentClassID = r.ReplenishmentClassID,
						ReplenishmentMethod = r.ReplenishmentMethod,
						ReplenishmentPolicyID = r.ReplenishmentPolicyID,
						LaunchDate = r.LaunchDate,
						TerminationDate = r.TerminationDate
					};
					replenishment.Insert(ri);
				}
			}
		}

		protected virtual void _(Events.RowInserted<InventoryItem> e)
		{
			e.Row.TotalPercentage = 100;
			foreach (INItemClassRep r in
				SelectFrom<INItemClassRep>.
				Where<INItemClassRep.itemClassID.IsEqual<@P.AsInt>>.
				View.Select(this, e.Row.ParentItemClassID))
			{
				var ri = new INItemRep
				{
					ReplenishmentClassID = r.ReplenishmentClassID,
					ReplenishmentMethod = r.ReplenishmentMethod,
					ReplenishmentPolicyID = r.ReplenishmentPolicyID,
					LaunchDate = r.LaunchDate,
					TerminationDate = r.TerminationDate
				};
				replenishment.Insert(ri);
			}

			INItemClass ic = ItemClass.Select();
			if (e.Row.InventoryCD != null && e.Row.ItemClassID != null && e.Row.DfltSiteID == ic.DfltSiteID)
				e.Cache.SetDefaultExt<InventoryItem.dfltSiteID>(e.Row);

			AppendGroupMask(e.Row.ItemClassID, true);
			_JustInserted = true;
		}

		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.postClassID> e)
		{
			e.Cache.SetDefaultExt<InventoryItem.invtAcctID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.invtSubID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.salesAcctID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.salesSubID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.cOGSAcctID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.cOGSSubID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.stdCstVarAcctID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.stdCstVarSubID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.stdCstRevAcctID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.stdCstRevSubID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.pPVAcctID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.pPVSubID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.pOAccrualAcctID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.pOAccrualSubID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.reasonCodeSubID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.lCVarianceAcctID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.lCVarianceSubID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.deferralAcctID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.deferralSubID>(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.purchaseUnit> e)
		{
			if (e.Row == null || string.Equals(e.Row.PurchaseUnit, (string)e.OldValue, StringComparison.InvariantCultureIgnoreCase))
				return;

			var result =
				SelectFrom<POVendorInventory>.
				Where<
					POVendorInventory.inventoryID.IsEqual<@P.AsInt>.
					And<
						POVendorInventory.purchaseUnit.IsEqual<@P.AsString>.
						Or<POVendorInventory.purchaseUnit.IsEqual<@P.AsString>>>>.
				View.Select(this, e.Row.InventoryID, e.Row.PurchaseUnit, e.OldValue).AsEnumerable().RowCast<POVendorInventory>();

			foreach (POVendorInventory detailWithOldPurchaseUnit in result.Where(x => x.PurchaseUnit == (string)e.OldValue))
			{
				POVendorInventory existing = result.FirstOrDefault(x => x.PurchaseUnit == e.Row.PurchaseUnit && x.VendorID == detailWithOldPurchaseUnit.VendorID);

				if (existing == null)
				{
					if (detailWithOldPurchaseUnit.LastPrice != null)
						detailWithOldPurchaseUnit.LastPrice = POItemCostManager.ConvertUOM(this, e.Row, (string)e.OldValue, detailWithOldPurchaseUnit.LastPrice.Value, e.Row.PurchaseUnit);

					detailWithOldPurchaseUnit.PurchaseUnit = e.Row.PurchaseUnit;
					VendorItems.Update(detailWithOldPurchaseUnit);
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<InventoryItem, InventoryItem.valMethod> e)
		{
			if (e.Row.ValMethod != null && string.Equals(e.Row.ValMethod, (string)e.NewValue) == false)
			{
				INCostStatus coststatus =
					SelectFrom<INCostStatus>.
					Where<
						INCostStatus.FK.InventoryItem.SameAsCurrent.
						And<INCostStatus.qtyOnHand.IsNotEqual<decimal0>>>.
					View.ReadOnly.Select(this)
					.OrderBy(layer => (((INCostStatus)layer).CostSiteID == insetup.Current.TransitSiteID) ? 1 : 0)
					.FirstOrDefault();

				if (coststatus != null)
				{
					var listattr = e.Cache.GetAttributesReadonly<InventoryItem.valMethod>(e.Row).OfType<INValMethod.ListAttribute>().First();

					listattr.ValueLabelDic.TryGetValue(e.Row.ValMethod, out string oldval);
					listattr.ValueLabelDic.TryGetValue((string)e.NewValue, out string newval);

					throw new PXSetPropertyException(
						coststatus.CostSiteID == insetup.Current.TransitSiteID
							? Messages.ValMethodCannotBeChangedTransit
							: Messages.ValMethodCannotBeChanged,
						oldval, newval);
				}
			}
		}

		protected virtual void _(Events.RowDeleting<InventoryItem> e)
		{
			if (e.Row == null)
				return;
			var sitestat = nonemptysitestatuses.SelectSingle();
			if (sitestat != null)
				throw new PXException(Messages.ItemHasStockRemainder, e.Row.InventoryCD, insitestatus.GetValueExt<INSiteStatus.siteID>(sitestat));
		}

		protected virtual void _(Events.FieldVerifying<InventoryItem, InventoryItem.packageOption> e)
		{
			if (e.Row != null && e.NewValue.ToString() == INPackageOption.Quantity && Boxes.Select().Count == 0)
				e.Cache.RaiseExceptionHandling<InventoryItem.packageOption>(e.Row, e.NewValue, new PXSetPropertyException(Messages.BoxesRequired, PXErrorLevel.Warning));
		}

		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.packageOption> e)
		{
			if (e.Row == null) return;

			if (e.Row.PackageOption == INPackageOption.Quantity)
			{
				e.Row.PackSeparately = true;
			}
			else if (e.Row.PackageOption == INPackageOption.WeightAndVolume)
			{
				e.Row.PackSeparately = false;
			}
			else if (e.Row.PackageOption == INPackageOption.Manual)
			{
				e.Row.PackSeparately = false;

				foreach (INItemBoxEx box in Boxes.Select())
					Boxes.Delete(box);
			}
		}

		protected virtual void _(Events.RowPersisted<InventoryItem> e)
		{
			if (e.TranStatus == PXTranStatus.Completed)
				DiscountEngine.RemoveFromCachedInventoryPriceClasses(e.Row.InventoryID);
		}
		#endregion
		#region INItemCost
		protected virtual void _(Events.RowSelected<INItemCost> e)
		{
			if (e.Row == null) return;
			bool lastCostEnabled = Item.Current.ValMethod.IsNotIn(INValMethod.Standard, INValMethod.Specific) && (!PXAccess.FeatureInstalled<FeaturesSet.warehouse>() || itemsiterecords.SelectSingle(e.Row.InventoryID) != null);
			PXUIFieldAttribute.SetEnabled<INItemCost.lastCost>(e.Cache, e.Row, lastCostEnabled);
		}

		protected virtual void _(Events.RowInserted<INItemCost> e)
		{
			if (e.Row != null && e.Row.LastCost != 0m && e.Row.LastCost != null)
				UdateLastCost(e.Row);
		}

		protected virtual void _(Events.RowUpdated<INItemCost> e)
		{
			if (e.Row != null && e.OldRow != null && e.Row.LastCost != e.OldRow.LastCost && e.Row.LastCost != null)
				UdateLastCost(e.Row);
		}

		private void UdateLastCost(INItemCost row)
		{
			foreach (ItemStats stats in itemstats.Cache.Inserted)
				itemstats.Cache.Delete(stats);

			foreach (INItemSite itemsite in itemsiterecords.Select(row.InventoryID))
			{
				var stats = new ItemStats
				{
					InventoryID = itemsite.InventoryID,
					SiteID = itemsite.SiteID
				};

				stats = itemstats.Insert(stats);
				stats.LastCost = row.LastCost;
				stats.LastCostDate = DateTime.Now;
			}
		}
		#endregion
		#region INSubItemRep
		protected virtual void _(Events.RowInserted<INSubItemRep> e) => UpdateSubItemSiteReplenishment(e.Row, PXDBOperation.Insert);
		protected virtual void _(Events.RowUpdated<INSubItemRep> e) => UpdateSubItemSiteReplenishment(e.Row, PXDBOperation.Update);
		protected virtual void _(Events.RowDeleted<INSubItemRep> e) => UpdateSubItemSiteReplenishment(e.Row, PXDBOperation.Delete);

		private void UpdateSubItemSiteReplenishment(INSubItemRep row, PXDBOperation operation)
		{
			if (row == null || row.InventoryID == null || row.SubItemID == null) return;

			foreach (INItemSite itemsite in
				SelectFrom<INItemSite>.
				Where<
					INItemSite.inventoryID.IsEqual<@P.AsInt>.
					And<INItemSite.replenishmentClassID.IsEqual<@P.AsString>>.
					And<INItemSite.subItemOverride.IsEqual<False>>>.
				OrderBy<INItemSite.inventoryID.Asc>.
				View.Select(this, row.InventoryID, row.ReplenishmentClassID))
			{
				PXCache source = Caches[typeof(INItemSiteReplenishment)];
				INItemSiteReplenishment r =
					SelectFrom<INItemSiteReplenishment>.
					Where<
						INItemSiteReplenishment.inventoryID.IsEqual<@P.AsInt>.
						And<INItemSiteReplenishment.siteID.IsEqual<@P.AsInt>>.
						And<INItemSiteReplenishment.subItemID.IsEqual<@P.AsInt>>>.
					View.SelectWindowed(this, 0, 1, row.InventoryID, itemsite.SiteID, row.SubItemID);

				if (r == null)
				{
					if (operation == PXDBOperation.Delete)
						continue;

					operation = PXDBOperation.Insert;
					r = new INItemSiteReplenishment
					{
						InventoryID = row.InventoryID,
						SiteID = itemsite.SiteID,
						SubItemID = row.SubItemID
					};
				}
				else
					r = PXCache<INItemSiteReplenishment>.CreateCopy(r);

				r.SafetyStock = row.SafetyStock;
				r.MinQty = row.MinQty;
				r.MaxQty = row.MaxQty;
				r.TransferERQ = row.TransferERQ;
				r.ItemStatus = row.ItemStatus;

				switch (operation)
				{
					case PXDBOperation.Insert:
						source.Insert(r);
						break;
					case PXDBOperation.Update:
						source.Update(r);
						break;
					case PXDBOperation.Delete:
						source.Delete(r);
						break;
				}
			}
		} 
		#endregion
		#region ItemStats
		Dictionary<int?, int?> _persisted = new Dictionary<int?, int?>();

		protected virtual void _(Events.RowPersisting<ItemStats> e)
		{
			if (e.Operation.Command() == PXDBOperation.Insert && e.Row.InventoryID < 0 && Item.Current != null)
			{
				int? _KeyToAbort = (int?)Item.Cache.GetValue<InventoryItem.inventoryID>(Item.Current);
				if (!_persisted.ContainsKey(_KeyToAbort))
					_persisted.Add(_KeyToAbort, ((ItemStats)e.Row).InventoryID);

				e.Row.InventoryID = _KeyToAbort;
				e.Cache.Normalize();
			}
		}

		protected virtual void _(Events.RowPersisted<ItemStats> e)
		{
			if (e.TranStatus == PXTranStatus.Aborted && e.Operation.Command() == PXDBOperation.Insert)
			{
				int? _KeyToAbort;
				if (_persisted.TryGetValue(e.Row.InventoryID, out _KeyToAbort))
					e.Row.InventoryID = _KeyToAbort;
			}
		}
		#endregion
		#region Vendor
		protected virtual void _(Events.FieldSelecting<Vendor, Vendor.curyID> e)
		{
			if (e.ReturnValue == null)
				e.ReturnValue = this.Company.Current.BaseCuryID;
		}
		#endregion
		#region INItemXRef
		protected virtual void _(Events.ExceptionHandling<INItemXRef, INItemXRef.bAccountID> e)
		{
			if (e.Row != null && e.Row.BAccountID == null && (e.NewValue is int intVal && intVal == 0 || e.NewValue is string strVal && strVal == "0"))
			{
				e.Row.BAccountID = 0;
				e.Cancel = true;
			}
		}
		#endregion
		#region INItemSite
		protected virtual void _(Events.RowInserted<INItemSite> e)
		{
			if (e.Row.IsDefault == true)
				SetSiteDefault(e.Row);

			if (e.Row != null && insetup.Current.UseInventorySubItem != true && e.Row.InventoryID != null && e.Row.SiteID != null)
			{
				var sitem = new SiteStatus
				{
					InventoryID = e.Row.InventoryID,
					SiteID = e.Row.SiteID
				};
				sitestatus.Insert(sitem);
			}
		}

		protected virtual void _(Events.RowUpdated<INItemSite> e)
		{
			if (e.OldRow.IsDefault != e.Row.IsDefault)
				SetSiteDefault(e.Row);
		}

		protected virtual void _(Events.RowSelected<INItemSite> e)
		{
			if (e.Row != null)
			{
				bool isTransfer = (e.Row != null) && INReplenishmentSource.IsTransfer(e.Row.ReplenishmentSource);
				if (isTransfer && e.Row.ReplenishmentSourceSiteID == e.Row.SiteID)
				{
					e.Cache.RaiseExceptionHandling<INItemSite.replenishmentSourceSiteID>(e.Row, e.Row.ReplenishmentSourceSiteID, new PXSetPropertyException(Messages.ReplenishmentSourceSiteMustBeDifferentFromCurrenSite, PXErrorLevel.Warning));
				}
				else
				{
					e.Cache.RaiseExceptionHandling<INItemSite.replenishmentSourceSiteID>(e.Row, e.Row.ReplenishmentSourceSiteID, null);
				}
			}

			if (e.Row != null && e.Row.InvtAcctID == null)
			{
				INSite insite = INSite.PK.Find(this, e.Row.SiteID);

				try
				{
					INItemSiteMaint.DefaultInvtAcctSub(this, e.Row, Item.Current, insite, postclass.Current);
				}
				catch (PXMaskArgumentException) { }
			}
		}

		protected virtual void _(Events.CommandPreparing<INItemSite.invtAcctID> e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update) && ((INItemSite)e.Row).OverrideInvtAcctSub != true)
				e.Args.ExcludeFromInsertUpdate();
		}

		protected virtual void _(Events.CommandPreparing<INItemSite.invtSubID> e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update) && ((INItemSite)e.Row).OverrideInvtAcctSub != true)
				e.Args.ExcludeFromInsertUpdate();
		}

		protected virtual void _(Events.FieldVerifying<INItemSite, INItemSite.inventoryID> e) => e.Cancel = true;
		#endregion
		#region RelationGroup
		protected virtual void _(Events.RowSelected<RelationGroup> e)
		{
			if (Item.Current != null && e.Row != null && Groups.Cache.GetStatus(e.Row) == PXEntryStatus.Notchanged)
			{
				// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs [Justification: virtual field]
				e.Row.Included = UserAccess.IsIncluded(Item.Current.GroupMask, e.Row);
			}
		}

		protected virtual void _(Events.RowPersisting<RelationGroup> e) => e.Cancel = true;
		#endregion
		#region INItemRep
		protected virtual void _(Events.RowSelected<INItemRep> e)
		{
			if (e.Row != null)
			{
				bool isTransfer = INReplenishmentSource.IsTransfer(e.Row.ReplenishmentSource);
				PXUIFieldAttribute.SetEnabled<INItemRep.replenishmentMethod>(e.Cache, e.Row,
					e.Row.ReplenishmentSource.IsNotIn(INReplenishmentSource.PurchaseToOrder, INReplenishmentSource.DropShipToOrder));
				PXUIFieldAttribute.SetEnabled<INItemRep.replenishmentSourceSiteID>(e.Cache, e.Row,
					e.Row.ReplenishmentSource.IsIn(INReplenishmentSource.PurchaseToOrder, INReplenishmentSource.DropShipToOrder, INReplenishmentSource.Transfer, INReplenishmentSource.Purchased));
				PXUIFieldAttribute.SetEnabled<INItemRep.maxShelfLife>(e.Cache, e.Row, e.Row.ReplenishmentMethod != INReplenishmentMethod.None);
				PXUIFieldAttribute.SetEnabled<INItemRep.launchDate>(e.Cache, e.Row, e.Row.ReplenishmentMethod != INReplenishmentMethod.None);
				PXUIFieldAttribute.SetEnabled<INItemRep.terminationDate>(e.Cache, e.Row, e.Row.ReplenishmentMethod != INReplenishmentMethod.None);
				PXUIFieldAttribute.SetEnabled<INItemRep.serviceLevelPct>(e.Cache, e.Row, e.Row.ReplenishmentMethod != INReplenishmentMethod.None);
				PXUIFieldAttribute.SetEnabled<INItemRep.safetyStock>(e.Cache, e.Row, e.Row.ReplenishmentMethod != INReplenishmentMethod.None);
				PXUIFieldAttribute.SetEnabled<INItemRep.minQty>(e.Cache, e.Row, e.Row.ReplenishmentMethod != INReplenishmentMethod.None);
				PXUIFieldAttribute.SetEnabled<INItemRep.maxQty>(e.Cache, e.Row, e.Row.ReplenishmentMethod != INReplenishmentMethod.None);
				PXUIFieldAttribute.SetEnabled<INItemRep.forecastModelType>(e.Cache, e.Row, e.Row.ReplenishmentMethod != INReplenishmentMethod.None);
				PXUIFieldAttribute.SetEnabled<INItemRep.forecastPeriodType>(e.Cache, e.Row, e.Row.ReplenishmentMethod != INReplenishmentMethod.None);
				PXUIFieldAttribute.SetEnabled<INItemRep.historyDepth>(e.Cache, e.Row, e.Row.ReplenishmentMethod != INReplenishmentMethod.None);
				PXUIFieldAttribute.SetEnabled<INItemRep.transferERQ>(e.Cache, e.Row, isTransfer && e.Row.ReplenishmentMethod == INReplenishmentMethod.FixedReorder);
				PXUIFieldAttribute.SetEnabled<INSubItemRep.transferERQ>(subreplenishment.Cache, null, isTransfer && e.Row.ReplenishmentMethod == INReplenishmentMethod.FixedReorder);
			}
			subreplenishment.Cache.AllowInsert = e.Row != null && (string.IsNullOrEmpty(e.Row.ReplenishmentClassID) == false) && insetup.Current.UseInventorySubItem == true;
		}

		protected virtual void _(Events.RowInserted<INItemRep> e)
		{
			if (e.Row != null && e.Row.ReplenishmentClassID != null)
				UpdateItemSiteReplenishment(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<INItemRep, INItemRep.replenishmentSource> e)
		{
			if (e.Row == null) return;

			if (e.Row.ReplenishmentSource.IsIn(INReplenishmentSource.PurchaseToOrder, INReplenishmentSource.DropShipToOrder))
				e.Cache.SetValueExt<INItemRep.replenishmentMethod>(e.Row, INReplenishmentMethod.None);

			if (PXAccess.FeatureInstalled<FeaturesSet.warehouse>() && e.Row.ReplenishmentSource.IsNotIn(INReplenishmentSource.PurchaseToOrder, INReplenishmentSource.DropShipToOrder, INReplenishmentSource.Transfer))
				e.Cache.SetDefaultExt<INItemRep.replenishmentSourceSiteID>(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<INItemRep, INItemRep.replenishmentMethod> e)
		{
			if (e.Row == null) return;
			if (e.Row.ReplenishmentMethod == INReplenishmentMethod.None)
			{
				e.Cache.SetDefaultExt<INItemRep.maxShelfLife>(e.Row);
				e.Cache.SetDefaultExt<INItemRep.launchDate>(e.Row);
				e.Cache.SetDefaultExt<INItemRep.terminationDate>(e.Row);
				e.Cache.SetDefaultExt<INItemRep.serviceLevelPct>(e.Row);
				e.Cache.SetDefaultExt<INItemRep.safetyStock>(e.Row);
				e.Cache.SetDefaultExt<INItemRep.minQty>(e.Row);
				e.Cache.SetDefaultExt<INItemRep.maxQty>(e.Row);
				e.Cache.SetDefaultExt<INItemRep.forecastModelType>(e.Row);
				e.Cache.SetDefaultExt<INItemRep.forecastPeriodType>(e.Row);
				e.Cache.SetDefaultExt<INItemRep.historyDepth>(e.Row);
			}
		}

		protected virtual void _(Events.RowUpdated<INItemRep> e)
		{
			if (e.Row == null) return;
			if (INReplenishmentSource.IsTransfer(e.Row.ReplenishmentSource) == false)
				e.Row.ReplenishmentSourceSiteID = null;

			UpdateItemSiteReplenishment(e.Row);
		}

		protected virtual void _(Events.RowDeleted<INItemRep> e)
		{
			if (e.Row == null) return;
			var def = new INItemRep
			{
				ReplenishmentClassID = e.Row.ReplenishmentClassID
			};
			UpdateItemSiteReplenishment(def);
		}

		private void UpdateItemSiteReplenishment(INItemRep r)
		{
			foreach (PXResult<INItemSite, INSite> rec in itemsiterecords.Select())
			{
				INItemSite itemsite = rec;
				if (itemsite.ReplenishmentPolicyOverride == true || itemsite.ReplenishmentClassID.IsNotIn(null, r.ReplenishmentClassID))
					continue;

				bool hasChanges = false;
				if (itemsite.ReplenishmentPolicyOverride == false)
				{
					itemsite.ReplenishmentClassID = r.ReplenishmentClassID;
					itemsite.ReplenishmentPolicyID = r.ReplenishmentPolicyID;
					itemsite.ReplenishmentSource = r.ReplenishmentSource;
					itemsite.ReplenishmentSourceSiteID = r.ReplenishmentSourceSiteID;
					itemsite.ReplenishmentMethod = r.ReplenishmentMethod;
					hasChanges = true;
				}

				if (itemsite.MaxShelfLifeOverride == false || itemsiterecords.Cache.GetStatus(itemsite) == PXEntryStatus.Inserted)
				{
					itemsite.MaxShelfLife = r.MaxShelfLife;
					hasChanges = true;
				}

				if (itemsite.LaunchDateOverride == false || itemsiterecords.Cache.GetStatus(itemsite) == PXEntryStatus.Inserted)
				{
					itemsite.LaunchDate = r.LaunchDate;
					hasChanges = true;
				}

				if (itemsite.TerminationDateOverride == false || itemsiterecords.Cache.GetStatus(itemsite) == PXEntryStatus.Inserted)
				{
					itemsite.TerminationDate = r.TerminationDate;
					hasChanges = true;
				}

				if (itemsite.SafetyStockOverride == false || itemsiterecords.Cache.GetStatus(itemsite) == PXEntryStatus.Inserted)
				{
					itemsite.SafetyStock = r.SafetyStock;
					hasChanges = true;
				}

				if (itemsite.MinQtyOverride == false || itemsiterecords.Cache.GetStatus(itemsite) == PXEntryStatus.Inserted)
				{
					itemsite.MinQty = r.MinQty;
					hasChanges = true;
				}

				if (itemsite.MaxQtyOverride == false || itemsiterecords.Cache.GetStatus(itemsite) == PXEntryStatus.Inserted)
				{
					itemsite.MaxQty = r.MaxQty;
					hasChanges = true;
				}

				if (itemsite.TransferERQOverride == false || itemsiterecords.Cache.GetStatus(itemsite) == PXEntryStatus.Inserted)
				{
					itemsite.TransferERQ = r.TransferERQ;
					hasChanges = true;
				}

				if (hasChanges)
					itemsiterecords.Cache.MarkUpdated(itemsite);
			}
		}
		#endregion
		#region INItemBoxEx
		protected virtual void _(Events.RowSelected<INItemBoxEx> e)
		{
			if (e.Row == null || Item.Current == null) return;

			if (Item.Current.PackageOption.IsIn(INPackageOption.Weight, INPackageOption.WeightAndVolume))
			{
				// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs [Justification: virtual field]
				e.Row.MaxQty = CalculateMaxQtyInBox(Item.Current, e.Row);
			}
		}

		protected virtual void _(Events.RowInserted<INItemBoxEx> e)
		{
			if (e.Row == null) return;

			CSBox box = CSBox.PK.Find(this, e.Row.BoxID);
			if (box != null)
			{
				e.Row.MaxWeight = box.MaxWeight;
				e.Row.MaxVolume = box.MaxVolume;
				e.Row.BoxWeight = box.BoxWeight;
				e.Row.Description = box.Description;
			}

			if (Item.Current.PackageOption.IsIn(INPackageOption.Weight, INPackageOption.WeightAndVolume))
			{
				e.Row.MaxQty = CalculateMaxQtyInBox(Item.Current, e.Row);
			}
		}

		protected virtual void _(Events.FieldVerifying<INItemBoxEx, INItemBoxEx.uOM> e)
		{
			if (e.Row != null && e.NewValue != null && Item.Current is InventoryItem inventory)
			{
				INUnit conversion = INUnit.UK.ByInventory.FindDirty(e.Cache.Graph, inventory.InventoryID, inventory.BaseUnit);
				if (conversion == null)
					throw new PXSetPropertyException(ErrorMessages.ValueDoesntExistOrNoRights, nameof(INItemBoxEx.uOM), e.NewValue);
			}
			e.Cancel = true;
		}
		#endregion
		#region POVendorInventory
		protected virtual void _(Events.FieldVerifying<POVendorInventory, POVendorInventory.purchaseUnit> e)
		{
			if (e.Row == null) return;

			foreach (INUnit unit in Caches[typeof(INUnit)].Inserted)
				if (unit.UnitType == INUnitType.InventoryItem && unit.InventoryID == e.Row.InventoryID && string.Equals(unit.FromUnit, (string)e.NewValue, StringComparison.InvariantCultureIgnoreCase))
					e.Cancel = true;
		}
		#endregion
		#endregion

		protected virtual void AppendGroupMask(int? itemClassID, bool clear)
		{
			if (itemClassID.GetValueOrDefault() != 0)
			{
				INItemClass ic =
					SelectFrom<INItemClass>.
					Where<INItemClass.itemClassID.IsEqual<@P.AsInt>>.
					View.Select(this, itemClassID);
				if (ic != null && ic.GroupMask != null)
				{
					if (clear)
						Groups.Cache.Clear();

					foreach (RelationGroup group in Groups.Select())
					{
						for (int i = 0; i < group.GroupMask.Length && i < ic.GroupMask.Length; i++)
						{
							if (group.Included != true && group.GroupMask[i] != 0x00 && (ic.GroupMask[i] & group.GroupMask[i]) == group.GroupMask[i])
							{
								group.Included = true;
								Groups.Cache.MarkUpdated(group);
								Groups.Cache.IsDirty = true;
								break;
							}
						}
					}
				}
			}
		}

		protected bool _JustInserted;
		public override bool IsDirty => (!_JustInserted || IsContractBasedAPI) && base.IsDirty;

		protected virtual void SetSiteDefault(INItemSite itemsite)
		{
			InventoryItem item = InventoryItem.PK.FindDirty(this, itemsite.InventoryID);

			if (item != null)
			{
				item.DfltSiteID = itemsite.IsDefault == true ? itemsite.SiteID : null;
				item.DfltReceiptLocationID = itemsite.IsDefault == true ? itemsite.DfltReceiptLocationID : null;
				item.DfltShipLocationID = itemsite.IsDefault == true ? itemsite.DfltShipLocationID : null;

				Item.Cache.MarkUpdated(item);
			}

			bool IsRefreshNeeded = false;

			foreach (INItemSite rec in itemsiterecords.Select())
			{
				if (object.Equals(rec.SiteID, itemsite.SiteID) == false && (bool)rec.IsDefault)
				{
					rec.IsDefault = false;
					itemsiterecords.Cache.MarkUpdated(rec);

					IsRefreshNeeded = true;
				}
			}

			if (IsRefreshNeeded)
				itemsiterecords.View.RequestRefresh();
		}

		public override void Persist()
		{
			if (Item.Current != null)
			{
				if (string.IsNullOrEmpty(Item.Current.LotSerClassID) && !PXAccess.FeatureInstalled<FeaturesSet.lotSerialTracking>())
					Item.Current.LotSerClassID = INLotSerClass.GetDefaultLotSerClass(this);

				if (Groups.Cache.IsDirty)
				{
					UserAccess.PopulateNeighbours(Item.Cache, Item.Current,
						new PXDataFieldValue[] {
							new PXDataFieldValue(typeof(InventoryItem.inventoryID).Name, PXDbType.Int, 4, Item.Current.InventoryID, PXComp.NE)
						},
						Groups,
						typeof(SegmentValue));
					PXSelectorAttribute.ClearGlobalCache<InventoryItem>();
				}
			}

			foreach (INItemSiteReplenishment repl in itemsitereplenihments.Cache.Inserted)
			{
				sitestatus.Insert(new SiteStatus
				{
					InventoryID = repl.InventoryID,
					SubItemID = repl.SubItemID,
					SiteID = repl.SiteID,
					PersistEvenZero = true
				});
			}

			base.Persist();

			Groups.Cache.Clear();
			GroupHelper.Clear();
		}

		public override void CopyPasteGetScript(bool isImportSimple, List<Command> script, List<Container> containers)
		{
			base.CopyPasteGetScript(isImportSimple, script, containers);
			if (DisableCopyPastingSubitems())
			{
				var indexesToRemove = script.SelectIndexesWhere(_
					=> IsMatchingPatternWithTrailingNumber(_.ObjectName, INSubItemSegmentValueList.SubItemViewsPattern))
					.Reverse();

				foreach (int i in indexesToRemove)
				{
					script.RemoveAt(i);
					containers.RemoveAt(i);
				}
			}

			script.Where(_ => _.ObjectName == nameof(itemxrefrecords)).ForEach(_ => _.Commit = false);
			script.Where(_ => _.ObjectName == nameof(itemxrefrecords)).Last().Commit = true;

			foreach (SubItemAttribute attr in ItemSettings.Cache.GetAttributesReadonly<InventoryItem.defaultSubItemID>()
				.Concat(VendorItems.Cache.GetAttributesReadonly<POVendorInventory.subItemID>())
				.Concat(itemxrefrecords.Cache.GetAttributesReadonly<INItemXRef.subItemID>())
				.OfType<SubItemAttribute>())
			{
				attr.ValidateValueOnFieldUpdating = false;
			}
		}

		protected virtual bool DisableCopyPastingSubitems()
		{
			// exclude from Copy-Paste because big number of Subitem segments leads to timeout (exponential growth)
			return SegmentValues.SegmentsNumber > 1;
		}

		protected virtual bool IsMatchingPatternWithTrailingNumber(string input, string pattern)
		{
			return (input?.Length > pattern.Length) && Regex.IsMatch(input, string.Format("^{0}[0-9]+$", pattern));
		}

		protected virtual void ValidatePackaging(InventoryItem row)
		{
			PXUIFieldAttribute.SetError<InventoryItem.weightUOM>(Item.Cache, row, null);
			PXUIFieldAttribute.SetError<InventoryItem.baseItemWeight>(Item.Cache, row, null);
			PXUIFieldAttribute.SetError<InventoryItem.volumeUOM>(Item.Cache, row, null);
			PXUIFieldAttribute.SetError<InventoryItem.baseItemVolume>(Item.Cache, row, null);

			//validate weight & volume:
			if (row.PackageOption.IsIn(INPackageOption.Weight, INPackageOption.WeightAndVolume))
			{
				if (string.IsNullOrEmpty(row.WeightUOM))
					Item.Cache.RaiseExceptionHandling<InventoryItem.weightUOM>(row, row.WeightUOM, new PXSetPropertyException(Messages.ValueIsRequiredForAutoPackage, PXErrorLevel.Warning));

				if (row.BaseItemWeight <= 0)
					Item.Cache.RaiseExceptionHandling<InventoryItem.baseItemWeight>(row, row.BaseItemWeight, new PXSetPropertyException(Messages.ValueIsRequiredForAutoPackage, PXErrorLevel.Warning));

				if (row.PackageOption == INPackageOption.WeightAndVolume)
				{
					if (string.IsNullOrEmpty(row.VolumeUOM))
						Item.Cache.RaiseExceptionHandling<InventoryItem.volumeUOM>(row, row.VolumeUOM, new PXSetPropertyException(Messages.ValueIsRequiredForAutoPackage, PXErrorLevel.Warning));

					if (row.BaseItemVolume <= 0)
						Item.Cache.RaiseExceptionHandling<InventoryItem.baseItemVolume>(row, row.BaseItemVolume, new PXSetPropertyException(Messages.ValueIsRequiredForAutoPackage, PXErrorLevel.Warning));
				}
			}

			//validate boxes:
			foreach (INItemBoxEx box in Boxes.Select())
			{
				PXUIFieldAttribute.SetError<INItemBoxEx.boxID>(Boxes.Cache, box, null);
				PXUIFieldAttribute.SetError<INItemBoxEx.maxQty>(Boxes.Cache, box, null);

				if (row.PackageOption.IsIn(INPackageOption.Weight, INPackageOption.WeightAndVolume) && box.MaxWeight.GetValueOrDefault() == 0)
					Boxes.Cache.RaiseExceptionHandling<INItemBoxEx.boxID>(box, box.BoxID, new PXSetPropertyException(Messages.MaxWeightIsNotDefined, PXErrorLevel.Warning));

				if (row.PackageOption == INPackageOption.WeightAndVolume && box.MaxVolume.GetValueOrDefault() == 0)
					Boxes.Cache.RaiseExceptionHandling<INItemBoxEx.boxID>(box, box.BoxID, new PXSetPropertyException(Messages.MaxVolumeIsNotDefined, PXErrorLevel.Warning));

				if (row.PackageOption.IsIn(INPackageOption.Weight, INPackageOption.WeightAndVolume) &&
					(box.MaxWeight.GetValueOrDefault() < row.BaseItemWeight.GetValueOrDefault() || box.MaxVolume > 0 && row.BaseItemVolume > box.MaxVolume))
				{
					Boxes.Cache.RaiseExceptionHandling<INItemBoxEx.boxID>(box, box.BoxID, new PXSetPropertyException(Messages.ItemDontFitInTheBox, PXErrorLevel.Warning));
				}
			}
		}

		private IEnumerable<RelationGroup> GetGroups()
		{
			if (IsImport)
				Groups.View.Clear();

			foreach (RelationGroup group in SelectFrom<RelationGroup>.View.Select(this))
			{
				if (group.SpecificModule.IsIn(null, typeof(InventoryItem).Namespace) &&
					group.SpecificType.IsIn(null, typeof(SegmentValue).FullName, typeof(InventoryItem).FullName) ||
					Item.Current != null && UserAccess.IsIncluded(Item.Current.GroupMask, group))
				{
					Groups.Current = group;
					yield return group;
				}
			}
		}

		#region Actions
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Update Cost", MapEnableRights = PXCacheRights.Update)]
		protected override IEnumerable UpdateCost(PXAdapter adapter)
		{
			if (ItemSettings.Current != null && ItemSettings.Current.PendingStdCostDate != null && ItemSettings.Current.ValMethod == INValMethod.Standard)
			{
				INCostStatus layer =
					SelectFrom<INCostStatus>.
					Where<
						INCostStatus.FK.InventoryItem.SameAsCurrent.
						And<INCostStatus.qtyOnHand.IsNotEqual<decimal0>>>.
					View.SelectWindowed(this, 0, 1);

				if (layer == null)
				{
					InventoryItem item = ItemSettings.Current;
					decimal newCost = item.PendingStdCost ?? 0m;
					DateTime newCostDate = item.PendingStdCostDate ?? (DateTime)Accessinfo.BusinessDate;

					item.LastStdCost = item.StdCost;
					item.StdCost = newCost;
					item.StdCostDate = newCostDate;
					item.PendingStdCost = 0m;
					item.PendingStdCostDate = null;

					// The intention is not to raise InventoryItem_RowUpdated event.
					// Otherwise INItemSite.pendingStdCost will be assigned the InventoryItem.stdCost value
					// and INItemSite.pendingStdCostReset flag will be set.
					ItemSettings.Cache.MarkUpdated(item);

					foreach (INItemSite itemSite in itemsiterecords.Select())
					{
						if (itemSite.StdCostOverride == true)
							continue;

						itemSite.LastStdCost = itemSite.StdCost;
						itemSite.StdCost = newCost;
						itemSite.StdCostDate = newCostDate;
						itemSite.PendingStdCost = 0m;
						itemSite.PendingStdCostDate = null;
						itemSite.PendingStdCostReset = false;

						itemsiterecords.Cache.MarkUpdated(itemSite);
					}

					Save.Press();
				}
				else
				{
					throw new PXException(Messages.QtyOnHandExists);
				}
			}
			return adapter.Get();
		}

		public PXAction<InventoryItem> viewSummary;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Summary", MapEnableRights = PXCacheRights.Select)]
		protected virtual IEnumerable ViewSummary(PXAdapter adapter)
		{
			if (Item.Current != null)
			{
				InventorySummaryEnq graph = PXGraph.CreateInstance<InventorySummaryEnq>();
				graph.Filter.Current.InventoryID = Item.Current.InventoryID;
				graph.Filter.Select();
				throw new PXRedirectRequiredException(graph, "Inventory Summary")
				{
					Mode = PXBaseRedirectException.WindowMode.New
				};
			}
			return adapter.Get();
		}

		public PXAction<InventoryItem> viewAllocationDetails;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Allocation Details", MapEnableRights = PXCacheRights.Select)]
		protected virtual IEnumerable ViewAllocationDetails(PXAdapter adapter)
		{
			if (Item.Current != null)
			{
				InventoryAllocDetEnq graph = PXGraph.CreateInstance<InventoryAllocDetEnq>();
				graph.Filter.Current.InventoryID = Item.Current.InventoryID;
				graph.Filter.Select();
				throw new PXRedirectRequiredException(graph, "Inventory Allocation Details")
				{
					Mode = PXBaseRedirectException.WindowMode.New
				};
			}
			return adapter.Get();
		}

		public PXAction<InventoryItem> viewTransactionSummary;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Transaction Summary", MapEnableRights = PXCacheRights.Select)]
		protected virtual IEnumerable ViewTransactionSummary(PXAdapter adapter)
		{
			if (Item.Current != null)
			{
				InventoryTranSumEnq graph = PXGraph.CreateInstance<InventoryTranSumEnq>();
				graph.Filter.Current.InventoryID = Item.Current.InventoryID;
				graph.Filter.Select();
				throw new PXRedirectRequiredException(graph, "Inventory Transaction Summary")
				{
					Mode = PXBaseRedirectException.WindowMode.New
				};
			}
			return adapter.Get();
		}

		public PXAction<InventoryItem> viewTransactionDetails;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Transaction Details", MapEnableRights = PXCacheRights.Select)]
		protected virtual IEnumerable ViewTransactionDetails(PXAdapter adapter)
		{
			if (Item.Current != null)
			{
				InventoryTranDetEnq graph = PXGraph.CreateInstance<InventoryTranDetEnq>();
				graph.Filter.Current.InventoryID = Item.Current.InventoryID;
				graph.Filter.Select();
				throw new PXRedirectRequiredException(graph, "Inventory Transaction Details")
				{
					Mode = PXBaseRedirectException.WindowMode.New
				};
			}
			return adapter.Get();
		}

		public PXAction<InventoryItem> viewTransactionHistory;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Transaction History", MapEnableRights = PXCacheRights.Select)]
		protected virtual IEnumerable ViewTransactionHistory(PXAdapter adapter)
		{
			if (Item.Current != null)
			{
				InventoryTranHistEnq graph = PXGraph.CreateInstance<InventoryTranHistEnq>();
				graph.Filter.Current.InventoryID = Item.Current.InventoryID;
				graph.Filter.Select();
				throw new PXRedirectRequiredException(graph, "Inventory Transaction History")
				{
					Mode = PXBaseRedirectException.WindowMode.New
				};
			}
			return adapter.Get();
		}

		public PXAction<InventoryItem> addWarehouseDetail;
		[PXUIField(DisplayName = "Add Warehouse Detail", MapEnableRights = PXCacheRights.Select)]
		[PXInsertButton]
		protected virtual IEnumerable AddWarehouseDetail(PXAdapter adapter)
		{
			foreach (InventoryItem item in adapter.Get())
			{
				if (item.InventoryID > 0)
				{
					INItemSiteMaint maint = PXGraph.CreateInstance<INItemSiteMaint>();
					PXCache cache = maint.itemsiterecord.Cache;
					IN.INItemSite rec = (IN.INItemSite)cache.CreateCopy(cache.Insert());
					rec.InventoryID = item.InventoryID;
					cache.Update(rec);
					cache.IsDirty = false;
					throw new PXRedirectRequiredException(maint, "Add Warehouse Detail");
				}
				yield return item;
			}
		}
		public PXAction<InventoryItem> updateReplenishment;

		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.DataEntry)]
		[PXUIField(DisplayName = "Reset to Default", MapEnableRights = PXCacheRights.Update)]
		protected virtual IEnumerable UpdateReplenishment(PXAdapter adapter)
		{
			if (this.replenishment.Current != null && insetup.Current.UseInventorySubItem == true)
				foreach (INSubItemRep rep in this.subreplenishment.Select())
				{
					INSubItemRep upd = PXCache<INSubItemRep>.CreateCopy(rep);
					upd.SafetyStock = this.replenishment.Current.SafetyStock;
					upd.MinQty = this.replenishment.Current.MinQty;
					upd.MaxQty = this.replenishment.Current.MaxQty;
					this.subreplenishment.Update(upd);
				}
			return adapter.Get();
		}

		public PXAction<InventoryItem> generateSubitems;
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.AddNew)]
		[PXUIField(DisplayName = "Generate Subitems", MapEnableRights = PXCacheRights.Update)]
		protected virtual IEnumerable GenerateSubitems(PXAdapter adapter)
		{
			if (replenishment.Current != null && insetup.Current.UseInventorySubItem == true)
			{
				var idSegments =
					SelectFrom<Segment>.
					Where<Segment.dimensionID.IsEqual<@P.AsString>>.
					View.Select(this, SubItemAttribute.DimensionName)
					.Select(res => res.GetItem<Segment>())
					.ToList();

				var valuesBySegmentId = idSegments.ToDictionary(
					segment => segment.SegmentID,
					segement => new List<string>());

				// Get active segment values (SUBITEMS tab)
				foreach (INSubItemSegmentValue activeSegment in
					SelectFrom<INSubItemSegmentValue>.
					InnerJoin<SegmentValue>.On<
						SegmentValue.segmentID.IsEqual<INSubItemSegmentValue.segmentID>.
						And<SegmentValue.value.IsEqual<INSubItemSegmentValue.value>>.
						And<SegmentValue.dimensionID.IsEqual<SubItemAttribute.dimensionName>>>.
					Where<INSubItemSegmentValue.FK.InventoryItem.SameAsCurrent>.
					View.Select(this))
				{
					valuesBySegmentId[activeSegment.SegmentID].Add(activeSegment.Value);
				}

				// Segments that requires validation can't be empty. If validation is not required adding the placeholder.
				foreach (var segment in idSegments)
				{
					if (valuesBySegmentId[segment.SegmentID].Any())
						continue;

					if (segment.Validate != true)
						valuesBySegmentId[segment.SegmentID].Add(new string(' ', segment.Length ?? 1));
					else
						throw new PXException(Messages.InactiveSegmentValues);
				}

				List<string> subItemIds = valuesBySegmentId.First().Value;
				foreach (var segmentValues in valuesBySegmentId.Skip(1).Select(kvp => kvp.Value))
				{
					// Cross Join
					subItemIds = subItemIds.Join(segmentValues, s => 0, s => 0, (subItemId, segment) => subItemId + segment).ToList();
				}

				foreach (var subItemId in subItemIds)
				{
					if (subItemId.All(char.IsWhiteSpace))
						continue;

					var subItem = new INSubItemRep();
					subItem.InventoryID = Item.Current.InventoryID;
					subItem.ReplenishmentClassID = replenishment.Current.ReplenishmentClassID;
					subreplenishment.SetValueExt<INSubItemRep.subItemID>(subItem, subItemId);
					subreplenishment.Insert(subItem);
				}
			}
			return adapter.Get();
		}

		public PXAction<InventoryItem> generateLotSerialNumber;
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.AddNew)]
		[PXUIField(DisplayName = "Generate Lot/Serial Number", Visible = false)]
		protected virtual IEnumerable GenerateLotSerialNumber(PXAdapter adapter)
		{
			foreach (InventoryItem item in adapter.Get())
			{
				item.LotSerNumberResult = INLotSerialNbrAttribute.GetNextNumber(adapter.View.Cache, item.InventoryID.GetValueOrDefault());
				yield return item;
			}
		}

		public PXAction<InventoryItem> viewGroupDetails;
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
		#endregion

		public static void Redirect(int? inventoryID) => Redirect(inventoryID, false);
		public static void Redirect(int? inventoryID, bool newWindow)
		{
			InventoryItemMaint graph = PXGraph.CreateInstance<InventoryItemMaint>();
			graph.Item.Current = graph.Item.Search<InventoryItem.inventoryID>(inventoryID);
			if (graph.Item.Current != null)
			{
				if (newWindow)
					throw new PXRedirectRequiredException(graph, true, Messages.InventoryItem) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
				else
					throw new PXRedirectRequiredException(graph, Messages.InventoryItem);
			}
		}

		protected virtual decimal? CalculateMaxQtyInBox(InventoryItem item, INItemBoxEx box)
		{
			decimal? resultWeight = null;
			decimal? resultVolume = null;

			if (item.BaseWeight > 0 && box.MaxWeight > 0)
				resultWeight = Math.Floor((box.MaxWeight.Value - box.BoxWeight.GetValueOrDefault()) / item.BaseWeight.Value);

			if (item.PackageOption == INPackageOption.Weight)
				return resultWeight;

			if (item.BaseVolume > 0 && box.MaxVolume > 0)
				resultVolume = Math.Floor(box.MaxVolume.Value / item.BaseVolume.Value);

			if (resultWeight != null && resultVolume != null)
				return Math.Min(resultWeight.Value, resultVolume.Value);

			if (resultWeight != null)
				return resultWeight;

			if (resultVolume != null)
				return resultVolume;

			return null;
		}
	}
}