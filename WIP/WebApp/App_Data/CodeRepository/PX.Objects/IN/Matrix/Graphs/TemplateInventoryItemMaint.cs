using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.Common.Attributes;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.PO;
using PX.Objects.IN.Matrix.Attributes;
using PX.Objects.IN.Matrix.Utility;
using PX.Objects.IN.Matrix.Interfaces;
using PX.Objects.IN.Matrix.GraphExtensions;
using PX.Objects.IN.Matrix.DAC;
using PX.Objects.IN.Matrix.DAC.Projections;
using PX.Objects.IN.Matrix.DAC.Unbound;

namespace PX.Objects.IN.Matrix.Graphs
{
	public class TemplateInventoryItemMaint : InventoryItemMaintBase, ICreateMatrixHelperFactory
	{
		public override bool IsStockItemFlag => ItemSettings.Current?.StkItem == true;

		#region Views
		public
			SelectFrom<INItemBoxEx>.
			Where<INItemBoxEx.FK.InventoryItem.SameAsCurrent>.
			View Boxes;

		public
			PXSetup<INPostClass>.
			Where<INPostClass.postClassID.IsEqual<InventoryItem.postClassID.FromCurrent>>
			postclass;

		public
			SelectFrom<ExcludedField>.
			Where<ExcludedField.templateID.IsEqual<InventoryItem.inventoryID.FromCurrent>>.
			OrderBy<INMatrixExcludedData.createdDateTime.Asc>.
			View FieldsExcludedFromUpdate;

		public
			SelectFrom<ExcludedAttribute>.
			LeftJoin<CSAnswers>.On<CSAnswers.attributeID.IsEqual<ExcludedAttribute.fieldName>
				.And<CSAnswers.refNoteID.IsEqual<InventoryItem.noteID.FromCurrent>>>.
			Where<
				ExcludedAttribute.templateID.IsEqual<InventoryItem.inventoryID.FromCurrent>>.
			OrderBy<CSAnswers.order.Asc, CSAnswers.attributeID.Asc>.
			View AttributesExcludedFromUpdate;
		public IEnumerable attributesExcludedFromUpdate() => GetAttributesExcludedFromUpdate();

		public
			SelectFrom<CSAttribute>.
			Where<CSAttribute.attributeID.IsEqual<MatrixAttributeSelectorAttribute.dummyAttributeName>>.
			View DummyAttribute;

		public
			SelectFrom<CSAttributeDetail>.
			Where<CSAttributeDetail.attributeID.IsEqual<MatrixAttributeSelectorAttribute.dummyAttributeName>>.
			View DummyAttributeValue;
		#endregion Views

		#region Constructor
		public TemplateInventoryItemMaint()
		{
			Item.View = new PXView(this, false, new
				SelectFrom<InventoryItem>.
				Where<
					InventoryItem.isTemplate.IsEqual<True>.
					And<MatchUser>>());

			Views[nameof(Item)] = Item.View;

			updateCost.SetVisible(false);
			viewSalesPrices.SetVisible(false);
			viewVendorPrices.SetVisible(false);
			viewRestrictionGroups.SetVisible(false);
		}
		#endregion Constructor

