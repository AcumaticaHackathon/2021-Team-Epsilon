using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

using PX.Objects.Common.GraphExtensions;
using CRLocation = PX.Objects.CR.Standalone.Location;
using ItemStats = PX.Objects.IN.Overrides.INDocumentRelease.ItemStats;
using SiteStatus = PX.Objects.IN.Overrides.INDocumentRelease.SiteStatus;

namespace PX.Objects.IN
{
	public class NonStockItemMaint : InventoryItemMaintBase
	{
		public class CostPriceSettings : CurySettingsExtension<NonStockItemMaint, InventoryItem, InventoryItemCurySettings>
		{
			[PXButton(CommitChanges = true), PXUIField(DisplayName = "Update Cost", MapEnableRights = PXCacheRights.Update)]
			public virtual IEnumerable UpdateCost(PXAdapter adapter)
			{
				InventoryItemCurySettings row = (InventoryItemCurySettings)curySettings.SelectSingle();
				if (row?.PendingStdCostDate != null)
				{
					row = (InventoryItemCurySettings) curySettings.Cache.CreateCopy(row);
					row.LastStdCost = row.StdCost;
					row.StdCostDate = row.PendingStdCostDate.GetValueOrDefault(Base.Accessinfo.BusinessDate.Value);
					row.StdCost = row.PendingStdCost;
					row.PendingStdCost = 0;
					row.PendingStdCostDate = null;
					curySettings.Cache.Update(row);
					Base.Save.Press();
				}
				return adapter.Get();
			}
		}
		public override bool IsStockItemFlag => false;

		#region Initialization
		public NonStockItemMaint()
		{
			Item.View = new PXView(this, false, new
				SelectFrom<InventoryItem>.
				Where<
					InventoryItem.stkItem.IsEqual<False>.
					And<InventoryItem.itemStatus.IsNotEqual<InventoryItemStatus.unknown>>.
					And<InventoryItem.isTemplate.IsEqual<False>>.
					And<MatchUser>>());

			Views[nameof(Item)] = Item.View;

			FieldDefaulting.AddHandler<BAccountR.type>((sender, e) =>
			{
				if (e.Row != null)
					e.NewValue = BAccountType.VendorType;
			});
		}

		public override void Configure(PXScreenConfiguration config)
		{
			var context = config.GetScreenConfigurationContext<NonStockItemMaint, InventoryItem>();
			context.AddScreenConfigurationFor(screen =>
			{
				return screen
					.WithActions(actions =>
					{
						actions.Add(g => g.ChangeID, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.updateCost, a => a.InFolder(FolderType.ActionsFolder));
						actions.Add(g => g.viewRestrictionGroups, a => a.InFolder(FolderType.ActionsFolder));

						actions.Add(g => g.viewSalesPrices, a => a.InFolder(FolderType.InquiriesFolder));
						actions.Add(g => g.viewVendorPrices, a => a.InFolder(FolderType.InquiriesFolder));
					});
			});
		}
		#endregion