		#region DAC overrides
		#region INItemClass
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(INItemTypes.FinishedGood,
			typeof(SearchFor<INItemClass.itemType>.Where<INItemClass.itemClassID.IsEqual<INItemClass.parentItemClassID.FromCurrent>.And<INItemClass.stkItem.IsEqual<INItemClass.stkItem.FromCurrent>>>),
			PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<INItemClass.itemType> eventArgs) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(typeof(Search<InventoryItem.stkItem>))]
		protected virtual void _(Events.CacheAttached<INItemClass.stkItem> eventArgs) { }
		#endregion
		#region INItemCategory
		[TemplateInventory(IsKey = true, DirtyRead = true)]
		[PXParent(typeof(INItemCategory.FK.InventoryItem))]
		[PXDBDefault(typeof(InventoryItem.inventoryID))]
		protected virtual void _(Events.CacheAttached<INItemCategory.inventoryID> eventArgs) { }
		#endregion
		#region InventoryItem
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(typeof(IIf<Where<FeatureInstalled<FeaturesSet.inventory>>, True, False>))]
		protected virtual void _(Events.CacheAttached<InventoryItem.stkItem> eventArgs) { }

		[PXDefault]
		[TemplateInventoryRaw(IsKey = true, Filterable = true)]
		protected virtual void _(Events.CacheAttached<InventoryItem.inventoryCD> eventArgs) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXDefaultAttribute))]
		[PXDefault(true)]
		protected virtual void _(Events.CacheAttached<InventoryItem.isTemplate> eventArgs) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXRestrictor(typeof(Where<INItemClass.stkItem.IsEqual<InventoryItem.stkItem.FromCurrent>>), Messages.StkItemSettingMustCoincide)]
		protected virtual void _(Events.CacheAttached<InventoryItem.itemClassID> eventArgs) { }

		[DefaultConditional(typeof(SelectFrom<INItemClass>.Where<INItemClass.itemClassID.IsEqual<InventoryItem.itemClassID.FromCurrent>>),
			typeof(InventoryItem.stkItem), true, SourceField = typeof(INItemClass.lotSerClassID), CacheGlobal = true)]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		protected virtual void _(Events.CacheAttached<InventoryItem.lotSerClassID> eventArgs) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXSelector(typeof(INPostClass.postClassID), DescriptionField = typeof(INPostClass.descr))]
		[DefaultConditional(typeof(SelectFrom<INItemClass>.Where<INItemClass.itemClassID.IsEqual<InventoryItem.itemClassID.FromCurrent>>),
			typeof(InventoryItem.stkItem), true, SourceField = typeof(INItemClass.postClassID), CacheGlobal = true)]
		protected virtual void _(Events.CacheAttached<InventoryItem.postClassID> eventArgs) { }

		[TemplateInventoryAccount(true, typeof(InventoryItem.invtAcctID), DisplayName = "Inventory Account",
			Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), ControlAccountForModule = ControlAccountModule.IN)]
		[PXDefault(typeof(SelectFrom<INPostClass>.Where<INPostClass.postClassID.IsEqual<InventoryItem.postClassID.FromCurrent>>), SourceField = typeof(INPostClass.invtAcctID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual void _(Events.CacheAttached<InventoryItem.invtAcctID> e) { }

		[TemplateInventorySubAccount(true, typeof(InventoryItem.invtSubID), typeof(InventoryItem.invtAcctID), DisplayName = "Inventory Sub.",
			Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXDefault(typeof(SelectFrom<INPostClass>.Where<INPostClass.postClassID.IsEqual<InventoryItem.postClassID.FromCurrent>>), SourceField = typeof(INPostClass.invtSubID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<InventoryItem.invtSubID> e) { }

		[TemplateInventoryAccount(true, typeof(InventoryItem.cOGSAcctID), DisplayName = "COGS Account",
			Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXDefault(typeof(SelectFrom<INPostClass>.Where<INPostClass.postClassID.IsEqual<InventoryItem.postClassID.FromCurrent>>), SourceField = typeof(INPostClass.cOGSAcctID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<InventoryItem.cOGSAcctID> e) { }

		[TemplateInventorySubAccount(true, typeof(InventoryItem.cOGSSubID), typeof(InventoryItem.cOGSAcctID),
			DisplayName = "COGS Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXDefault(typeof(SelectFrom<INPostClass>.Where<INPostClass.postClassID.IsEqual<InventoryItem.postClassID.FromCurrent>>), SourceField = typeof(INPostClass.cOGSSubID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<InventoryItem.cOGSSubID> e) { }

		[TemplateInventoryAccount(false, typeof(InventoryItem.invtAcctID), DisplayName = "Expense Accrual Account",
			DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXDefault(typeof(SelectFrom<INPostClass>.Where<INPostClass.postClassID.IsEqual<InventoryItem.postClassID.FromCurrent>>), SourceField = typeof(INPostClass.invtAcctID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual void _(Events.CacheAttached<InventoryItem.expenseAccrualAcctID> e) { }

		[TemplateInventorySubAccount(false, typeof(InventoryItem.invtSubID), typeof(InventoryItem.invtAcctID), DisplayName = "Expense Accrual Sub.", DescriptionField = typeof(Sub.description))]
		[PXDefault(typeof(SelectFrom<INPostClass>.Where<INPostClass.postClassID.IsEqual<InventoryItem.postClassID.FromCurrent>>), SourceField = typeof(INPostClass.invtSubID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<InventoryItem.expenseAccrualSubID> e) { }

		[DefaultConditional(typeof(SelectFrom<INPostClass>.Where<INPostClass.postClassID.IsEqual<InventoryItem.postClassID.FromCurrent>>),
			typeof(InventoryItem.stkItem), false, SourceField = typeof(INPostClass.cOGSAcctID), CacheGlobal = true)]
		[TemplateInventoryAccount(false, typeof(InventoryItem.cOGSAcctID), DisplayName = "Expense Account",
			DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		protected virtual void _(Events.CacheAttached<InventoryItem.expenseAcctID> e) { }

		[DefaultConditional(typeof(SelectFrom<INPostClass>.Where<INPostClass.postClassID.IsEqual<InventoryItem.postClassID.FromCurrent>>),
			typeof(InventoryItem.stkItem), false, SourceField = typeof(INPostClass.cOGSSubID), CacheGlobal = true)]
		[TemplateInventorySubAccount(false, typeof(InventoryItem.cOGSSubID), typeof(InventoryItem.cOGSAcctID),
			DisplayName = "Expense Sub.", DescriptionField = typeof(Sub.description))]
		protected virtual void _(Events.CacheAttached<InventoryItem.expenseSubID> e) { }
		#endregion
		#region CSAttribute
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[DBMatrixLocalizableDescription(60, IsUnicode = true)]
		protected virtual void _(Events.CacheAttached<CSAttribute.description> eventArgs) { }
		#endregion
		#region CSAttributeDetail
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[DBMatrixLocalizableDescription(60, IsUnicode = true)]
		protected virtual void _(Events.CacheAttached<CSAttributeDetail.description> eventArgs) { }
		#endregion
		#endregion // Cache Attached

		#region Event Handlers
		#region InventoryItem
		protected virtual void _(Events.FieldSelecting<InventoryItem, InventoryItem.hasChild> eventArgs)
		{
			if (eventArgs.Row?.InventoryID > 0 && eventArgs.Row.IsTemplate == true && eventArgs.Row.HasChild == null)
			{
				var childSelect = new
					SelectFrom<InventoryItem>.
					Where<InventoryItem.templateItemID.IsEqual<@P.AsInt>>.
					View.ReadOnly(this);

				using (new PXFieldScope(childSelect.View, typeof(InventoryItem.inventoryID)))
				{
					// Acuminator disable once PX1015 IncorrectNumberOfSelectParameters [False positive]
					InventoryItem child = childSelect.Select(eventArgs.Row.InventoryID);
					eventArgs.Row.HasChild = (child != null);
				}
			}

			eventArgs.ReturnValue = eventArgs.Row?.HasChild;
		}

		protected override void _(Events.RowSelected<InventoryItem> e)
		{
			base._(e);

			if (e.Row == null) return;

			PXUIFieldAttribute.SetEnabled<InventoryItem.cOGSSubID>(e.Cache, e.Row, (postclass.Current != null && postclass.Current.COGSSubFromSales == false));
			PXUIFieldAttribute.SetEnabled<InventoryItem.stdCstVarAcctID>(e.Cache, e.Row, e.Row?.ValMethod == INValMethod.Standard);
			PXUIFieldAttribute.SetEnabled<InventoryItem.stdCstVarSubID>(e.Cache, e.Row, e.Row?.ValMethod == INValMethod.Standard);
			PXUIFieldAttribute.SetEnabled<InventoryItem.stdCstRevAcctID>(e.Cache, e.Row, e.Row?.ValMethod == INValMethod.Standard);
			PXUIFieldAttribute.SetEnabled<InventoryItem.stdCstRevSubID>(e.Cache, e.Row, e.Row?.ValMethod == INValMethod.Standard);
			PXUIFieldAttribute.SetEnabled<InventoryItem.pendingStdCost>(e.Cache, e.Row, e.Row?.ValMethod == INValMethod.Standard);
			PXUIFieldAttribute.SetEnabled<InventoryItem.pendingStdCostDate>(e.Cache, e.Row, e.Row?.ValMethod == INValMethod.Standard);
			PXUIFieldAttribute.SetVisible<InventoryItem.defaultSubItemOnEntry>(e.Cache, null, insetup.Current.UseInventorySubItem == true);
			INAcctSubDefault.Required(e.Cache, new PXRowSelectedEventArgs(e.Row));

			PXUIFieldAttribute.SetVisible<InventoryItem.defaultSubItemOnEntry>(e.Cache, null, insetup.Current.UseInventorySubItem == true);

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

			FieldsDependOnStkItemFlag(e.Cache, e.Row);

			var hasChildObject = e.Cache.GetValueExt<InventoryItem.hasChild>(e.Row);
			bool hasChild = ((hasChildObject is PXFieldState s) ? (bool?)s.Value : (bool?)hasChildObject) == true;
			e.Cache.AdjustUI().For<InventoryItem.itemClassID>(a => a.Enabled = !hasChild)
				.SameFor<InventoryItem.stkItem>()
				.SameFor<InventoryItem.baseUnit>()
				.SameFor<InventoryItem.decimalBaseUnit>()
				.SameFor<InventoryItem.purchaseUnit>()
				.SameFor<InventoryItem.decimalPurchaseUnit>()
				.SameFor<InventoryItem.salesUnit>()
				.SameFor<InventoryItem.decimalSalesUnit>();
		}

		protected virtual void _(Events.RowPersisting<InventoryItem> eventArgs)
		{
			if (eventArgs.Row?.IsTemplate == true)
			{
				ValidateChangeStkItemFlag();
				ValidateMainFieldsAreNotChanged(eventArgs.Cache, eventArgs.Row);

				if (eventArgs.Row.StkItem != true)
				{
					if (!PXAccess.FeatureInstalled<FeaturesSet.inventory>())
					{
						eventArgs.Row.NonStockReceipt = false;
						eventArgs.Row.NonStockShip = false;
					}

					if (!PXAccess.FeatureInstalled<FeaturesSet.pOReceiptsWithoutInventory>())
					{
						eventArgs.Row.NonStockReceiptAsService = eventArgs.Row.NonStockReceipt;
					}

					if (eventArgs.Row.NonStockReceipt == true && string.IsNullOrEmpty(eventArgs.Row.PostClassID))
					{
						throw new PXRowPersistingException(nameof(InventoryItem.postClassID),
							eventArgs.Row.PostClassID, ErrorMessages.FieldIsEmpty, nameof(InventoryItem.postClassID));
					}
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.itemClassID> eventArgs)
		{
			var row = eventArgs.Row;
			if (row != null && row.ItemClassID < 0)
			{
				INItemClass ic = ItemClass.Select();
				row.ParentItemClassID = ic?.ParentItemClassID;
			}
			else if (row != null)
			{
				row.ParentItemClassID = row.ItemClassID;
			}

			if (doResetDefaultsOnItemClassChange)
				ResetDefaultsOnItemClassChange(row);

			if (row != null && row.ItemClassID != null && eventArgs.ExternalCall)
			{
				Answers.Cache.Clear();
			}
		}

		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.postClassID> eventArgs)
		{
			eventArgs.Cache.SetDefaultExt<InventoryItem.invtAcctID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<InventoryItem.invtSubID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<InventoryItem.salesAcctID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<InventoryItem.salesSubID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<InventoryItem.cOGSAcctID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<InventoryItem.cOGSSubID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<InventoryItem.stdCstVarAcctID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<InventoryItem.stdCstVarSubID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<InventoryItem.stdCstRevAcctID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<InventoryItem.stdCstRevSubID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<InventoryItem.pPVAcctID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<InventoryItem.pPVSubID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<InventoryItem.pOAccrualAcctID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<InventoryItem.pOAccrualSubID>(eventArgs.Row);

			eventArgs.Cache.SetDefaultExt<InventoryItem.reasonCodeSubID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<InventoryItem.lCVarianceAcctID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<InventoryItem.lCVarianceSubID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<InventoryItem.deferralAcctID>(eventArgs.Row);
			eventArgs.Cache.SetDefaultExt<InventoryItem.deferralSubID>(eventArgs.Row);

			if (eventArgs.Row != null && eventArgs.Row.StkItem != true)
			{
				eventArgs.Cache.SetDefaultExt<InventoryItem.expenseAccrualAcctID>(eventArgs.Row);
				eventArgs.Cache.SetDefaultExt<InventoryItem.expenseAccrualSubID>(eventArgs.Row);
				eventArgs.Cache.SetDefaultExt<InventoryItem.expenseAcctID>(eventArgs.Row);
				eventArgs.Cache.SetDefaultExt<InventoryItem.expenseSubID>(eventArgs.Row);
			}
		}

		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.dfltSiteID> eventArgs)
		{
			INSite site = INSite.PK.Find(this, ((InventoryItem)eventArgs.Row).DfltSiteID);

			eventArgs.Row.DfltShipLocationID = site?.ShipLocationID;
			eventArgs.Row.DfltReceiptLocationID = site?.ReceiptLocationID;
		}

		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.stkItem> eventArgs)
		{
			eventArgs.Cache.SetValueExt<InventoryItem.itemType>(eventArgs.Row, null);
			eventArgs.Cache.SetValueExt<InventoryItem.itemClassID>(eventArgs.Row, null);
			eventArgs.Cache.SetValueExt<InventoryItem.invtAcctID>(eventArgs.Row, null);
			eventArgs.Cache.SetValueExt<InventoryItem.invtSubID>(eventArgs.Row, null);
			eventArgs.Cache.SetValueExt<InventoryItem.cOGSAcctID>(eventArgs.Row, null);
			eventArgs.Cache.SetValueExt<InventoryItem.cOGSSubID>(eventArgs.Row, null);
			eventArgs.Cache.SetValueExt<InventoryItem.expenseAccrualAcctID>(eventArgs.Row, null);
			eventArgs.Cache.SetValueExt<InventoryItem.expenseAccrualSubID>(eventArgs.Row, null);
			eventArgs.Cache.SetValueExt<InventoryItem.expenseAcctID>(eventArgs.Row, null);
			eventArgs.Cache.SetValueExt<InventoryItem.expenseSubID>(eventArgs.Row, null);
			eventArgs.Cache.SetDefaultExt<InventoryItem.valMethod>(eventArgs.Row);
		}

		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.defaultColumnMatrixAttributeID> e)
		{
			if (e.NewValue as string == MatrixAttributeSelectorAttribute.DummyAttributeName)
				EnsureDummyAttributeExists();

			GetAttributeGroupHelper().Recalculate(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<InventoryItem, InventoryItem.defaultRowMatrixAttributeID> e)
		{
			if (e.NewValue as string == MatrixAttributeSelectorAttribute.DummyAttributeName)
				EnsureDummyAttributeExists();

			GetAttributeGroupHelper().Recalculate(e.Row);
		}
		#endregion
		#region ExcludedField
		protected virtual void _(Events.FieldSelecting<ExcludedField, ExcludedField.tableName> eventArgs)
		{
			var tables = GetCreateMatrixItemsHelper().GetTablesToUpdateItem();

			// Acuminator disable once PX1070 UiPresentationLogicInEventHandlers The soution is copied from Generic Inquiry (Conditions -> Data Field), see GIWhere_DataFieldName_FieldSelecting (SetList)
			PXStringListAttribute.SetList<ExcludedField.tableName>(eventArgs.Cache, eventArgs.Row,
				tables.Select(t => (t.Dac.FullName, t.DisplayName)).ToArray());
		}
		#endregion // ExcludedField Events
		#region ExcludedAttribute
		protected virtual void _(Events.RowInserted<ExcludedAttribute> eventArgs)
		{
			AttributesExcludedFromUpdate.View.RequestRefresh(); // To show correct value of combo value, otherwise there is a value instead of a description
		}
		#endregion // ExcludedAttribute Events
		#endregion // Event handlers

		#region Methods

		public virtual CreateMatrixItemsHelper GetCreateMatrixItemsHelper()
			=> new CreateMatrixItemsHelper(this);

		public virtual AttributeGroupHelper GetAttributeGroupHelper()
			=> new AttributeGroupHelper(this);

		protected virtual void ValidateChangeStkItemFlag()
		{
			InventoryItem childWithDifferentStkItemValue =
				SelectFrom<InventoryItem>.
				Where<
					InventoryItem.templateItemID.IsEqual<InventoryItem.inventoryID.FromCurrent>.
					And<InventoryItem.stkItem.IsNotEqual<InventoryItem.stkItem.FromCurrent>>>.
				View.Select(this);

			if (childWithDifferentStkItemValue != null)
			{
				throw new PXSetPropertyException<InventoryItem.stkItem>(Messages.ItIsNotAllowedToChangeStkItemFlagIfChildExists);
			}
		}

		protected virtual void ValidateMainFieldsAreNotChanged(PXCache cache, InventoryItem row)
		{
			bool hasChild = ReloadHasChild();

			if (!hasChild)
				return;
			
			InventoryItem oldRow =
				SelectFrom<InventoryItem>.
				Where<InventoryItem.inventoryID.IsEqual<InventoryItem.inventoryID.FromCurrent>>.
				View.ReadOnly.Select(this);

			if (oldRow != null)
			{
				if (!cache.ObjectsEqual<InventoryItem.itemClassID,
					InventoryItem.baseUnit, InventoryItem.decimalBaseUnit,
					InventoryItem.salesUnit, InventoryItem.decimalSalesUnit,
					InventoryItem.purchaseUnit, InventoryItem.decimalPurchaseUnit>(oldRow, row))
					throw new PXException(Messages.ItIsNotAllowedToChangeMainFieldsIfChildExists);
			}
		}

		protected virtual void ValidatePackaging(InventoryItem row)
		{
			PXUIFieldAttribute.SetError<InventoryItem.weightUOM>(Item.Cache, row, null);
			PXUIFieldAttribute.SetError<InventoryItem.baseItemWeight>(Item.Cache, row, null);
			PXUIFieldAttribute.SetError<InventoryItem.volumeUOM>(Item.Cache, row, null);
			PXUIFieldAttribute.SetError<InventoryItem.baseItemVolume>(Item.Cache, row, null);

			//validate weight & volume:
			if (row.PackageOption == INPackageOption.Weight || row.PackageOption == INPackageOption.WeightAndVolume)
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

				if ((row.PackageOption == INPackageOption.Weight || row.PackageOption == INPackageOption.WeightAndVolume) && box.MaxWeight.GetValueOrDefault() == 0)
				{
					Boxes.Cache.RaiseExceptionHandling<INItemBoxEx.boxID>(box, box.BoxID, new PXSetPropertyException(Messages.MaxWeightIsNotDefined, PXErrorLevel.Warning));
				}

				if (row.PackageOption == INPackageOption.WeightAndVolume && box.MaxVolume.GetValueOrDefault() == 0)
				{
					Boxes.Cache.RaiseExceptionHandling<INItemBoxEx.boxID>(box, box.BoxID, new PXSetPropertyException(Messages.MaxVolumeIsNotDefined, PXErrorLevel.Warning));
				}

				if ((row.PackageOption == INPackageOption.Weight || row.PackageOption == INPackageOption.WeightAndVolume) &&
					(box.MaxWeight.GetValueOrDefault() < row.BaseItemWeight.GetValueOrDefault() ||
					(box.MaxVolume > 0 && row.BaseItemVolume > box.MaxVolume)))
				{
					Boxes.Cache.RaiseExceptionHandling<INItemBoxEx.boxID>(box, box.BoxID, new PXSetPropertyException(Messages.ItemDontFitInTheBox, PXErrorLevel.Warning));
				}

			}
		}

		protected virtual bool ReloadHasChild()
		{
			var childSelect = new
				SelectFrom<InventoryItem>.
				Where<InventoryItem.templateItemID.IsEqual<InventoryItem.inventoryID.FromCurrent>>.
				View.ReadOnly(this);

			childSelect.Cache.ClearQueryCache();

			using (new PXFieldScope(childSelect.View, typeof(InventoryItem.inventoryID)))
			{
				InventoryItem child = childSelect.Select();
				return (child != null);
			}
		}

		public virtual InventoryItem UpdateChild(InventoryItem item)
		{
			var oldCurrent = Item.Current;
			try
			{
				return Item.Update(item);
			}
			finally
			{
				if (oldCurrent != null)
					Item.Current = oldCurrent; // Restore Template to Current property
			}
		}

		protected virtual void ResetDefaultsOnItemClassChange(InventoryItem row)
		{
			var cache = Item.Cache;

			cache.SetDefaultExt<InventoryItem.postClassID>(row);
			cache.SetDefaultExt<InventoryItem.priceClassID>(row);
			cache.SetDefaultExt<InventoryItem.priceWorkgroupID>(row);
			cache.SetDefaultExt<InventoryItem.priceManagerID>(row);
			cache.SetDefaultExt<InventoryItem.markupPct>(row);
			cache.SetDefaultExt<InventoryItem.minGrossProfitPct>(row);

			INItemClass ic = ItemClass.Select();
			if (ic != null)
			{
				cache.SetValue<InventoryItem.priceWorkgroupID>(row, ic.PriceWorkgroupID);
				cache.SetValue<InventoryItem.priceManagerID>(row, ic.PriceManagerID);
			}

			cache.SetDefaultExt<InventoryItem.lotSerClassID>(row);

			ResetConversionsSettings(cache, row);

			cache.SetDefaultExt<InventoryItem.dfltSiteID>(row);
			cache.SetDefaultExt<InventoryItem.valMethod>(row);

			cache.SetDefaultExt<InventoryItem.taxCategoryID>(row);
			cache.SetDefaultExt<InventoryItem.itemType>(row);
		}

		private PXDelegateResult GetAttributesExcludedFromUpdate()
		{
			var internalView = new PXView(this, false, PXView.View.BqlSelect);

			int startRow = PXView.StartRow;
			int maximumRows = PXView.MaximumRows;
			int totalRows = 0;

			var result = new PXDelegateResult();
			result.IsResultTruncated = true;
			result.IsResultFiltered = true;
			result.IsResultSorted = !PXView.ReverseOrder;

			foreach (PXResult<ExcludedAttribute, CSAnswers> row in internalView.Select(
				PXView.Currents, PXView.Parameters, PXView.Searches, PXView.SortColumns, PXView.Descendings, PXView.Filters,
				ref startRow, maximumRows, ref totalRows))
			{
				ExcludedAttribute excludedAttribute = row;
				var answer = PXResult.Unwrap<CSAnswers>(row);

				var answerFromCache = Answers.Cache.Locate(new CSAnswers
				{
					AttributeID = excludedAttribute.FieldName,
					RefNoteID = PXView.Parameters.Length > 0 ? (PXView.Parameters[0] as Guid?) : null
				}) as CSAnswers;
				if (answerFromCache == null)
				{
					result.Add(row);
				}
				else if (Answers.Cache.GetStatus(answerFromCache).IsIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted))
				{
					result.Add(new PXResult<ExcludedAttribute, CSAnswers>(excludedAttribute, null));
				}
				else
				{
					result.Add(new PXResult<ExcludedAttribute, CSAnswers>(excludedAttribute, answerFromCache));
				}
			}

			PXView.StartRow = 0;

			return result;
		}
		#endregion // Methods

		#region Show/Hide Fields by Stock Item Flag

		protected virtual void FieldsDependOnStkItemFlag(PXCache cache, InventoryItem row)
		{
			bool isStock = row.StkItem == true;

			ItemTypeValuesDependOnStkItemFlag(cache, row, isStock);
			GeneralSettingsFieldsDependOnStkItemFlag(cache, row, isStock);
			FulfillmentFieldsDependOnStkItemFlag(cache, row, isStock);
			PriceCostInfoFieldsDependOnStkItemFlag(cache, row, isStock);
			VendorDetailsFieldsDependOnStkItemFlag(row, isStock);
			GLAccountsFieldsDependOnStkItemFlag(cache, row, isStock);
		}

		protected virtual void ItemTypeValuesDependOnStkItemFlag(PXCache cache, InventoryItem row, bool isStock)
		{
			INItemTypes.CustomListAttribute strings = isStock ? 
				(INItemTypes.CustomListAttribute)new INItemTypes.StockListAttribute() : new INItemTypes.NonStockListAttribute();

			PXStringListAttribute.SetList<InventoryItem.itemType>(cache, row, strings.AllowedValues, strings.AllowedLabels);
		}

		protected virtual void GeneralSettingsFieldsDependOnStkItemFlag(PXCache cache, InventoryItem row, bool isStock)
		{
			cache.AdjustUI(row)
				.For<InventoryItem.valMethod>(fa => fa.Visible = isStock)
				.SameFor<InventoryItem.lotSerClassID>()
				.SameFor<InventoryItem.countryOfOrigin>()
				.SameFor<InventoryItem.cycleID>()
				.SameFor<InventoryItem.aBCCodeID>()
				.SameFor<InventoryItem.aBCCodeIsFixed>()
				.SameFor<InventoryItem.movementClassID>()
				.SameFor<InventoryItem.movementClassIsFixed>()
				.SameFor<InventoryItem.dfltShipLocationID>()
				.SameFor<InventoryItem.dfltReceiptLocationID>()
				.SameFor<InventoryItem.defaultSubItemID>();

			cache.AdjustUI(row)
				.For<InventoryItem.completePOLine>(fa => fa.Visible = !isStock)
				.SameFor<InventoryItem.nonStockReceipt>()
				.SameFor<InventoryItem.nonStockShip>();

			bool isService = row.ItemType == INItemTypes.ServiceItem;
			bool fieldServiceVisible = !isStock && isService && PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();

			cache.AdjustUI(row).For("EstimatedDuration", fa => fa.Visible = fieldServiceVisible); // Field Service
			ItemClass.Cache.AdjustUI().For("Mem_RouteService", fa => fa.Visible = fieldServiceVisible); // Field Service
		}

		protected virtual void FulfillmentFieldsDependOnStkItemFlag(PXCache cache, InventoryItem row, bool isStock)
		{
			cache.AdjustUI(row).For<InventoryItem.hSTariffCode>(fa => fa.Visible = isStock)
				.SameFor<InventoryItem.packageOption>()
				.SameFor<InventoryItem.packSeparately>();
		}

		protected virtual void PriceCostInfoFieldsDependOnStkItemFlag(PXCache cache, InventoryItem row, bool isStock)
		{
			cache.AdjustUI(row).For<InventoryItem.accrueCost>(fa => fa.Visible = !isStock)
				.SameFor<InventoryItem.costBasis>()
				.SameFor<InventoryItem.percentOfSalesPrice>()
				.SameFor("DfltEarningType"); // Field Service
		}

		protected virtual void VendorDetailsFieldsDependOnStkItemFlag(InventoryItem row, bool isStock)
		{
			Caches[typeof(CR.Standalone.Location)].AdjustUI()
				.For<CR.Standalone.Location.vSiteID>(fa => fa.Visible = isStock)
				.SameFor<CR.Standalone.Location.vLeadTime>();
			
			VendorItems.Cache.AdjustUI(VendorItems.Current).For<POVendorInventory.subItemID>(fa => fa.Visible = isStock)
				.SameFor<POVendorInventory.overrideSettings>()
				.SameFor<POVendorInventory.subItemID>()
				.SameFor<POVendorInventory.addLeadTimeDays>()
				.SameFor<POVendorInventory.minOrdFreq>()
				.SameFor<POVendorInventory.minOrdQty>()
				.SameFor<POVendorInventory.maxOrdQty>()
				.SameFor<POVendorInventory.lotSize>()
				.SameFor<POVendorInventory.eRQ>();
		}

		protected virtual void GLAccountsFieldsDependOnStkItemFlag(PXCache cache, InventoryItem row, bool isStock)
		{
			cache.AdjustUI(row).For<InventoryItem.stdCstRevAcctID>(fa => fa.Visible = isStock)
				.SameFor<InventoryItem.stdCstRevSubID>()
				.SameFor<InventoryItem.stdCstVarAcctID>()
				.SameFor<InventoryItem.stdCstVarSubID>()
				.SameFor<InventoryItem.lCVarianceAcctID>()
				.SameFor<InventoryItem.lCVarianceSubID>()
				
				.SameFor<InventoryItem.invtAcctID>()
				.SameFor<InventoryItem.invtSubID>()
				.SameFor<InventoryItem.cOGSAcctID>()
				.SameFor<InventoryItem.cOGSSubID>();

			cache.AdjustUI(row).For<InventoryItem.expenseAccrualAcctID>(fa => fa.Visible = !isStock)
				.SameFor<InventoryItem.expenseAccrualSubID>()
				.SameFor<InventoryItem.expenseAcctID>()
				.SameFor<InventoryItem.expenseSubID>();
		}

		#endregion // Show/Hide Fields by Stock Item Flag

		#region Dummy Attribute Creation

		protected virtual void EnsureDummyAttributeExists()
		{
			EnsureDummyAttributeHeaderExists();
			EnsureDummyAttributeValueExists();
		}

		protected virtual void EnsureDummyAttributeHeaderExists()
		{
			const string DummyAttributeName = MatrixAttributeSelectorAttribute.DummyAttributeName;

			if (CRAttribute.Attributes[DummyAttributeName] == null &&
				DummyAttribute.SelectSingle() == null)
			{
				var newAttribute = DummyAttribute.Insert(new CSAttribute() { AttributeID = DummyAttributeName });

				newAttribute.ControlType = CSAttribute.Combo;
				newAttribute.Description = DummyAttributeName;

				DBMatrixLocalizableDescriptionAttribute.SetTranslations<CSAttribute.description>(
					DummyAttribute.Cache, newAttribute, (locale) => newAttribute.Description);

				DummyAttribute.Update(newAttribute);
			}
		}

		protected virtual void EnsureDummyAttributeValueExists()
		{
			const string DummyAttributeName = MatrixAttributeSelectorAttribute.DummyAttributeName;

			if (CRAttribute.Attributes[DummyAttributeName]?.Values?.Any() != true &&
				DummyAttributeValue.SelectSingle() == null)
			{
				var newDetail = DummyAttributeValue.Insert(new CSAttributeDetail()
				{
					AttributeID = DummyAttributeName,
					ValueID = MatrixAttributeSelectorAttribute.DummyAttributeValue
				});

				newDetail.Description = newDetail.ValueID;

				DBMatrixLocalizableDescriptionAttribute.SetTranslations<CSAttribute.description>(
					DummyAttributeValue.Cache, newDetail, (locale) => newDetail.Description);

				DummyAttributeValue.Update(newDetail);
			}
		}

		#endregion Dummy Attribute Creation
	}
}