		#region DAC overrides
		#region InventoryItem
		#region InventoryCD
		[PXDefault]
		[InventoryRaw(typeof(Where<InventoryItem.stkItem.IsEqual<False>>), IsKey = true, DisplayName = "Inventory ID", Filterable = true)]
		protected virtual void _(Events.CacheAttached<InventoryItem.inventoryCD> e) { }
		#endregion
		#region PostClassID
		[PXDBString(10, IsUnicode = true)]
		[PXSelector(typeof(INPostClass.postClassID), DescriptionField = typeof(INPostClass.descr))]
		[PXUIField(DisplayName = "Posting Class", Required = true)]
		[PXDefault(typeof(SelectFrom<INItemClass>.Where<INItemClass.itemClassID.IsEqual<InventoryItem.itemClassID.FromCurrent>>), SourceField = typeof(INItemClass.postClassID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<InventoryItem.postClassID> e) { }
		#endregion
		#region LotSerClassID
		[PXDBString(10, IsUnicode = true)]
		protected virtual void _(Events.CacheAttached<InventoryItem.lotSerClassID> e) { }
		#endregion
		#region ItemType
		[PXDBString(1, IsFixed = true)]
		[PXDefault(INItemTypes.NonStockItem, typeof(SelectFrom<INItemClass>.Where<INItemClass.itemClassID.IsEqual<InventoryItem.itemClassID.FromCurrent>>), SourceField = typeof(INItemClass.itemType), CacheGlobal = true)]
		[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible)]
		[INItemTypes.NonStockList]
		protected virtual void _(Events.CacheAttached<InventoryItem.itemType> e) { }
		#endregion
		#region ValMethod
		[PXDBString(1, IsFixed = true)]
		[PXDefault(INValMethod.Standard)]
		protected virtual void _(Events.CacheAttached<InventoryItem.valMethod> e) { }
		#endregion
		#region InvtAcctID
		[Account(DisplayName = "Expense Accrual Account", DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXDefault(typeof(SelectFrom<INPostClass>.Where<INPostClass.postClassID.IsEqual<InventoryItem.postClassID.FromCurrent>>), SourceField = typeof(INPostClass.invtAcctID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<InventoryItem.invtAcctID> e) { }
		#endregion
		#region InvtSubID
		[SubAccount(typeof(InventoryItem.invtAcctID), DisplayName = "Expense Accrual Sub.", DescriptionField = typeof(Sub.description))]
		[PXDefault(typeof(SelectFrom<INPostClass>.Where<INPostClass.postClassID.IsEqual<InventoryItem.postClassID.FromCurrent>>), SourceField = typeof(INPostClass.invtSubID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<InventoryItem.invtSubID> e) { }
		#endregion
		#region COGSAcctID
		[PXDefault(typeof(SelectFrom<INPostClass>.Where<INPostClass.postClassID.IsEqual<InventoryItem.postClassID.FromCurrent>>), SourceField = typeof(INPostClass.cOGSAcctID), CacheGlobal = true)]
		[Account(DisplayName = "Expense Account", DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		protected virtual void _(Events.CacheAttached<InventoryItem.cOGSAcctID> e) { }
		#endregion
		#region COGSSubID
		[PXDefault(typeof(SelectFrom<INPostClass>.Where<INPostClass.postClassID.IsEqual<InventoryItem.postClassID.FromCurrent>>), SourceField = typeof(INPostClass.cOGSSubID), CacheGlobal = true)]
		[SubAccount(typeof(InventoryItem.cOGSAcctID), DisplayName = "Expense Sub.", DescriptionField = typeof(Sub.description))]
		protected virtual void _(Events.CacheAttached<InventoryItem.cOGSSubID> e) { }
		#endregion
		#region StkItem
		[PXDBBool]
		[PXDefault(false)]
		protected virtual void _(Events.CacheAttached<InventoryItem.stkItem> e) { }
		#endregion
		#region ItemClassID
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXRestrictor(typeof(Where<INItemClass.stkItem.IsNotEqual<True>>), Messages.ItemClassIsStock)]
		protected virtual void _(Events.CacheAttached<InventoryItem.itemClassID> e) { }
		#endregion
		#region KitItem
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Is a Kit")]
		protected virtual void _(Events.CacheAttached<InventoryItem.kitItem> e) { }
		#endregion
		#region DefaultSubItemOnEntry
		[PXDBBool]
		[PXDefault(false)]
		protected virtual void _(Events.CacheAttached<InventoryItem.defaultSubItemOnEntry> e) { }
		#endregion
		#region DeferredCode
		[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa", BqlField = typeof(InventoryItem.deferredCode))]
		[PXUIField(DisplayName = "Deferral Code")]
		[PXSelector(typeof(SearchFor<DRDeferredCode.deferredCodeID>))]
		[PXRestrictor(typeof(Where<DRDeferredCode.active.IsEqual<True>>), DR.Messages.InactiveDeferralCode, typeof(DRDeferredCode.deferredCodeID))]
		[PXDefault(typeof(SelectFrom<INItemClass>.Where<INItemClass.itemClassID.IsEqual<InventoryItem.itemClassID.FromCurrent>>), SourceField = typeof(INItemClass.deferredCode), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<InventoryItem.deferredCode> e) { }
		#endregion
		#region IsSplitted
		[PXDBBool(BqlField = typeof(InventoryItem.isSplitted))]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Split into Components")]
		protected virtual void _(Events.CacheAttached<InventoryItem.isSplitted> e) { }
		#endregion
		#region UseParentSubID
		[PXDBBool(BqlField = typeof(InventoryItem.useParentSubID))]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Use Component Subaccounts")]
		protected virtual void _(Events.CacheAttached<InventoryItem.useParentSubID> e) { }
		#endregion
		#region TotalPercentage
		[PXDecimal]
		[PXUIField(DisplayName = "Total Percentage", Enabled = false)]
		protected virtual void _(Events.CacheAttached<InventoryItem.totalPercentage> e) { }
		#endregion
		#region NonStockReceipt
		[PXDBBool(BqlField = typeof(InventoryItem.nonStockReceipt))]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Require Receipt")]
		protected virtual void _(Events.CacheAttached<InventoryItem.nonStockReceipt> e) { }
		#endregion
		#region NonStockShip
		[PXDBBool(BqlField = typeof(InventoryItem.nonStockShip))]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Require Shipment")]
		protected virtual void _(Events.CacheAttached<InventoryItem.nonStockShip> e) { }
		#endregion
		#endregion
		#region INItemClass
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(false)]
		protected virtual void _(Events.CacheAttached<INItemClass.stkItem> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(INItemTypes.NonStockItem,
			typeof(SelectFrom<INItemClass>.Where<INItemClass.itemClassID.IsEqual<INItemClass.parentItemClassID.FromCurrent>.And<INItemClass.stkItem.IsEqual<False>>>),
			SourceField = typeof(INItemClass.itemType), PersistingCheck = PXPersistingCheck.Nothing, CacheGlobal = true)]
		protected virtual void _(Events.CacheAttached<INItemClass.itemType> e) { }
		#endregion
		#region INComponent
		[PXDBDefault(typeof(InventoryItem.inventoryID))]
		[PXParent(typeof(INComponent.FK.InventoryItem))]
		[PXDBInt(IsKey = true)]
		protected virtual void _(Events.CacheAttached<INComponent.inventoryID> e) { }

		[PXDefault]
		[Inventory(typeof(SearchFor<InventoryItem.inventoryID>.Where<MatchUser>), typeof(InventoryItem.inventoryCD), typeof(InventoryItem.descr), Filterable = true, IsKey = true, DisplayName = "Inventory ID")]
		protected virtual void _(Events.CacheAttached<INComponent.componentID> e) { }
		#endregion
		#region POVendorInventory
		[PXDBInt]
		[PXParent(typeof(SelectFrom<InventoryItem>.Where<InventoryItem.inventoryID.IsEqual<POVendorInventory.inventoryID.FromCurrent>>))]
		[PXDBDefault(typeof(InventoryItem.inventoryID))]
		protected virtual void _(Events.CacheAttached<POVendorInventory.inventoryID> e) { }

		[SubItem(typeof(POVendorInventory.inventoryID), DisplayName = "Subitem")]
		protected virtual void _(Events.CacheAttached<POVendorInventory.subItemID> e) { }
		#endregion
		#region INItemXRef
		[PXCustomizeBaseAttribute(typeof(SubItemAttribute), nameof(SubItemAttribute.Disabled), true)]
		protected virtual void _(Events.CacheAttached<INItemXRef.subItemID> e) { }
		#endregion
		#region INItemCategory
		[NonStockItem(IsKey = true, DirtyRead = true)]
		[PXParent(typeof(INItemCategory.FK.InventoryItem))]
		[PXDBDefault(typeof(InventoryItem.inventoryID))]
		protected virtual void _(Events.CacheAttached<INItemCategory.inventoryID> e) { }
		#endregion
		#endregion

		#region Selects
		public
			SelectFrom<Branch>.
			Where<Branch.branchID.IsEqual<@P.AsInt>>.
			View CurrentBranch;
		#endregion

		#region Event Handlers
		#region InventoryItem
		protected override void _(Events.RowSelected<InventoryItem> e)
		{
			base._(e);

			if (e.Row == null)
				return;

			PXUIFieldAttribute.SetRequired<InventoryItem.postClassID>(e.Cache, e.Row.NonStockReceipt == true);
			PXUIFieldAttribute.SetVisible<InventoryItem.taxCalcMode>(e.Cache, e.Row, e.Row.ItemType == INItemTypes.ExpenseItem);

			e.Cache.AdjustUI(e.Row)
				.For<InventoryItem.completePOLine>(fa => fa.Enabled = (e.Row.TemplateItemID == null))
				.SameFor<InventoryItem.nonStockReceipt>()
				.SameFor<InventoryItem.nonStockShip>();
		}

		protected override void _(Events.FieldUpdated<InventoryItem, InventoryItem.itemClassID> e)
		{
			base._(e);

			if (doResetDefaultsOnItemClassChange)
			{
				e.Cache.SetDefaultExt<InventoryItem.accrueCost>(e.Row);
			}
		}

		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.postClassID> e)
		{
			e.Cache.SetDefaultExt<InventoryItem.invtAcctID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.invtSubID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.salesAcctID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.salesSubID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.cOGSAcctID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.cOGSSubID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.pOAccrualAcctID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.pOAccrualSubID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.pPVAcctID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.pPVSubID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.deferralAcctID>(e.Row);
			e.Cache.SetDefaultExt<InventoryItem.deferralSubID>(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.kitItem> e)
		{
			if (e.Row != null && e.Row.KitItem == true)
				e.Cache.SetValueExt<InventoryItem.accrueCost>(e.Row, false);
		}

		protected virtual void _(Events.RowInserted<InventoryItem> e)
		{
			e.Row.TotalPercentage = 100;
		}

		protected virtual void _(Events.RowPersisting<InventoryItem> e)
		{
			base._(e);

			if (e.Row.IsSplitted == true)
			{
				if (string.IsNullOrEmpty(e.Row.DeferredCode))
					if (e.Cache.RaiseExceptionHandling<InventoryItem.deferredCode>(e.Row, e.Row.DeferredCode, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, $"[{nameof(InventoryItem.deferredCode)}]")))
						throw new PXRowPersistingException(typeof(InventoryItem.deferredCode).Name, e.Row.DeferredCode, ErrorMessages.FieldIsEmpty, typeof(InventoryItem.deferredCode).Name);

				var components = Components.Select().RowCast<INComponent>().ToList();

				VerifyComponentPercentages(e.Cache, e.Row, components);
				VerifyOnlyOneResidualComponent(e.Cache, e.Row, components);
				CheckSameTermOnAllComponents(Components.Cache, components);
			}

			if (!PXAccess.FeatureInstalled<FeaturesSet.distributionModule>() || !PXAccess.FeatureInstalled<FeaturesSet.inventory>())
			{
				e.Row.NonStockReceipt = false;
				e.Row.NonStockShip = false;
			}

			if (!PXAccess.FeatureInstalled<FeaturesSet.pOReceiptsWithoutInventory>())
				e.Row.NonStockReceiptAsService = e.Row.NonStockReceipt;

			if (e.Row.NonStockReceipt == true && string.IsNullOrEmpty(e.Row.PostClassID))
				throw new PXRowPersistingException(typeof(InventoryItem.postClassID).Name, e.Row.PostClassID, ErrorMessages.FieldIsEmpty, typeof(InventoryItem.postClassID).Name);

			if (e.Operation.Command() == PXDBOperation.Delete)
			{
				using (PXTransactionScope ts = new PXTransactionScope())
				{
					// Acuminator disable once PX1043 SavingChangesInEventHandlers [Justification]
					PXDatabase.Delete<CSAnswers>(new PXDataFieldRestrict(nameof(CSAnswers.RefNoteID), PXDbType.UniqueIdentifier, e.Row.NoteID));
					ts.Complete(this);
				}
			}
		}

		protected virtual void _(Events.RowPersisted<InventoryItem> e)
		{
			Common.Discount.DiscountEngine.RemoveFromCachedInventoryPriceClasses(e.Row.InventoryID);
		}
		#endregion

		#region INItemXRef
		protected virtual void _(Events.FieldVerifying<INItemXRef, INItemXRef.subItemID> e) => e.Cancel = true;
		#endregion
		#endregion
	}
}