using System;

using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;

using PX.TM;
using PX.Objects.Common;
using PX.Objects.Common.Extensions;
using PX.Objects.GL;
using PX.Objects.CS;
using PX.Objects.TX;
using PX.Objects.EP;
using PX.Objects.DR;
using PX.Objects.CR;
using PX.Objects.IN.Matrix.Graphs;
using PX.Objects.IN.Matrix.Attributes;

// TODO: move to the namespace-level once AC-183872 is fixed
using SelectParentPostClass = PX.Data.BQL.Fluent.SelectFrom<PX.Objects.IN.INPostClass>.Where<PX.Objects.IN.INPostClass.postClassID.IsEqual<PX.Objects.IN.InventoryItem.postClassID.FromCurrent>>;
using SelectParentItemClass = PX.Data.BQL.Fluent.SelectFrom<PX.Objects.IN.INItemClass>.Where<PX.Objects.IN.INItemClass.itemClassID.IsEqual<PX.Objects.IN.InventoryItem.itemClassID.FromCurrent>>;

namespace PX.Objects.IN
{
	/// <summary>
	/// Represents Stock Items (have value and stored in a warehouse) and
	/// Non-Stock Items (not kept in a warehouse and immediately available for purchase).
	/// Whether the item is Stock or Non-Stock is determined by the value of the <see cref="StkItem"/> field.
	/// The records of this type are created and edited through the Stock Items (IN.20.25.00)
	/// (corresponds to the <see cref="InventoryItemMaint"/> graph) and
	/// the Non-Stock Items (IN.20.20.00) (corresponds to the <see cref="NonStockItemMaint"/> graph) screens.
	/// </summary>
	[PXPrimaryGraph(
		new Type[] {
			typeof(NonStockItemMaint),
			typeof(InventoryItemMaint),
			typeof(TemplateInventoryItemMaint)
		},
		new Type[] {
			typeof(Where<isTemplate.IsEqual<False>.And<stkItem.IsEqual<False>>>),
			typeof(Where<isTemplate.IsEqual<False>.And<stkItem.IsEqual<True>>>),
			typeof(Where<isTemplate.IsEqual<True>>)
		})]
	[PXCacheName(Messages.InventoryItem, PXDacType.Catalogue, CacheGlobal = true)]
	public class InventoryItem : IBqlTable, PX.SM.IIncludable
	{
		#region Keys
		public class PK : PrimaryKeyOf<InventoryItem>.By<inventoryID>
		{
			public static InventoryItem Find(PXGraph graph, int? inventoryID) => FindBy(graph, inventoryID);
			public static InventoryItem FindDirty(PXGraph graph, int? inventoryID)
				=> (InventoryItem)PXSelect<InventoryItem, Where<inventoryID, Equal<Required<inventoryID>>>>.SelectWindowed(graph, 0, 1, inventoryID);
		}
		public class UK : PrimaryKeyOf<InventoryItem>.By<inventoryCD>
		{
			public static InventoryItem Find(PXGraph graph, string inventoryCD) => FindBy(graph, inventoryCD);
		}
		public static class FK
		{
			public class TaxCategory : TX.TaxCategory.PK.ForeignKeyOf<InventoryItem>.By<taxCategoryID> { }
			public class SalesAccount : Account.PK.ForeignKeyOf<InventoryItem>.By<salesAcctID> { }
			public class SalesSubaccount : Sub.PK.ForeignKeyOf<InventoryItem>.By<salesSubID> { }
			public class InventoryAccount : Account.PK.ForeignKeyOf<InventoryItem>.By<invtAcctID> { }
			public class InventorySubaccount : Sub.PK.ForeignKeyOf<InventoryItem>.By<invtSubID> { }
			public class COGSAccount : Account.PK.ForeignKeyOf<InventoryItem>.By<cOGSAcctID> { }
			public class COGSSubaccount : Sub.PK.ForeignKeyOf<InventoryItem>.By<cOGSSubID> { }
			public class StandardCostRevaluationAccount : Account.PK.ForeignKeyOf<InventoryItem>.By<stdCstRevAcctID> { }
			public class StandardCostRevaluationSubaccount : Sub.PK.ForeignKeyOf<InventoryItem>.By<stdCstRevSubID> { }
			public class PPVAccount : Account.PK.ForeignKeyOf<InventoryItem>.By<pPVAcctID> { }
			public class PPVSubaccount : Sub.PK.ForeignKeyOf<InventoryItem>.By<pPVSubID> { }
			public class DeferralAccount : Account.PK.ForeignKeyOf<InventoryItem>.By<deferralAcctID> { }
			public class DeferralSubaccount : Sub.PK.ForeignKeyOf<InventoryItem>.By<deferralSubID> { }
			public class LastSite : INSite.PK.ForeignKeyOf<InventoryItem>.By<lastSiteID> { }
			public class StandardCostVarianceAccount : Account.PK.ForeignKeyOf<InventoryItem>.By<stdCstVarAcctID> { }
			public class StandardCostVarianceSubaccount : Sub.PK.ForeignKeyOf<InventoryItem>.By<stdCstVarSubID> { }
			public class POAccrualAccount : Account.PK.ForeignKeyOf<InventoryItem>.By<pOAccrualAcctID> { }
			public class POAccrualSubaccount : Sub.PK.ForeignKeyOf<InventoryItem>.By<pOAccrualSubID> { }
			public class ItemClass : INItemClass.PK.ForeignKeyOf<InventoryItem>.By<itemClassID> { }
			public class PostClass : INPostClass.PK.ForeignKeyOf<InventoryItem>.By<postClassID> { }
			public class DefaultSite : INSite.PK.ForeignKeyOf<InventoryItem>.By<dfltSiteID> { }
			public class DefaultShipLocation : INLocation.PK.ForeignKeyOf<InventoryItem>.By<dfltShipLocationID> { }
			public class DefaultReceiptLocation : INLocation.PK.ForeignKeyOf<InventoryItem>.By<dfltReceiptLocationID> { }
			public class DefaultSubItem : INSubItem.PK.ForeignKeyOf<InventoryItem>.By<defaultSubItemID> { }
			public class PriceClass : INPriceClass.PK.ForeignKeyOf<InventoryItem>.By<priceClassID> { }
			public class ReasonCodeSubaccount : Sub.PK.ForeignKeyOf<InventoryItem>.By<reasonCodeSubID> { }
			public class PICycle : INPICycle.PK.ForeignKeyOf<InventoryItem>.By<cycleID> { }
			public class LotSerialClass : INLotSerClass.PK.ForeignKeyOf<InventoryItem>.By<lotSerClassID> { }
			public class LandedCostVarianceAccount : Account.PK.ForeignKeyOf<InventoryItem>.By<lCVarianceAcctID> { }
			public class LandedCostVarianceSubaccount : Sub.PK.ForeignKeyOf<InventoryItem>.By<lCVarianceSubID> { }
			public class ABCCode : INABCCode.PK.ForeignKeyOf<InventoryItem>.By<aBCCodeID> { }
			public class PreferredVendor : AP.Vendor.PK.ForeignKeyOf<InventoryItem>.By<preferredVendorID> { }
			public class PreferredVendorLocation : Location.PK.ForeignKeyOf<InventoryItem>.By<preferredVendorID, preferredVendorLocationID> { }
			public class ProductWorkgroup : EPCompanyTree.PK.ForeignKeyOf<InventoryItem>.By<productWorkgroupID> { }
			public class ProductManager : CR.Standalone.EPEmployee.PK.ForeignKeyOf<InventoryItem>.By<productManagerID> { }
			public class PriceWorkgroup : EPCompanyTree.PK.ForeignKeyOf<InventoryItem>.By<priceWorkgroupID> { }
			public class PriceManager : CR.Standalone.EPEmployee.PK.ForeignKeyOf<InventoryItem>.By<priceManagerID> { }
			public class DeferredCode : DRDeferredCode.PK.ForeignKeyOf<InventoryItem>.By<deferredCode> { }
			public class MovementClass : INMovementClass.PK.ForeignKeyOf<InventoryItem>.By<movementClassID> { }
			public class CountryOfOrigin : Country.PK.ForeignKeyOf<InventoryItem>.By<countryOfOrigin> { }
			//todo public class BaseUnitOfMeasure : INUnit.UK.ByInventory.ForeignKeyOf<INTranSplit>.By<inventoryID, baseUnit> { }
			//todo public class SalesUnitOfMeasure : INUnit.UK.ByInventory.ForeignKeyOf<INTranSplit>.By<inventoryID, salesUnit> { }
			//todo public class PurchaseUnitOfMeasure : INUnit.UK.ByInventory.ForeignKeyOf<INTranSplit>.By<inventoryID, purchaseUnit> { }
			//todo public class WeightUnitOfMeasure : INUnit.UK.ByGlobal.ForeignKeyOf<INTranSplit>.By<weightUOM> { }
			//todo public class VolumeUnitOfMeasure : INUnit.UK.ByGlobal.ForeignKeyOf<INTranSplit>.By<volumeUOM> { }

			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use SalesSubaccount instead.")]
			public class SalesSub : SalesSubaccount { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use InventorySubaccount instead.")]
			public class InvtSub : InventorySubaccount { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use COGSSubaccount instead.")]
			public class COGSSub : COGSSubaccount { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use StandardCostRevaluationSubaccount instead.")]
			public class StdCstRevSub : StandardCostRevaluationSubaccount { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use PPVSubaccount instead.")]
			public class PPVSub : PPVSubaccount { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use DeferralSubaccount instead.")]
			public class DeferralSub : DeferralSubaccount { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use StandardCostVarianceSubaccount instead.")]
			public class StdCstVarSub : StandardCostVarianceSubaccount { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use POAccrualSubaccount instead.")]
			public class POAccrualSub : POAccrualSubaccount { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use DefaultSite instead.")]
			public class DfltSite : DefaultSite { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use DefaultShipLocation instead.")]
			public class DfltShipLocation : DefaultShipLocation { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use DefaultReceiptLocation instead.")]
			public class DfltReceiptLocation : DefaultReceiptLocation { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use ReasonCodeSubaccount instead.")]
			public class ReasonCodeSub : ReasonCodeSubaccount { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use LotSerialClass instead.")]
			public class LotSerClass : LotSerialClass { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use LandedCostVarianceSubaccount instead.")]
			public class LCVarianceSub : LandedCostVarianceSubaccount { }
		}
		#endregion

		#region Selected
		/// <summary>
		/// Indicates whether the record is selected for mass processing.
		/// </summary>
		[PXBool]
		[PXUIField(DisplayName = "Selected")]
		[PXFormula(typeof(False))]
		public virtual Boolean? Selected { get; set; }
		public abstract class selected : BqlBool.Field<selected> { }
		#endregion
		#region InventoryID
		/// <summary>
		/// Database identity.
		/// The unique identifier of the Inventory Item.
		/// </summary>
		[PXDBIdentity]
		[PXUIField(DisplayName = "Inventory ID", Visibility = PXUIVisibility.Visible, Visible = false)]
		[PXReferentialIntegrityCheck]
		public virtual Int32? InventoryID { get; set; }
		public abstract class inventoryID : BqlInt.Field<inventoryID> { }
		#endregion
		#region InventoryCD
		/// <summary>
		/// Key field.
		/// The user-friendly unique identifier of the Inventory Item.
		/// The structure of the identifier is determined by the <i>INVENTORY</i> <see cref="CS.Dimension">Segmented Key</see>.
		/// </summary>
		[PXDefault]
		[InventoryRaw(IsKey = true, DisplayName = "Inventory ID")]
		public virtual String InventoryCD { get; set; }
		public abstract class inventoryCD : BqlString.Field<inventoryCD> { }
		#endregion
		#region StkItem
		/// <summary>
		/// When set to <c>true</c>, indicates that this item is a Stock Item.
		/// </summary>
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Stock Item")]
		public virtual Boolean? StkItem { get; set; }
		public abstract class stkItem : BqlBool.Field<stkItem> { }
		#endregion
		#region Descr
		/// <summary>
		/// The description of the Inventory Item.
		/// </summary>
		[DBMatrixLocalizableDescription(Constants.TranDescLength, IsUnicode = true)]
		[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
		[PX.Data.EP.PXFieldDescription]
		public virtual String Descr { get; set; }
		public abstract class descr : BqlString.Field<descr> { }
		#endregion
		#region ItemClassID
		/// <summary>
		/// The identifier of the <see cref="INItemClass">Item Class</see>, to which the Inventory Item belongs.
		/// Item Classes provide default settings for items, which belong to them, and are used to group items.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="INItemClass.ItemClassID"/> field.
		/// </value>
		[PXDBInt]
		[PXUIField(DisplayName = "Item Class", Visibility = PXUIVisibility.SelectorVisible)]
		[PXDimensionSelector(INItemClass.Dimension, typeof(Search<INItemClass.itemClassID>), typeof(INItemClass.itemClassCD), DescriptionField = typeof(INItemClass.descr), CacheGlobal = true)]
		[PXDefault(typeof(
			Search2<INItemClass.itemClassID, InnerJoin<INSetup,
				On<stkItem.FromCurrent.IsEqual<False>.And<INSetup.dfltNonStkItemClassID.IsEqual<INItemClass.itemClassID>>.
				Or<stkItem.FromCurrent.IsEqual<True>.And<INSetup.dfltStkItemClassID.IsEqual<INItemClass.itemClassID>>>>>>))]
		[PXUIRequired(typeof(INItemClass.stkItem))]
		public virtual int? ItemClassID { get; set; }
		public abstract class itemClassID : BqlInt.Field<itemClassID> { }
		#endregion
		#region ParentItemClassID
		/// <summary>
		/// The field is used to populate standard settings of <see cref="InventoryItem">Inventory Item</see> from
		/// <see cref="INItemClass">Item Class</see> when it's created On-The-Fly and not yet persisted.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="INItemClass.ItemClassID"/> field.
		/// </value>
		[PXInt]
		[PXDefault(typeof(
			Search2<INItemClass.itemClassID, InnerJoin<INSetup,
				On<stkItem.FromCurrent.IsEqual<False>.And<INSetup.dfltNonStkItemClassID.IsEqual<INItemClass.itemClassID>>.
				Or<stkItem.FromCurrent.IsEqual<True>.And<INSetup.dfltStkItemClassID.IsEqual<INItemClass.itemClassID>>>>>>),
			PersistingCheck = PXPersistingCheck.Nothing)]
		[PXDBCalced(typeof(itemClassID), typeof(int), Persistent = true)]
		public virtual int? ParentItemClassID { get; set; }
		public abstract class parentItemClassID : BqlInt.Field<parentItemClassID> { }
		#endregion
		#region ItemStatus
		/// <summary>
		/// The status of the Inventory Item.
		/// </summary>
		/// <value>
		/// Possible values are:
		/// <c>"AC"</c> - Active (can be used in inventory operations, such as issues and receipts),
		/// <c>"NS"</c> - No Sales (cannot be sold),
		/// <c>"NP"</c> - No Purchases (cannot be purchased),
		/// <c>"NR"</c> - No Request (cannot be used on requisition requests),
		/// <c>"IN"</c> - Inactive,
		/// <c>"DE"</c> - Marked for Deletion.
		/// Defaults to Active (<c>"AC"</c>).
		/// </value>
		[PXDBString(2, IsFixed = true)]
		[PXDefault("AC")]
		[PXUIField(DisplayName = "Item Status", Visibility = PXUIVisibility.SelectorVisible)]
		[InventoryItemStatus.List]
		public virtual String ItemStatus { get; set; }
		public abstract class itemStatus : BqlString.Field<itemStatus> { }
		#endregion
		#region ItemType
		/// <summary>
		/// The type of the Inventory Item.
		/// </summary>
		/// <value>
		/// Possible values are:
		/// <c>"F"</c> - Finished Good (Stock Items only),
		/// <c>"M"</c> - Component Part (Stock Items only),
		/// <c>"A"</c> - Subassembly (Stock Items only),
		/// <c>"N"</c> - Non-Stock Item (a general type of Non-Stock Item),
		/// <c>"L"</c> - Labor (Non-Stock Items only),
		/// <c>"S"</c> - Service (Non-Stock Items only),
		/// <c>"C"</c> - Charge (Non-Stock Items only),
		/// <c>"E"</c> - Expense (Non-Stock Items only).
		/// Defaults to the <see cref="INItemClass.ItemType">Type</see> associated with the <see cref="ItemClassID">Item Class</see>
		/// of the item if it's specified, or to Finished Good (<c>"F"</c>) otherwise.
		/// </value>
		[PXDBString(1, IsFixed = true)]
		[PXDefault(INItemTypes.FinishedGood, typeof(SelectParentItemClass), SourceField = typeof(INItemClass.itemType), CacheGlobal = true)]
		[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible)]
		[INItemTypes.List]
		public virtual String ItemType { get; set; }
		public abstract class itemType : BqlString.Field<itemType> { }
		#endregion
		#region ValMethod
		/// <summary>
		/// The method used for inventory valuation of the item (Stock Items only).
		/// </summary>
		/// <value>
		/// Allowed values are:
		/// <c>"T"</c> - Standard,
		/// <c>"A"</c> - Average,
		/// <c>"F"</c> - FIFO,
		/// <c>"S"</c> - Specific.
		/// Defaults to the <see cref="INItemClass.ValMethod">Valuation Method</see> associated with the <see cref="ItemClassID">Item Class</see>
		/// of the item if it's specified, or to Standard (<c>"T"</c>) otherwise.
		/// </value>
		[PXDBString(1, IsFixed = true)]
		[PXDefault(INValMethod.Standard, typeof(SelectParentItemClass), SourceField = typeof(INItemClass.valMethod), CacheGlobal = true)]
		[PXUIField(DisplayName = "Valuation Method")]
		[INValMethod.List]
		public virtual String ValMethod { get; set; }
		public abstract class valMethod : BqlString.Field<valMethod> { }
		#endregion
		#region TaxCategoryID
		/// <summary>
		/// Identifier of the <see cref="TaxCategory"/> associated with the item.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INItemClass.TaxCategoryID">Tax Category</see> associated with the <see cref="ItemClassID">Item Class</see>.
		/// Corresponds to the <see cref="TaxCategory.TaxCategoryID"/> field.
		/// </value>
		[PXDBString(10, IsUnicode = true)]
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(INItemClass.taxCategoryID), CacheGlobal = true)]
		[PXUIField(DisplayName = "Tax Category", Visibility = PXUIVisibility.Visible)]
		[PXSelector(typeof(TaxCategory.taxCategoryID), DescriptionField = typeof(TaxCategory.descr))]
		[PXRestrictor(typeof(Where<TaxCategory.active.IsEqual<True>>), TX.Messages.InactiveTaxCategory, typeof(TaxCategory.taxCategoryID))]
		public virtual String TaxCategoryID { get; set; }
		public abstract class taxCategoryID : BqlString.Field<taxCategoryID> { }
		#endregion
		#region TaxCalcMode
		/// <summary>
		/// The tax calculation mode, which defines which amounts (tax-inclusive or tax-exclusive) 
		/// should be entered in the detail lines of a document. 
		/// This field is displayed only if the <see cref="FeaturesSet.NetGrossEntryMode"/> field is set to <c>true</c>.
		/// </summary>
		/// <value>
		/// The field can have one of the following values:
		/// <c>"T"</c> (Tax Settings): The tax amount for the document is calculated according to the settings of the applicable tax or taxes.
		/// <c>"G"</c> (Gross): The amount in the document detail line includes a tax or taxes.
		/// <c>"N"</c> (Net): The amount in the document detail line does not include taxes.
		/// </value>
		[PXDBString(1, IsFixed = true)]
		[PXFormula(typeof(Switch<Case<Where<itemClassID.IsNotNull>,
							Selector<itemClassID, INItemClass.taxCalcMode>>,
							TaxCalculationMode.taxSetting>))]
		[TaxCalculationMode.List]
		[PXDefault(TaxCalculationMode.TaxSetting)]
		[PXUIField(DisplayName = "Tax Calculation Mode")]
		public virtual string TaxCalcMode { get; set; }
		public abstract class taxCalcMode : BqlString.Field<taxCalcMode> { }
		#endregion
		#region WeightItem
		/// <summary>
		/// When set to <c>true</c>, indicates that this item is a Weight Item.
		/// </summary>
		[PXDBBool]
		[PXDefault(false)]
		[PXUIVisible(typeof(stkItem))]
		[PXUIEnabled(typeof(stkItem.IsEqual<True>.And<Use<Selector<lotSerClassID, INLotSerClass.lotSerTrack>>.AsString.IsNotEqual<INLotSerTrack.serialNumbered>>))]
		[PXFormula(typeof(False.When<Use<Selector<lotSerClassID, INLotSerClass.lotSerTrack>>.AsString.IsEqual<INLotSerTrack.serialNumbered>>.Else<weightItem>))]
		[PXUIField(DisplayName = "Weight Item")]
		public virtual Boolean? WeightItem { get; set; }
		public abstract class weightItem : BqlBool.Field<weightItem> { }
		#endregion
		#region BaseUnit
		/// <summary>
		/// The <see cref="INUnit">Unit of Measure</see> used as the base unit for the Inventory Item.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INItemClass.BaseUnit">Base Unit</see> associated with the <see cref="ItemClassID">Item Class</see>.
		/// Corresponds to the <see cref="INUnit.FromUnit"/> field.
		/// </value>
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(INItemClass.baseUnit), CacheGlobal = true)]
		[INSyncUoms(typeof(salesUnit), typeof(purchaseUnit))]
		[INUnit(DisplayName = "Base Unit", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String BaseUnit { get; set; }
		public abstract class baseUnit : BqlString.Field<baseUnit>
		{
			public class PreventEditIfExists<TSelect> : PreventEditOf<baseUnit>.On<InventoryItemMaintBase>.IfExists<TSelect>
				where TSelect : BqlCommand, new()
			{
				protected override String CreateEditPreventingReason(GetEditPreventingReasonArgs arg, Object firstPreventingEntity, String fieldName, String currentTableName, String foreignTableName)
				{
					PXCache cache = Base.Caches[firstPreventingEntity.GetType()];
					return PXMessages.Localize(Messages.BaseUnitCouldNotBeChanged) + Environment.NewLine + cache.GetRowDescription(firstPreventingEntity);
				}
			}
		}
		#endregion
		#region SalesUnit
		/// <summary>
		/// The <see cref="INUnit">Unit of Measure</see> used as the sales unit for the Inventory Item.
		/// This field can be changed only if the <see cref="FeaturesSet.MultipleUnitMeasure">Multiple Units of Measure feature</see> is enabled.
		/// Otherwise, the sales unit is assumed to be the same as the <see cref="BaseUnit">Base Unit</see>.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INItemClass.SalesUnit">Sales Unit</see> associated with the <see cref="ItemClassID">Item Class</see>.
		/// Corresponds to the <see cref="INUnit.FromUnit"/> field.
		/// </value>
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(INItemClass.salesUnit), CacheGlobal = true)]
		[INUnit(typeof(inventoryID), DisplayName = "Sales Unit", Visibility = PXUIVisibility.SelectorVisible, DirtyRead = true)]
		public virtual String SalesUnit { get; set; }
		public abstract class salesUnit : BqlString.Field<salesUnit> { }
		#endregion
		#region PurchaseUnit
		/// <summary>
		/// The <see cref="INUnit">Unit of Measure</see> used as the purchase unit for the Inventory Item.
		/// This field can be changed only if the <see cref="FeaturesSet.MultipleUnitMeasure">Multiple Units of Measure feature</see> is enabled.
		/// Otherwise, the purchase unit is assumed to be the same as the <see cref="BaseUnit">Base Unit</see>.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INItemClass.PurchaseUnit">Purchase Unit</see> associated with the <see cref="ItemClassID">Item Class</see>.
		/// Corresponds to the <see cref="INUnit.FromUnit"/> field.
		/// </value>
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(INItemClass.purchaseUnit), CacheGlobal = true)]
		[INUnit(typeof(inventoryID), DisplayName = "Purchase Unit", Visibility = PXUIVisibility.SelectorVisible, DirtyRead = true)]
		public virtual String PurchaseUnit { get; set; }
		public abstract class purchaseUnit : BqlString.Field<purchaseUnit> { }
		#endregion
		#region DecimalBaseUnit
		/// <summary>
		/// When set to <c>false</c>, indicates that the system will prevent enter of non-integer values into the quantity field for choosed <see cref="BaseUnit">Base Unit</see> value.
		/// <value>
		/// Defaults to <c>true</c></value>
		/// </summary>
		[PXDBBool]
		[INSyncUoms(typeof(decimalSalesUnit), typeof(decimalPurchaseUnit))]
		[PXDefault(true, typeof(SelectParentItemClass), SourceField = typeof(INItemClass.decimalBaseUnit), CacheGlobal = true)]
		[PXUIField(DisplayName = "Divisible Unit", Visibility = PXUIVisibility.Visible)]
		public virtual bool? DecimalBaseUnit { get; set; }
		public abstract class decimalBaseUnit : BqlBool.Field<decimalBaseUnit> { }
		#endregion
		#region DecimalSalesUnit
		/// <summary>
		/// When set to <c>false</c>, indicates that the system will prevent enter of non-integer values into the quantity field for choosed <see cref="SalesUnit">Sales Unit</see> value.
		/// <value>
		/// Defaults to <c>true</c></value>
		/// </summary>
		[PXDBBool]
		[PXDefault(true, typeof(SelectParentItemClass), SourceField = typeof(INItemClass.decimalSalesUnit), CacheGlobal = true)]
		[PXUIField(DisplayName = "Divisible Unit", Visibility = PXUIVisibility.Visible)]
		public virtual bool? DecimalSalesUnit { get; set; }
		public abstract class decimalSalesUnit : BqlBool.Field<decimalSalesUnit> { }
		#endregion
		#region DecimalPurchaseUnit
		/// <summary>
		/// When set to <c>false</c>, indicates that the system will prevent enter of non-integer values into the quantity field for choosed <see cref="PurchaseUnit">Purchase Unit</see> value.
		/// <value>
		/// Defaults to <c>true</c></value>
		/// </summary>
		[PXDBBool]
		[PXDefault(true, typeof(SelectParentItemClass), SourceField = typeof(INItemClass.decimalPurchaseUnit), CacheGlobal = true)]
		[PXUIField(DisplayName = "Divisible Unit", Visibility = PXUIVisibility.Visible)]
		public virtual bool? DecimalPurchaseUnit { get; set; }
		public abstract class decimalPurchaseUnit : BqlBool.Field<decimalPurchaseUnit> { }
		#endregion
		#region Commisionable
		/// <summary>
		/// When set to <c>true</c>, indicates that the system must calculate commission on the sale of this item.
		/// </summary>
		/// <value>
		/// Defaults to <c>false</c>.
		/// </value>
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Subject to Commission", Visibility = PXUIVisibility.Visible)]
		public virtual Boolean? Commisionable { get; set; }
		public abstract class commisionable : BqlBool.Field<commisionable> { }
		#endregion
		#region ReasonCodeSubID
		/// <summary>
		/// The identifier of the <see cref="Sub">Suabaccount</see> defined by the <see cref="ReasonCode">Reason Code</see>, associated with this item.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.ReasonCodeSubID">Reason Code Sub.</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Sub.SubID"/> field.
		/// </value>
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.reasonCodeSubID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[SubAccount(DisplayName = "Reason Code Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXForeignReference(typeof(FK.ReasonCodeSubaccount))]
		public virtual Int32? ReasonCodeSubID { get; set; }
		public abstract class reasonCodeSubID : BqlInt.Field<reasonCodeSubID> { }
		#endregion
		#region SalesAcctID
		/// <summary>
		/// The income <see cref="Account"/> used to record sales of the item.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.SalesAcctID">Sales Account</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Account.AccountID"/> field.
		/// </value>
		[Account(DisplayName = "Sales Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.salesAcctID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.SalesAccount))]
		public virtual Int32? SalesAcctID { get; set; }
		public abstract class salesAcctID : BqlInt.Field<salesAcctID> { }
		#endregion
		#region SalesSubID
		/// <summary>
		/// The <see cref="Sub">Subaccount</see> used to record sales of the item.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.SalesSubID">Sales Account</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Sub.SubID"/> field.
		/// </value>
		[SubAccount(typeof(salesAcctID), DisplayName = "Sales Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.salesSubID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.SalesSubaccount))]
		public virtual Int32? SalesSubID { get; set; }
		public abstract class salesSubID : BqlInt.Field<salesSubID> { }
		#endregion
		#region InvtAcctID
		/// <summary>
		/// The asset <see cref="Account"/> used to keep the inventory balance, resulting from the transactions with this item.
		/// Applicable only for Stock Items (see <see cref="StkItem"/>).
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.InvtAcctID">Inventory Account</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Account.AccountID"/> field.
		/// </value>
		[Account(DisplayName = "Inventory Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), ControlAccountForModule = ControlAccountModule.IN)]
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.invtAcctID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.InventoryAccount))]
		public virtual Int32? InvtAcctID { get; set; }
		public abstract class invtAcctID : BqlInt.Field<invtAcctID> { }
		#endregion
		#region InvtSubID
		/// <summary>
		/// The <see cref="Sub">Subaccount</see> used to keep the inventory balance, resulting from the transactions with this item.
		/// Applicable only for Stock Items (see <see cref="StkItem"/>).
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.InvtSubID">Inventory Sub.</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Sub.SubID"/> field.
		/// </value>
		[SubAccount(typeof(invtAcctID), DisplayName = "Inventory Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.invtSubID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.InventorySubaccount))]
		public virtual Int32? InvtSubID { get; set; }
		public abstract class invtSubID : BqlInt.Field<invtSubID> { }
		#endregion
		#region COGSAcctID
		/// <summary>
		/// The expense <see cref="Account"/> used to record the cost of goods sold for this item when a sales order for it is released.
		/// Applicable only for Stock Items (see <see cref="StkItem"/>).
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.COGSAcctID">COGS Account</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Account.AccountID"/> field.
		/// </value>
		[Account(DisplayName = "COGS Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.cOGSAcctID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.COGSAccount))]
		public virtual Int32? COGSAcctID { get; set; }
		public abstract class cOGSAcctID : BqlInt.Field<cOGSAcctID> { }
		#endregion
		#region COGSSubID
		/// <summary>
		/// The <see cref="Sub">Subaccount</see> used to record the cost of goods sold for this item when a sales order for it is released.
		/// Applicable only for Stock Items (see <see cref="StkItem"/>).
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.COGSSubID">COGS Sub.</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Sub.SubID"/> field.
		/// </value>
		[SubAccount(typeof(cOGSAcctID), DisplayName = "COGS Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.cOGSSubID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.COGSSubaccount))]
		public virtual Int32? COGSSubID { get; set; }
		public abstract class cOGSSubID : BqlInt.Field<cOGSSubID> { }
		#endregion
		#region ExpenseAccrualAcctID
		/// <summary>
		/// The asset <see cref="Account"/> used to keep the Expense Accrual Account, resulting from the transactions with this item.
		/// Applicable only for Non-Stock Items (see <see cref="StkItem"/>).
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.InvtAcctID">Inventory Account</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Account.AccountID"/> field.
		/// </value>
		[PXInt]
		public virtual Int32? ExpenseAccrualAcctID { get; set; }
		public abstract class expenseAccrualAcctID : BqlInt.Field<expenseAccrualAcctID> { }
		#endregion
		#region ExpenseAccrualSubID
		/// <summary>
		/// The <see cref="Sub">Subaccount</see> used to keep the Expense Accrual Account, resulting from the transactions with this item.
		/// Applicable only for Non-Stock Items (see <see cref="StkItem"/>).
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.InvtSubID">Inventory Sub.</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Sub.SubID"/> field.
		/// </value>
		[PXInt]
		public virtual Int32? ExpenseAccrualSubID { get; set; }
		public abstract class expenseAccrualSubID : BqlInt.Field<expenseAccrualSubID> { }
		#endregion
		#region ExpenseAcctID
		/// <summary>
		/// The expense <see cref="Account"/> used to record the cost of goods sold for this item when a sales order for it is released.
		/// Applicable only for Non-Stock Items (see <see cref="StkItem"/>).
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.COGSAcctID">COGS Account</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Account.AccountID"/> field.
		/// </value>
		[PXInt]
		public virtual Int32? ExpenseAcctID { get; set; }
		public abstract class expenseAcctID : BqlInt.Field<expenseAcctID> { }
		#endregion
		#region ExpenseSubID
		/// <summary>
		/// The <see cref="Sub">Subaccount</see> used to record the cost of goods sold for this item when a sales order for it is released.
		/// Applicable only for Non-Stock Items (see <see cref="StkItem"/>).
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.COGSSubID">COGS Sub.</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Sub.SubID"/> field.
		/// </value>
		[PXInt]
		public virtual Int32? ExpenseSubID { get; set; }
		public abstract class expenseSubID : BqlInt.Field<expenseSubID> { }
		#endregion
		#region StdCstRevAcctID
		/// <summary>
		/// The expense <see cref="Account"/> used to record the differences in inventory value of this item estimated
		/// by using the pending standard cost and the currently effective standard cost for the quantities on hand.
		/// Applicable only for Stock Items (see <see cref="StkItem"/>) under Standard <see cref="ValMethod">Valuation Method</see>.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.StdCstRevAcctID">Standard Cost Revaluation Account</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Account.AccountID"/> field.
		/// </value>
		[Account(DisplayName = "Standard Cost Revaluation Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.stdCstRevAcctID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.StandardCostRevaluationAccount))]
		public virtual Int32? StdCstRevAcctID { get; set; }
		public abstract class stdCstRevAcctID : BqlInt.Field<stdCstRevAcctID> { }
		#endregion
		#region StdCstRevSubID
		/// <summary>
		/// The <see cref="Sub">Subaccount</see> used to record the differences in inventory value of this item estimated
		/// by using the pending standard cost and the currently effective standard cost for the quantities on hand.
		/// Applicable only for Stock Items (see <see cref="StkItem"/>) under Standard <see cref="ValMethod">Valuation Method</see>.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.StdCstRevSubID">Standard Cost Revaluation Sub.</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Sub.SubID"/> field.
		/// </value>
		[SubAccount(typeof(stdCstRevAcctID), DisplayName = "Standard Cost Revaluation Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.stdCstRevSubID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.StandardCostRevaluationSubaccount))]
		public virtual Int32? StdCstRevSubID { get; set; }
		public abstract class stdCstRevSubID : BqlInt.Field<stdCstRevSubID> { }
		#endregion
		#region StdCstVarAcctID
		/// <summary>
		/// The expense <see cref="Account"/> used to record the differences between the currently effective standard cost 
		/// and the cost on the inventory receipt of the item.
		/// Applicable only for Stock Items (see <see cref="StkItem"/>) under Standard <see cref="ValMethod">Valuation Method</see>.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.StdCstVarAcctID">Standard Cost Variance Account</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Account.AccountID"/> field.
		/// </value>
		[Account(DisplayName = "Standard Cost Variance Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.stdCstVarAcctID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.StandardCostVarianceAccount))]
		public virtual Int32? StdCstVarAcctID { get; set; }
		public abstract class stdCstVarAcctID : BqlInt.Field<stdCstVarAcctID> { }
		#endregion
		#region StdCstVarSubID
		/// <summary>
		/// The <see cref="Sub">Subaccount</see> used to record the differences between the currently effective standard cost 
		/// and the cost on the inventory receipt of the item.
		/// Applicable only for Stock Items (see <see cref="StkItem"/>) under Standard <see cref="ValMethod">Valuation Method</see>.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.StdCstVarSubID">Standard Cost Variance Sub.</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Sub.SubID"/> field.
		/// </value>
		[SubAccount(typeof(stdCstVarAcctID), DisplayName = "Standard Cost Variance Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.stdCstVarSubID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.StandardCostVarianceSubaccount))]
		public virtual Int32? StdCstVarSubID { get; set; }
		public abstract class stdCstVarSubID : BqlInt.Field<stdCstVarSubID> { }
		#endregion
		#region PPVAcctID
		/// <summary>
		/// The expense <see cref="Account"/> used to record the differences between the extended price on the purchase receipt
		/// and the extended price on the Accounts Payable bill for this item.
		/// Applicable only for Stock Items (see <see cref="StkItem"/>) under any <see cref="ValMethod">Valuation Method</see> except Standard.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.PPVAcctID">Purchase Price Variance Account</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Account.AccountID"/> field.
		/// </value>
		[Account(DisplayName = "Purchase Price Variance Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.pPVAcctID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.PPVAccount))]
		public virtual Int32? PPVAcctID { get; set; }
		public abstract class pPVAcctID : BqlInt.Field<pPVAcctID> { }
		#endregion
		#region PPVSubID
		/// <summary>
		/// The <see cref="Sub">Subaccount</see> used to record the differences between the extended price on the purchase receipt
		/// and the extended price on the Accounts Payable bill for this item.
		/// Applicable only for Stock Items (see <see cref="StkItem"/>) under any <see cref="ValMethod">Valuation Method</see> except Standard.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.PPVSubID">Purchase Price Variance Sub.</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Sub.SubID"/> field.
		/// </value>
		[SubAccount(typeof(pPVAcctID), DisplayName = "Purchase Price Variance Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.pPVSubID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.PPVSubaccount))]
		public virtual Int32? PPVSubID { get; set; }
		public abstract class pPVSubID : BqlInt.Field<pPVSubID> { }
		#endregion
		#region POAccrualAcctID
		/// <summary>
		/// The liability <see cref="Account"/> used to accrue amounts on purchase orders related to this item.
		/// Applicable for all Stock Items (see <see cref="StkItem"/>) and for Non-Stock Items, for which a receipt is required.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.POAccrualAcctID">PO Accrual Account</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Account.AccountID"/> field.
		/// </value>
		[Account(DisplayName = "PO Accrual Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), ControlAccountForModule = ControlAccountModule.PO)]
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.pOAccrualAcctID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.POAccrualAccount))]
		public virtual Int32? POAccrualAcctID { get; set; }
		public abstract class pOAccrualAcctID : BqlInt.Field<pOAccrualAcctID> { }
		#endregion
		#region POAccrualSubID
		/// <summary>
		/// The <see cref="Sub">Subaccount</see> used to accrue amounts on purchase orders related to this item.
		/// Applicable for all Stock Items (see <see cref="StkItem"/>) and for Non-Stock Items, for which a receipt is required.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.POAccrualSubID">PO Accrual Sub.</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Sub.SubID"/> field.
		/// </value>
		[SubAccount(typeof(pOAccrualAcctID), DisplayName = "PO Accrual Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.pOAccrualSubID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.POAccrualSubaccount))]
		public virtual Int32? POAccrualSubID { get; set; }
		public abstract class pOAccrualSubID : BqlInt.Field<pOAccrualSubID> { }
		#endregion
		#region LCVarianceAcctID
		/// <summary>
		/// The expense <see cref="Account"/> used to record differences between the landed cost amounts specified on purchase receipts
		/// and the amounts on inventory receipts.
		/// Applicable only for Stock Items (see <see cref="StkItem"/>).
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.LCVarianceAcctID">Landed Cost Variance Account</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Account.AccountID"/> field.
		/// </value>
		[Account(DisplayName = "Landed Cost Variance Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), AvoidControlAccounts = true)]
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.lCVarianceAcctID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.LandedCostVarianceAccount))]
		public virtual Int32? LCVarianceAcctID { get; set; }
		public abstract class lCVarianceAcctID : BqlInt.Field<lCVarianceAcctID> { }
		#endregion
		#region LCVarianceSubID
		/// <summary>
		/// The <see cref="Sub">Subaccount</see> used to record differences between the landed cost amounts specified on purchase receipts
		/// and the amounts on inventory receipts.
		/// Applicable only for Stock Items (see <see cref="StkItem"/>).
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INPostClass.LCVarianceSubID">Landed Cost Variance Sub.</see> set on the <see cref="PostClassID">Posting Class</see> of the item.
		/// Corresponds to the <see cref="Sub.SubID"/> field.
		/// </value>
		[SubAccount(typeof(lCVarianceAcctID), DisplayName = "Landed Cost Variance Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.lCVarianceSubID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.LandedCostVarianceSubaccount))]
		public virtual Int32? LCVarianceSubID { get; set; }
		public abstract class lCVarianceSubID : BqlInt.Field<lCVarianceSubID> { }
		#endregion
		#region DeferralAcctID
		[Account(DisplayName = "Deferral Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Account.description), ControlAccountForModule = ControlAccountModule.DR)]
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.deferralAcctID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.DeferralAccount))]
		public int? DeferralAcctID { get; set; }
		public abstract class deferralAcctID : BqlInt.Field<deferralAcctID> { }
		#endregion
		#region DeferralSubID
		[SubAccount(typeof(deferralAcctID), DisplayName = "Deferral Sub.", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(Sub.description))]
		[PXDefault(typeof(SelectParentPostClass), SourceField = typeof(INPostClass.deferralSubID), CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.DeferralSubaccount))]
		public int? DeferralSubID { get; set; }
		public abstract class deferralSubID : BqlInt.Field<deferralSubID> { }
		#endregion
		#region LastSiteID
		/// <summary>
		/// Reserved for internal use.
		/// </summary>
		[PXDBInt]
		public virtual Int32? LastSiteID { get; set; }
		public abstract class lastSiteID : BqlInt.Field<lastSiteID> { }
		#endregion
		#region LastStdCost
		/// <summary>
		/// The standard cost assigned to the item before the current standard cost was set.
		/// </summary>
		[PXDBPriceCost]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Last Cost", Enabled = false)]
		public virtual Decimal? LastStdCost { get; set; }
		public abstract class lastStdCost : BqlDecimal.Field<lastStdCost> { }
		#endregion
		#region PendingStdCost
		/// <summary>
		/// The standard cost to be assigned to the item when the costs are updated.
		/// </summary>
		[PXDBPriceCost]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Pending Cost")]
		public virtual Decimal? PendingStdCost { get; set; }
		public abstract class pendingStdCost : BqlDecimal.Field<pendingStdCost> { }
		#endregion
		#region PendingStdCostDate
		/// <summary>
		/// The date when the <see cref="PendingStdCost">Pending Cost</see> becomes effective.
		/// </summary>
		[PXDBDate]
		[PXUIField(DisplayName = "Pending Cost Date")]
		[PXFormula(typeof(AccessInfo.businessDate.FromCurrent.When<pendingStdCost.IsNotEqual<decimal0>>.Else<pendingStdCostDate>))]
		public virtual DateTime? PendingStdCostDate { get; set; }
		public abstract class pendingStdCostDate : BqlDateTime.Field<pendingStdCostDate> { }
		#endregion
		#region StdCost
		/// <summary>
		/// The current standard cost of the item.
		/// </summary>
		[PXDBPriceCost]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Current Cost", Enabled = false)]
		public virtual Decimal? StdCost { get; set; }
		public abstract class stdCost : BqlDecimal.Field<stdCost> { }
		#endregion
		#region StdCostDate
		/// <summary>
		/// The date when the <see cref="StdCost">Current Cost</see> became effective.
		/// </summary>
		[PXDBDate]
		[PXUIField(DisplayName = "Effective Date", Enabled = false)]
		public virtual DateTime? StdCostDate { get; set; }
		public abstract class stdCostDate : BqlDateTime.Field<stdCostDate> { }
		#endregion
		#region BasePrice
		/// <summary>
		/// The price used as the default price, if there are no other prices defined for this item in any price list in the Accounts Receivable module.
		/// </summary>
		[PXDBPriceCost]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Default Price", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual Decimal? BasePrice { get; set; }
		public abstract class basePrice : BqlDecimal.Field<basePrice> { }
		#endregion
		#region BaseWeight
		/// <summary>
		/// The weight of the <see cref="BaseUnit">Base Unit</see> of the item.
		/// </summary>
		/// <value>
		/// Given in the <see cref="CommonSetup.WeightUOM">default Weight Unit of the Inventory module</see>.
		/// </value>
		[PXDBDecimal(6)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? BaseWeight { get; set; }
		public abstract class baseWeight : BqlDecimal.Field<baseWeight> { }
		#endregion
		#region BaseVolume
		/// <summary>
		/// The volume of the item.
		/// </summary>
		/// <value>
		/// Given in the <see cref="CommonSetup.VolumeUOM">default Volume Unit of the Inventory module</see>.
		/// </value>
		[PXDBDecimal(6)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Volume")]
		public virtual Decimal? BaseVolume { get; set; }
		public abstract class baseVolume : BqlDecimal.Field<baseVolume> { }
		#endregion
		#region BaseItemWeight
		/// <summary>
		/// The weight of the <see cref="BaseUnit">Base Unit</see> of the item.
		/// </summary>
		/// <value>
		/// Given in the <see cref="WeightUOM">Weight Unit of this item</see>.
		/// </value>
		[PXDBQuantity(6, typeof(weightUOM), typeof(baseWeight), HandleEmptyKey = true)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Weight")]
		public virtual Decimal? BaseItemWeight { get; set; }
		public abstract class baseItemWeight : BqlDecimal.Field<baseItemWeight> { }
		#endregion
		#region BaseItemVolume
		/// <summary>
		/// The volume of the <see cref="BaseUnit">Base Unit</see> of the item.
		/// </summary>
		/// <value>
		/// Given in the <see cref="VolumeUOM">Volume Unit of this item</see>.
		/// </value>
		[PXDBQuantity(6, typeof(volumeUOM), typeof(baseVolume), HandleEmptyKey = true)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Volume")]
		public virtual Decimal? BaseItemVolume { get; set; }
		public abstract class baseItemVolume : BqlDecimal.Field<baseItemVolume> { }
		#endregion
		#region WeightUOM
		/// <summary>
		/// The <see cref="INUnit">Unit of Measure</see> used for the <see cref="BaseItemWeight">Weight</see> of the item.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="INUnit.FromUnit"/> field.
		/// </value>
		[INUnit(null, typeof(CommonSetup.weightUOM), DisplayName = "Weight UOM")]
		public virtual String WeightUOM { get; set; }
		public abstract class weightUOM : BqlString.Field<weightUOM> { }
		#endregion
		#region VolumeUOM
		/// <summary>
		/// The <see cref="INUnit">Unit of Measure</see> used for the <see cref="BaseItemVolume">Volume</see> of the item.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="INUnit.FromUnit"/> field.
		/// </value>
		[INUnit(null, typeof(CommonSetup.volumeUOM), DisplayName = "Volume UOM")]
		public virtual String VolumeUOM { get; set; }
		public abstract class volumeUOM : BqlString.Field<volumeUOM> { }
		#endregion
		#region PackSeparately
		/// <summary>
		/// When set to <c>true</c>, indicates that the item must be packaged separately from other items.
		/// This field is automatically set to <c>true</c> if <i>By Quantity</i> is selected as the <see cref="PackageOption">PackageOption</see>.
		/// Applicable only for Stock Items (see <see cref="StkItem"/>).
		/// </summary>
		/// <value>
		/// Defaults to <c>false</c>.
		/// </value>
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Pack Separately", Visibility = PXUIVisibility.Visible)]
		public virtual Boolean? PackSeparately { get; set; }
		public abstract class packSeparately : BqlBool.Field<packSeparately> { }
		#endregion
		#region PackageOption
		/// <summary>
		/// The option that governs the system in the process of determining the optimal set of boxes for the item on each sales order.
		/// Applicable only for Stock Items (see <see cref="StkItem"/>).
		/// </summary>
		/// <value>
		/// Allowed values are:
		/// <c>"N"</c> - Manual,
		/// <c>"W"</c> - By Weight (the system will take into account the <see cref="INItemBoxEx.MaxWeight">Max. Weight</see> allowed for each box specififed for the item),
		/// <c>"Q"</c> - By Quantity (the system will take into account the <see cref="INItemBoxEx.MaxQty">Max. Quantity</see> allowed for each box specififed for the item.
		///	With this option items of this kind are always packages separately from other items),
		/// <c>"V"</c> - By Weight and Volume (the system will take into account the <see cref="INItemBoxEx.MaxWeight">Max. Weight</see> and
		/// <see cref="INItemBoxEx.MaxVolume">Max. Volume</see> allowed for each box specififed for the item).
		/// Defaults to Manual (<c>"M"</c>).
		/// </value>
		[PXDBString(1, IsFixed = true)]
		[PXDefault(INPackageOption.Manual)]
		[PXUIField(DisplayName = "Packaging Option")]
		[INPackageOption.List]
		public virtual String PackageOption { get; set; }
		public abstract class packageOption : BqlString.Field<packageOption> { }
		#endregion
		#region PreferredVendorID
		/// <summary>
		/// Preferred (default) <see cref="AP.Vendor">Vendor</see> for purchases of this item. 
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="BAccount.BAccountID"/> field.
		/// </value>
		[AP.VendorNonEmployeeActive(DisplayName = "Preferred Vendor", Required = false, DescriptionField = typeof(AP.Vendor.acctName))]
		public virtual Int32? PreferredVendorID { get; set; }
		public abstract class preferredVendorID : BqlInt.Field<preferredVendorID> { }
		#endregion
		#region PreferredVendorLocationID
		/// <summary>
		/// The <see cref="Location"/> of the <see cref="PreferredVendorID">Preferred (default) Vendor</see>.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="Location.LocationID"/> field.
		/// </value>
		[LocationID(typeof(Where<Location.bAccountID.IsEqual<preferredVendorID.FromCurrent>>), DescriptionField = typeof(Location.descr), DisplayName = "Preferred Location")]
		public virtual Int32? PreferredVendorLocationID { get; set; }
		public abstract class preferredVendorLocationID : BqlInt.Field<preferredVendorLocationID> { }
		#endregion
		#region DefaultSubItemID
		/// <summary>
		/// The default <see cref="INSubItem">Subitem</see> for this item, which is used when there are no subitems
		/// or when specifying subitems is not important.
		/// This field is relevant only if the <see cref="FeaturesSet.SubItem">Inventory Subitems</see> feature is enabled.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="INSubItem.SubItemID"/> field.
		/// </value>
		[IN.SubItem(typeof(inventoryID), DisplayName = "Default Subitem")]
		public virtual Int32? DefaultSubItemID { get; set; }
		public abstract class defaultSubItemID : BqlInt.Field<defaultSubItemID> { }
		#endregion
		#region DefaultSubItemOnEntry
		/// <summary>
		/// When set to <c>true</c>, indicates that the system must set the <see cref="DefaultSubItemID">Default Subitem</see>
		/// for the lines involving this item by default on data entry forms.
		/// This field is relevant only if the <see cref="FeaturesSet.SubItem">Inventory Subitems</see> feature is enabled.
		/// </summary>
		/// <value>
		/// Defaults to <c>false</c>.
		/// </value>
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Use On Entry")]
		public virtual Boolean? DefaultSubItemOnEntry { get; set; }
		public abstract class defaultSubItemOnEntry : BqlBool.Field<defaultSubItemOnEntry> { }
		#endregion
		#region DfltSiteID
		/// <summary>
		/// The default <see cref="INSite">Warehouse</see> used to store the items of this kind.
		/// Applicable only for Stock Items (see <see cref="StkItem"/>) and when the <see cref="FeaturesSet.Warehouse">Warehouses</see> feature is enabled.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="INSite.SiteID"/> field.
		/// Defaults to the <see cref="INItemClass.DfltSiteID">Default Warehouse</see> specified for the <see cref="ItemClassID">Class of the item</see>.
		/// </value>
		[IN.Site(DisplayName = "Default Warehouse", DescriptionField = typeof(INSite.descr))]
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(INItemClass.dfltSiteID), PersistingCheck = PXPersistingCheck.Nothing, CacheGlobal = true)]
		[PXForeignReference(typeof(FK.DefaultSite))]
		public virtual Int32? DfltSiteID { get; set; }
		public abstract class dfltSiteID : BqlInt.Field<dfltSiteID> { }
		#endregion
		#region DfltShipLocationID
		/// <summary>
		/// The <see cref="INLocation">Location of warehouse</see> used by default to issue items of this kind.
		/// Applicable only for Stock Items (see <see cref="StkItem"/>) when the <see cref="FeaturesSet.WarehouseLocation">Warehouse Locations</see> feature is enabled.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="INLocation.LocationID"/> field.
		/// </value>
		[Location(typeof(dfltSiteID), DisplayName = "Default Issue From", KeepEntry = false, ResetEntry = false, DescriptionField = typeof(INLocation.descr))]
		[PXRestrictor(typeof(Where<INLocation.active.IsEqual<True>>), Messages.LocationIsNotActive)]
		public virtual Int32? DfltShipLocationID { get; set; }
		public abstract class dfltShipLocationID : BqlInt.Field<dfltShipLocationID> { }
		#endregion
		#region DfltReceiptLocationID
		/// <summary>
		/// The <see cref="INLocation">Location of warehouse</see> used by default to receive items of this kind.
		/// Applicable only for Stock Items (see <see cref="StkItem"/>) when the <see cref="FeaturesSet.WarehouseLocation">Warehouse Locations</see> feature is enabled.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="INLocation.LocationID"/> field.
		/// </value>
		[Location(typeof(dfltSiteID), DisplayName = "Default Receipt To", KeepEntry = false, ResetEntry = false, DescriptionField = typeof(INLocation.descr))]
		[PXRestrictor(typeof(Where<INLocation.active.IsEqual<True>>), Messages.LocationIsNotActive)]
		public virtual Int32? DfltReceiptLocationID { get; set; }
		public abstract class dfltReceiptLocationID : BqlInt.Field<dfltReceiptLocationID> { }
		#endregion
		#region ProductWorkgroupID
		/// <summary>
		/// The workgroup that is responsible for the item.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="EPCompanyTree.WorkGroupID"/> field.
		/// </value>
		[PXDBInt]
		[PXWorkgroupSelector]
		[PXUIField(DisplayName = "Product Workgroup")]
		public virtual Int32? ProductWorkgroupID { get; set; }
		public abstract class productWorkgroupID : BqlInt.Field<productWorkgroupID> { }
		#endregion
		#region ProductManagerID
		/// <summary>
		/// The <see cref="Contact">product manager</see> responsible for this item.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="Contact.ContactID"/> field.
		/// </value>
		[Owner(typeof(productWorkgroupID), DisplayName = "Product Manager")]
		public virtual int? ProductManagerID { get; set; }
		public abstract class productManagerID : BqlInt.Field<productManagerID> { }
		#endregion
		#region PriceWorkgroupID
		/// <summary>
		/// The workgroup that is responsible for the pricing of this item.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="EPCompanyTree.WorkGroupID"/> field.
		/// </value>
		[PXDBInt]
		[PXWorkgroupSelector]
		[PXUIField(DisplayName = "Price Workgroup")]
		public virtual Int32? PriceWorkgroupID { get; set; }
		public abstract class priceWorkgroupID : BqlInt.Field<priceWorkgroupID> { }
		#endregion
		#region PriceManagerID
		/// <summary>
		/// The <see cref="Contact">manager</see> responsible for the pricing of this item.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="Contact.ContactID"/> field.
		/// </value>
		[Owner(typeof(priceWorkgroupID), DisplayName = "Price Manager")]
		public virtual int? PriceManagerID { get; set; }
		public abstract class priceManagerID : BqlInt.Field<priceManagerID> { }
		#endregion
		#region NegQty
		/// <summary>
		/// An unbound field that, when equal to <c>true</c>, indicates that negative quantities are allowed for this item.
		/// </summary>
		/// <value>
		/// The value of this field is taken from the <see cref="INItemClass.NegQty"/> field of the <see cref="ItemClass">Class</see>, to which the item belongs.
		/// </value>
		[PXBool]
		[PXFormula(typeof(Selector<itemClassID, INItemClass.negQty>))]
		public virtual bool? NegQty { get; set; }
		public abstract class negQty : BqlBool.Field<negQty> { }
		#endregion
		#region LotSerClassID
		/// <summary>
		/// The <see cref="INLotSerClass">lot/serial class</see>, to which the item is assigned.
		/// This field is relevant only if the <see cref="FeaturesSet.LotSerialTracking">Lot/Serial Tracking</see> feature is enabled.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="INLotSerClass.LotSerClassID"/> field.
		/// </value>
		[PXDBString(10, IsUnicode = true)]
		[PXSelector(typeof(INLotSerClass.lotSerClassID), DescriptionField = typeof(INLotSerClass.descr), CacheGlobal = true)]
		[PXUIField(DisplayName = "Lot/Serial Class")]
		public virtual String LotSerClassID { get; set; }
		public abstract class lotSerClassID : BqlString.Field<lotSerClassID> { }
		#endregion
		#region LotSerNumberResult
		/// <summary>
		/// The Lot/Serial Number generated for the item by the system.
		/// The Lot/Serial Numbers are generated by the <see cref="InventoryItemMaint.GenerateLotSerialNumber"/> action of the <see cref="InventoryItemMaint"/> graph
		/// (corresponds to the Stock Items (IN.20.25.00) screen).
		/// </summary>
		[PXString(30, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Result of Generation Lot/Serial Number", Enabled = false)]
		public virtual String LotSerNumberResult { get; set; }
		public abstract class lotSerNumberResult : BqlString.Field<lotSerNumberResult> { }
		#endregion
		#region PostClassID
		/// <summary>
		/// Identifier of the <see cref="INPostClass">Posting Class</see> associated with the item.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INItemClass.PostClassID">Posting Class</see> selected for the <see cref="ItemClassID">item class</see>.
		/// Corresponds to the <see cref="INPostClass.PostClassID"/> field.
		/// </value>
		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Posting Class")]
		public virtual String PostClassID { get; set; }
		public abstract class postClassID : BqlString.Field<postClassID> { }
		#endregion
		#region DeferredCode
		/// <summary>
		/// The deferral code used to perform deferrals on sale of the item.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INItemClass.DeferredCode">Deferral Code</see> selected for the <see cref="ItemClassID">Item Class</see>.
		/// Corresponds to the <see cref="DRDeferredCode.DeferredCodeID"/> field.
		/// </value>
		[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
		[PXUIField(DisplayName = "Deferral Code")]
		[PXSelector(typeof(Search<DRDeferredCode.deferredCodeID>))]
		[PXRestrictor(typeof(Where<DRDeferredCode.active.IsEqual<True>>), DR.Messages.InactiveDeferralCode, typeof(DRDeferredCode.deferredCodeID))]
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(INItemClass.deferredCode), PersistingCheck = PXPersistingCheck.Nothing, CacheGlobal = true)]
		public virtual String DeferredCode { get; set; }
		public abstract class deferredCode : BqlString.Field<deferredCode> { }
		#endregion
		#region DefaultTerm
		[PXDBDecimal(0, MinValue = 0.0, MaxValue = 10000.0)]
		[PXUIField(DisplayName = "Default Term")]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? DefaultTerm { get; set; }
		public abstract class defaultTerm : BqlDecimal.Field<defaultTerm> { }
		#endregion

		#region DefaultTermUOM
		[PXDBString(1, IsFixed = true, IsUnicode = false)]
		[PXUIField(DisplayName = "Default Term UOM")]
		[DRTerms.UOMList]
		[PXDefault(DRTerms.Year)]
		public virtual string DefaultTermUOM { get; set; }
		public abstract class defaultTermUOM : BqlString.Field<defaultTermUOM> { }
		#endregion
		#region PriceClassID
		/// <summary>
		/// The <see cref="INPriceClass">Item Price Class</see> associated with the item.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INItemClass.PriceClassID">Price Class</see> selected for the <see cref="ItemClassID">Item Class</see>.
		/// Corresponds to the <see cref="INPriceClass.PriceClassID"/> field.
		/// </value>
		[PXDBString(10, IsUnicode = true)]
		[PXSelector(typeof(INPriceClass.priceClassID), DescriptionField = typeof(INPriceClass.description))]
		[PXUIField(DisplayName = "Price Class", Visibility = PXUIVisibility.Visible)]
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(INItemClass.priceClassID), PersistingCheck = PXPersistingCheck.Nothing, CacheGlobal = true)]
		public virtual String PriceClassID { get; set; }
		public abstract class priceClassID : BqlString.Field<priceClassID> { }
		#endregion
		#region IsSplitted
		/// <summary>
		/// When set to <c>true</c>, indicates that the system should split the revenue from sale of the item among its components.
		/// </summary>
		/// <value>
		/// Defaults to <c>false</c>.
		/// </value>
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Split into Components")]
		public virtual Boolean? IsSplitted { get; set; }
		public abstract class isSplitted : BqlBool.Field<isSplitted> { }
		#endregion
		#region UseParentSubID
		/// <summary>
		/// When set to <c>true</c>, indicates that the system should use the component subaccounts in the component-associated deferrals.
		/// </summary>
		/// <value>
		/// Defaults to <c>false</c>.
		/// </value>
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Use Component Subaccounts", FieldClass = SubAccountAttribute.DimensionName)]
		public virtual Boolean? UseParentSubID { get; set; }
		public abstract class useParentSubID : BqlBool.Field<useParentSubID> { }
		#endregion
		#region TotalPercentage
		/// <summary>
		/// The total percentage of the item price as split among components.
		/// </summary>
		/// <value>
		/// The value is calculated automatically from the <see cref="INComponent.Percentage">percentages</see>
		/// of the <see cref="INComponent">components</see> associated with the item.
		/// Set to <c>100</c> if the item is not a package.
		/// </value>
		[PXDecimal]
		[PXUIField(DisplayName = "Total Percentage", Enabled = false)]
		public virtual Decimal? TotalPercentage { get; set; }
		public abstract class totalPercentage : BqlDecimal.Field<totalPercentage> { }
		#endregion
		#region KitItem
		/// <summary>
		/// When set to <c>true</c>, indicates that the item is a kit.
		/// Kits are stock or non-stock items that consist of other items and are sold as a whole.
		/// </summary>
		/// <value>
		/// Defaults to <c>false</c>.
		/// </value>
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Is a Kit", Visibility = PXUIVisibility.Visible)]
		public virtual Boolean? KitItem { get; set; }
		public abstract class kitItem : BqlBool.Field<kitItem> { }
		#endregion
		#region MinGrossProfitPct
		/// <summary>
		/// The minimum markup percentage for the item.
		/// See the <see cref="MarkupPct"/> field.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INItemClass.MinGrossProfitPct">Min. Margup %</see> defined for the <see cref="ItemClassID">Item Class</see>.
		/// </value>
		[PXDefault(TypeCode.Decimal, "0.0", typeof(SelectParentItemClass), SourceField = typeof(INItemClass.minGrossProfitPct), CacheGlobal = true)]
		[PXDBDecimal(6, MinValue = 0, MaxValue = 1000)]
		[PXUIField(DisplayName = "Min. Markup %", Visibility = PXUIVisibility.Visible)]
		public virtual Decimal? MinGrossProfitPct { get; set; }
		public abstract class minGrossProfitPct : BqlDecimal.Field<minGrossProfitPct> { }
		#endregion
		#region NonStockReceipt
		/// <summary>
		/// Reserved for internal use.
		/// Indicates whether the item (assumed Non-Stock) requires receipt.
		/// </summary>
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Require Receipt")]
		public virtual Boolean? NonStockReceipt { get; set; }
		public abstract class nonStockReceipt : BqlBool.Field<nonStockReceipt> { }
		#endregion
		#region NonStockReceiptAsService
		public abstract class nonStockReceiptAsService : PX.Data.BQL.BqlBool.Field<nonStockReceiptAsService> { }
		protected Boolean? _NonStockReceiptAsService;

		/// <summary>
		/// Indicates whether the item (assumed Non-Stock) should be receipted as service.
		/// </summary>
		[PXDBBool()]
		[PXDefault(typeof(FeatureInstalled<FeaturesSet.pOReceiptsWithoutInventory>))]
		[PXUIField(DisplayName = "Process Item via Receipt")]
		public virtual Boolean? NonStockReceiptAsService
		{
			get
			{
				return this._NonStockReceiptAsService;
			}
			set
			{
				this._NonStockReceiptAsService = value;
			}
		}
		#endregion
		#region NonStockShip
		/// <summary>
		/// Reserved for internal use.
		/// Indicates whether the item (assumed Non-Stock) requires shipment.
		/// </summary>
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Require Shipment")]
		public virtual Boolean? NonStockShip { get; set; }
		public abstract class nonStockShip : BqlBool.Field<nonStockShip> { }
		#endregion
		#region AccrueCost
		/// <summary>
		/// When set to <c>true</c>, indicates that cost will be processed using expense accrual account.
		/// </summary>
		[PXDBBool]
		[PXUIEnabled(typeof(Where<kitItem.IsNotEqual<True>>))]
		[PXDefault(false, typeof(SelectParentItemClass), SourceField = typeof(INItemClass.accrueCost), CacheGlobal = true)]
		[PXUIField(DisplayName = "Accrue Cost")]
		public virtual Boolean? AccrueCost { get; set; }
		public abstract class accrueCost : BqlBool.Field<accrueCost> { }
		#endregion
		#region CostBasis
		[PXDBString(1, IsFixed = true)]
		[PXUIField(DisplayName = "Cost Based On")]
		[PXUIEnabled(typeof(Where<accrueCost.IsEqual<True>.And<kitItem.IsNotEqual<True>>>))]
		[PXDefault(CostBasisOption.StandardCost)]
		[CostBasisOption.List]
		public virtual String CostBasis { get; set; }
		public abstract class costBasis : BqlString.Field<costBasis> { }
		#endregion
		#region PercentOfSalesPrice
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXDBDecimal(6, MinValue = 0, MaxValue = 1000)]
		[PXUIEnabled(typeof(Where<accrueCost.IsEqual<True>.And<costBasis.IsEqual<CostBasisOption.percentOfSalesPrice>>>))]
		[PXUIField(DisplayName = "Percent of Sales Price", Visibility = PXUIVisibility.Visible)]
		public virtual Decimal? PercentOfSalesPrice { get; set; }
		public abstract class percentOfSalesPrice : BqlDecimal.Field<percentOfSalesPrice> { }
		#endregion
		#region CompletePOLine
		[PXDBString(1, IsFixed = true)]
		[PXDefault]
		[PXFormula(typeof(Switch<Case<Where<itemType.IsIn<INItemTypes.laborItem, INItemTypes.serviceItem, INItemTypes.chargeItem, INItemTypes.expenseItem>>,
			CompletePOLineTypes.amount>,
			CompletePOLineTypes.quantity>))]
		[PXUIField(DisplayName = "Close PO Line")]
		[CompletePOLineTypes.List]
		public virtual String CompletePOLine { get; set; }
		public abstract class completePOLine : BqlString.Field<completePOLine> { }
		#endregion

		#region ABCCodeID
		/// <summary>
		/// The <see cref="INABCCode">ABC code</see>, to which the item is assigned for the purpose of physical inventories.
		/// The field is relevant only for Stock Items (see <see cref="StkItem"/>).
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="INABCCode.ABCCodeID"/> field.
		/// </value>
		[PXDBString(1, IsFixed = true)]
		[PXUIField(DisplayName = "ABC Code")]
		[PXSelector(typeof(INABCCode.aBCCodeID), DescriptionField = typeof(INABCCode.descr))]
		public virtual String ABCCodeID { get; set; }
		public abstract class aBCCodeID : BqlString.Field<aBCCodeID> { }
		#endregion
		#region ABCCodeIsFixed
		/// <summary>
		/// When set to <c>true</c>, indicates that the system must not change the <see cref="ABCCodeID">ABC Code</see>
		/// assigned to the item when ABC code assignments are updated.
		/// The field is relevant only for Stock Items (see <see cref="StkItem"/>).
		/// </summary>
		/// <value>
		/// Defaults to <c>false</c>.
		/// </value>
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Fixed ABC Code")]
		public virtual Boolean? ABCCodeIsFixed { get; set; }
		public abstract class aBCCodeIsFixed : BqlBool.Field<aBCCodeIsFixed> { }
		#endregion
		#region MovementClassID
		/// <summary>
		/// The <see cref="INMovementClass">Movement Class</see>, to which the item is assigned for the purpose of physical inventories.
		/// The field is relevant only for Stock Items (see <see cref="StkItem"/>).
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="INMovementClass.MovementClassID"/> field.
		/// </value>
		[PXDBString(1)]
		[PXUIField(DisplayName = "Movement Class")]
		[PXSelector(typeof(INMovementClass.movementClassID), DescriptionField = typeof(INMovementClass.descr))]
		public virtual String MovementClassID { get; set; }
		public abstract class movementClassID : BqlString.Field<movementClassID> { }
		#endregion
		#region MovementClassIsFixed
		/// <summary>
		/// When set to <c>true</c>, indicates that the system must not change the <see cref="MovementClassID">Movement Class</see>
		/// assigned to the item when movement class assignments are updated.
		/// The field is relevant only for Stock Items (see <see cref="StkItem"/>).
		/// </summary>
		/// <value>
		/// Defaults to <c>false</c>.
		/// </value>
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Fixed Movement Class")]
		public virtual Boolean? MovementClassIsFixed { get; set; }
		public abstract class movementClassIsFixed : BqlBool.Field<movementClassIsFixed> { }
		#endregion

		#region MarkupPct
		/// <summary>
		/// The percentage that is added to the item cost to get the selling price for it.
		/// </summary>
		/// <value>
		/// Defaults to the <see cref="INItemClass.MarkupPct">Markup %</see> specified for the <see cref="ItemClassID">Item Class</see>.
		/// </value>
		[PXDBDecimal(6, MinValue = 0, MaxValue = 1000)]
		[PXDefault(TypeCode.Decimal, "0.0", typeof(SelectParentItemClass), SourceField = typeof(INItemClass.markupPct), CacheGlobal = true)]
		[PXUIField(DisplayName = "Markup %")]
		public virtual Decimal? MarkupPct { get; set; }
		public abstract class markupPct : BqlDecimal.Field<markupPct> { }
		#endregion
		#region RecPrice
		/// <summary>
		/// The manufacturer's suggested retail price of the item.
		/// </summary>
		[PXDBPriceCost]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "MSRP")]
		public virtual Decimal? RecPrice { get; set; }
		public abstract class recPrice : BqlDecimal.Field<recPrice> { }
		#endregion
		#region ImageUrl
		/// <summary>
		/// The URL of the image associated with the item.
		/// </summary>
		[PXDBString(255)]
		[PXUIField(DisplayName = "Image")]
		public virtual String ImageUrl { get; set; }
		public abstract class imageUrl : BqlString.Field<imageUrl> { }
		#endregion

		#region HSTariffCode
		[PXDBString(30, IsUnicode = true)]
		[PXUIField(DisplayName = "Tariff Code")]
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(INItemClass.hSTariffCode), PersistingCheck = PXPersistingCheck.Nothing, CacheGlobal = true)]
		[PXFormula(typeof(Default<itemClassID>))]
		public virtual string HSTariffCode { get; set; }
		public abstract class hSTariffCode : BqlString.Field<hSTariffCode> { }
		#endregion

		#region UndershipThreshold
		[PXDBDecimal(2, MinValue = 0.0, MaxValue = 100.0)]
		[PXDefault(TypeCode.Decimal, "100.0", typeof(SelectParentItemClass), SourceField = typeof(INItemClass.undershipThreshold), CacheGlobal = true)]
		[PXUIField(DisplayName = "Undership Threshold (%)")]
		public virtual decimal? UndershipThreshold { get; set; }
		public abstract class undershipThreshold : BqlDecimal.Field<undershipThreshold> { }
		#endregion
		#region OvershipThreshold
		[PXDBDecimal(2, MinValue = 100.0, MaxValue = 999.0)]
		[PXDefault(TypeCode.Decimal, "100.0", typeof(SelectParentItemClass), SourceField = typeof(INItemClass.overshipThreshold), CacheGlobal = true)]
		[PXUIField(DisplayName = "Overship Threshold (%)")]
		public virtual decimal? OvershipThreshold { get; set; }
		public abstract class overshipThreshold : BqlDecimal.Field<overshipThreshold> { }
		#endregion

		#region CountryOfOrigin
		[PXDBString(100, IsUnicode = true)]
		[PXUIField(DisplayName = "Country Of Origin")]
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(INItemClass.countryOfOrigin), PersistingCheck = PXPersistingCheck.Nothing, CacheGlobal = true)]
		[PXFormula(typeof(Default<itemClassID>))]
		[Country]
		public virtual string CountryOfOrigin { get; set; }
		public abstract class countryOfOrigin : BqlString.Field<countryOfOrigin> { }
		#endregion

		#region NoteID
		/// <summary>
		/// Identifier of the <see cref="PX.Data.Note">Note</see> object, associated with the item.
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="PX.Data.Note.NoteID">Note.NoteID</see> field. 
		/// </value>
		[PXSearchable(SM.SearchCategory.IN, "{0}: {1}", new Type[] { typeof(itemType), typeof(inventoryCD) },
			new Type[] { typeof(descr) },
			NumberFields = new Type[] { typeof(inventoryCD) },
			Line1Format = "{0}{1}{2}", Line1Fields = new Type[] { typeof(INItemClass.itemClassCD), typeof(INItemClass.descr), typeof(baseUnit) },
			Line2Format = "{0}", Line2Fields = new Type[] { typeof(descr) },
			WhereConstraint = typeof(Where<itemStatus.FromCurrent.IsNotEqual<InventoryItemStatus.unknown>>)
		)]
		[PXNote(PopupTextEnabled = true)]
		public virtual Guid? NoteID { get; set; }
		public abstract class noteID : BqlGuid.Field<noteID> { }
		#endregion

		#region Audit Fields
		#region CreatedByID
		[PXDBCreatedByID]
		public virtual Guid? CreatedByID { get; set; }
		public abstract class createdByID : BqlGuid.Field<createdByID> { }
		#endregion
		#region CreatedByScreenID
		[PXDBCreatedByScreenID]
		public virtual String CreatedByScreenID { get; set; }
		public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }
		#endregion
		#region CreatedDateTime
		[PXDBCreatedDateTime]
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.CreatedDateTime, Enabled = false, IsReadOnly = true)]
		public virtual DateTime? CreatedDateTime { get; set; }
		public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }
		#endregion
		#region LastModifiedByID
		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID { get; set; }
		public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
		#endregion
		#region LastModifiedByScreenID
		[PXDBLastModifiedByScreenID]
		public virtual String LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }
		#endregion
		#region LastModifiedDateTime
		[PXDBLastModifiedDateTime]
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.LastModifiedDateTime, Enabled = false, IsReadOnly = true)]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }
		#endregion
		#region tstamp
		[PXDBTimestamp]
		public virtual Byte[] tstamp { get; set; }
		public abstract class Tstamp : BqlByteArray.Field<Tstamp> { }
		#endregion
		#endregion

		#region GroupMask
		/// <summary>
		/// The group mask showing which <see cref="PX.SM.RelationGroup">restriction groups</see> the item belongs to.
		/// </summary>
		[PXDBGroupMask]
		public virtual Byte[] GroupMask { get; set; }
		public abstract class groupMask : BqlByteArray.Field<groupMask> { }
		#endregion

		#region CycleID
		/// <summary>
		/// The <see cref="INPICycle">Physical Inventory Cycle</see> assigned to the item.
		/// The cycle defines how often the physical inventory counts will be performed for the item.
		/// The field is relevant only for Stock Items (see <see cref="StkItem"/>).
		/// </summary>
		/// <value>
		/// Corresponds to the <see cref="INPICycle.CycleID"/> field.
		/// </value>
		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "PI Cycle")]
		[PXSelector(typeof(INPICycle.cycleID), DescriptionField = typeof(INPICycle.descr))]
		public virtual String CycleID { get; set; }
		public abstract class cycleID : BqlString.Field<cycleID> { }
		#endregion

		#region Attributes
		/// <summary>
		/// Reserved for internal use.
		/// Provides the values of attributes associated with the item.
		/// For more information see the <see cref="CSAnswers"/> class.
		/// </summary>
		[CRAttributesField(typeof(parentItemClassID))]
		public virtual string[] Attributes { get; set; }
		public abstract class attributes : BqlAttributes.Field<attributes> { }

		/// <summary>
		/// Reserved for internal use.
		/// <see cref="IAttributeSupport"/> implementation. The record class ID for attributes retrieval.
		/// </summary>
		public virtual int? ClassID => ItemClassID;
		#endregion
		#region Included
		/// <summary>
		/// An unbound field used in the User Interface to include the item into a <see cref="PX.SM.RelationGroup">restriction group</see>.
		/// </summary>
		[PXBool]
		[PXUIField(DisplayName = "Included")]
		[PXUnboundDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual bool? Included { get; set; }
		public abstract class included : BqlBool.Field<included> { }
		#endregion

		#region Body
		/// <summary>
		/// Rich text description of the item.
		/// </summary>
		[PXDBLocalizableString(IsUnicode = true)]
		[PXUIField(DisplayName = "Content")]
		public virtual string Body { get; set; }
		public abstract class body : BqlString.Field<body> { }
		#endregion

		#region Matrix

		#region IsTemplate
		/// <summary>
		/// When set to <c>true</c>, indicates that the item is a template for other matrix items.
		/// </summary>
		/// <value>
		/// Defaults to <c>false</c>.
		/// </value>
		[PXDBBool]
		[PXDefault(false)]
		public virtual bool? IsTemplate { get; set; }
		public abstract class isTemplate : Data.BQL.BqlBool.Field<isTemplate> { }
		#endregion
		#region TemplateItemID
		/// <summary>
		/// References to parent Inventory Item, its database identifier, if this item was created from template.
		/// </summary>
		[PXUIField(DisplayName = "Template Item", FieldClass = nameof(FeaturesSet.MatrixItem), Enabled = false)]
		[TemplateInventory]
		[PXForeignReference(typeof(Field<templateItemID>.IsRelatedTo<inventoryID>))]
		public virtual int? TemplateItemID { get; set; }
		public abstract class templateItemID : BqlInt.Field<templateItemID> { }
		#endregion
		#region DefaultRowMatrixAttributeID
		/// <summary>
		/// References to Attribute which will be put as Row Attribute in Inventory Matrix by default.
		/// </summary>
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Default Row Attribute ID", FieldClass = nameof(FeaturesSet.MatrixItem))]
		[MatrixAttributeSelector(typeof(
			Search2<CSAttribute.attributeID,
			InnerJoin<CSAttributeGroup, On<CSAttributeGroup.attributeID.IsEqual<CSAttribute.attributeID>>>,
			Where<
				CSAttributeGroup.entityClassID.IsEqual<Use<parentItemClassID.FromCurrent>.AsString>.
				And<CSAttributeGroup.entityType.IsEqual<Constants.DACName<InventoryItem>>>.
				And<CSAttributeGroup.attributeCategory.IsEqual<CSAttributeGroup.attributeCategory.variant>>.
				And<
					CSAttributeGroup.attributeID.IsNotEqual<defaultColumnMatrixAttributeID.FromCurrent.NoDefault>.
					Or<defaultColumnMatrixAttributeID.FromCurrent.NoDefault.IsNull>>>>),
			typeof(defaultColumnMatrixAttributeID), false, typeof(CSAttributeGroup.attributeID),
			DescriptionField = typeof(CSAttributeGroup.description))]
		[PXRestrictor(typeof(Where<CSAttributeGroup.isActive, Equal<True>>), Messages.AttributeIsInactive, typeof(CSAttributeGroup.attributeID))]
		public virtual string DefaultRowMatrixAttributeID { get; set; }
		public abstract class defaultRowMatrixAttributeID : BqlString.Field<defaultRowMatrixAttributeID> { }
		#endregion
		#region DefaultColumnMatrixAttributeID
		/// <summary>
		/// References to Attribute which will be put as Column Attribute in Inventory Matrix by default.
		/// </summary>
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Default Column Attribute ID", FieldClass = nameof(FeaturesSet.MatrixItem))]
		[MatrixAttributeSelector(typeof(
			Search2<CSAttribute.attributeID,
			InnerJoin<CSAttributeGroup, On<CSAttributeGroup.attributeID.IsEqual<CSAttribute.attributeID>>>,
			Where<
				CSAttributeGroup.entityClassID.IsEqual<Use<parentItemClassID.FromCurrent>.AsString>.
				And<CSAttributeGroup.entityType.IsEqual<Constants.DACName<InventoryItem>>>.
				And<CSAttributeGroup.attributeCategory.IsEqual<CSAttributeGroup.attributeCategory.variant>>.
				And<
					CSAttributeGroup.attributeID.IsNotEqual<defaultRowMatrixAttributeID.FromCurrent.NoDefault>.
					Or<defaultRowMatrixAttributeID.FromCurrent.NoDefault.IsNull>>>>),
			typeof(defaultRowMatrixAttributeID), false, typeof(CSAttributeGroup.attributeID),
			DescriptionField = typeof(CSAttributeGroup.description))]
		[PXRestrictor(typeof(Where<CSAttributeGroup.isActive, Equal<True>>), Messages.AttributeIsInactive, typeof(CSAttributeGroup.attributeID))]
		public virtual string DefaultColumnMatrixAttributeID { get; set; }
		public abstract class defaultColumnMatrixAttributeID : BqlString.Field<defaultColumnMatrixAttributeID> { }
		#endregion
		#region GenerationRuleCntr
		[PXDBInt]
		[PXDefault(0)]
		public virtual int? GenerationRuleCntr { get; set; }
		public abstract class generationRuleCntr : BqlInt.Field<generationRuleCntr> { }
		#endregion
		#region HasChild
		/// <summary>
		/// The flag is true if there is Inventory Item which has TemplateItemId equals current InventoryID value.
		/// </summary>
		[PXBool]
		public virtual bool? HasChild { get; set; }
		public abstract class hasChild : BqlString.Field<hasChild> { }
		#endregion
		#region AttributeDescriptionGroupID
		/// <summary>
		/// References to parent Group <see cref="INAttributeDescriptionGroup.GroupID"/>
		/// </summary>
		[PXDBInt]
		public virtual int? AttributeDescriptionGroupID { get; set; }
		public abstract class attributeDescriptionGroupID : BqlInt.Field<attributeDescriptionGroupID> { }
		#endregion
		#region ColumnAttributeValue
		/// <summary>
		/// Value of column matrix attribute of template item.
		/// </summary>
		[PXAttributeValue]
		public virtual string ColumnAttributeValue { get; set; }
		public abstract class columnAttributeValue : BqlString.Field<columnAttributeValue> { }
		#endregion
		#region RowAttributeValue
		/// <summary>
		/// Value of row matrix attribute of template item.
		/// </summary>
		[PXAttributeValue]
		public virtual string RowAttributeValue { get; set; }
		public abstract class rowAttributeValue : BqlString.Field<rowAttributeValue> { }
		#endregion
		#region SampleID
		/// <summary>
		/// Contains Inventory ID Example <see cref="Matrix.DAC.Projections.IDGenerationRule" />
		/// </summary>
		[PXString]
		public virtual string SampleID { get; set; }
		public abstract class sampleID : BqlString.Field<sampleID> { }
		#endregion
		#region SampleDescription
		/// <summary>
		/// Contains Inventory Description Example <see cref="Matrix.DAC.Projections.DescriptionGenerationRule" />
		/// </summary>
		[PXString]
		public virtual string SampleDescription { get; set; }
		public abstract class sampleDescription : BqlString.Field<sampleDescription> { }
		#endregion
		#region UpdateOnlySelected
		/// <summary>
		/// If true, only items selected in the Matrix Items list will be updated with the template changes on Update Matrix Items action.
		/// </summary>
		[PXBool]
		[PXUIField(DisplayName = "Update Only Selected Items with Template Changes")]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual bool? UpdateOnlySelected { get; set; }
		public abstract class updateOnlySelected : BqlBool.Field<updateOnlySelected> { }
		#endregion
		#endregion Matrix

		#region DiscAcctID
		[Obsolete("The field is preserved only for the support of the Default Endpoints.")]
		public abstract class discAcctID : BqlInt.Field<discAcctID> { }
		[PXInt]
		[Obsolete("The field is preserved only for the support of the Default Endpoints.")]
		public virtual int? DiscAcctID
		{
			get => null;
			set { }
		}
		#endregion
		#region DiscSubID
		[Obsolete("The field is preserved only for the support of the Default Endpoints.")]
		public abstract class discSubID : BqlInt.Field<discSubID> { }
		[PXInt]
		[Obsolete("The field is preserved only for the support of the Default Endpoints.")]
		public virtual int? DiscSubID
		{
			get => null;
			set { }
		}
		#endregion

		#region Visibility
		[PXDBString(1, IsUnicode = true)]
		[PXUIField(DisplayName = "Visibility", Visible = false)]
		[PXDefault("X")]
		public virtual string Visibility { get; set; }
		public abstract class visibility : BqlString.Field<visibility> { }
		#endregion
		#region Availability
		[PXDBString(1, IsUnicode = true)]
		[PXUIField(DisplayName = "Availability", Visible = false)]
		[PXDefault("X")]
		public virtual string Availability { get; set; }
		public abstract class availability : BqlString.Field<availability> { }
		#endregion
		#region NotAvailMode
		[PXDBString(1, IsUnicode = true)]
		[PXUIField(DisplayName = "When Qty Unavailable", Visible = false)]
		[PXDefault("X")]
		public virtual string NotAvailMode { get; set; }
		public abstract class notAvailMode : BqlString.Field<notAvailMode> { }
		#endregion
		#region ExportToExternal
		public abstract class exportToExternal : IBqlField { }
		protected bool? _ExportToExternal;
		[PXDBBool()]
		[PXUIField(DisplayName = "Export To External System")]
		[PXUIEnabled(typeof(Where<Current<InventoryItem.templateItemID>, Equal<Null>>))]
		[PXDefault(true, typeof(SelectParentItemClass), SourceField = typeof(INItemClass.exportToExternal), CacheGlobal = true)]
		public virtual Boolean? ExportToExternal {
			get
			{
				return this._ExportToExternal;
			}
			set
			{
				this._ExportToExternal = value;
			}
		}
		#endregion
	}

	public class InventoryItemStatus
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				Pair(Active, Messages.Active),
				Pair(NoSales, Messages.NoSales),
				Pair(NoPurchases, Messages.NoPurchases),
				Pair(NoRequest, Messages.NoRequest),
				Pair(Inactive, Messages.Inactive),
				Pair(MarkedForDeletion, Messages.ToDelete))
			{ }
		}

		public class SubItemListAttribute : PXStringListAttribute
		{
			public SubItemListAttribute() : base(
				Pair(Active, Messages.Active),
				Pair(NoSales, Messages.NoSales),
				Pair(NoPurchases, Messages.NoPurchases),
				Pair(NoRequest, Messages.NoRequest),
				Pair(Inactive, Messages.Inactive))
			{ }
		}

		public const string Active = "AC";
		public const string NoSales = "NS";
		public const string NoPurchases = "NP";
		public const string NoRequest = "NR";
		public const string Inactive = "IN";
		public const string MarkedForDeletion = "DE";
		public const string Unknown = "XX";

		public class active : BqlString.Constant<active> { public active() : base(Active) { } }
		public class noSales : BqlString.Constant<noSales> { public noSales() : base(NoSales) { } }
		public class noPurchases : BqlString.Constant<noPurchases> { public noPurchases() : base(NoPurchases) { } }
		public class noRequest : BqlString.Constant<noRequest> { public noRequest() : base(NoRequest) { } }
		public class inactive : BqlString.Constant<inactive> { public inactive() : base(Inactive) { } }
		public class markedForDeletion : BqlString.Constant<markedForDeletion> { public markedForDeletion() : base(MarkedForDeletion) { } }
		public class unknown : BqlString.Constant<unknown> { public unknown() : base(Unknown) { } }
	}

	public class CompletePOLineTypes
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				Pair(Amount, Messages.ByAmount),
				Pair(Quantity, Messages.ByQuantity))
			{ }
		}

		public const string Amount = "A";
		public const string Quantity = "Q";

		public class amount : BqlString.Constant<amount> { public amount() : base(Amount) { } }
		public class quantity : BqlString.Constant<quantity> { public quantity() : base(Quantity) { } }
	}

	#region Attributes
	public class INItemTypes
	{
		public class CustomListAttribute : PXStringListAttribute
		{
			public string[] AllowedValues => _AllowedValues;
			public string[] AllowedLabels => _AllowedLabels;

			public CustomListAttribute(string[] AllowedValues, string[] AllowedLabels) : base(AllowedValues, AllowedLabels) { }
			public CustomListAttribute(params Tuple<string, string>[] valuesToLabels) : base(valuesToLabels) { }
		}

		public class StockListAttribute : CustomListAttribute
		{
			public StockListAttribute() : base(
				Pair(FinishedGood, Messages.FinishedGood),
				Pair(Component, Messages.Component),
				Pair(SubAssembly, Messages.SubAssembly))
			{ }
		}

		public class NonStockListAttribute : CustomListAttribute
		{
			public NonStockListAttribute() : base(
				Pair(NonStockItem, Messages.NonStockItem),
				Pair(LaborItem, Messages.LaborItem),
				Pair(ServiceItem, Messages.ServiceItem),
				Pair(ChargeItem, Messages.ChargeItem),
				Pair(ExpenseItem, Messages.ExpenseItem))
			{ }
		}

		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				Pair(FinishedGood, Messages.FinishedGood),
				Pair(Component, Messages.Component),
				Pair(SubAssembly, Messages.SubAssembly),
				Pair(NonStockItem, Messages.NonStockItem),
				Pair(LaborItem, Messages.LaborItem),
				Pair(ServiceItem, Messages.ServiceItem),
				Pair(ChargeItem, Messages.ChargeItem),
				Pair(ExpenseItem, Messages.ExpenseItem))
			{ }
		}

		public const string NonStockItem = "N";
		public const string LaborItem = "L";
		public const string ServiceItem = "S";
		public const string ChargeItem = "C";
		public const string ExpenseItem = "E";

		public const string FinishedGood = "F";
		public const string Component = "M";
		public const string SubAssembly = "A";

		public class nonStockItem : BqlString.Constant<nonStockItem> { public nonStockItem() : base(NonStockItem) { } }
		public class laborItem : BqlString.Constant<laborItem> { public laborItem() : base(LaborItem) { } }
		public class serviceItem : BqlString.Constant<serviceItem> { public serviceItem() : base(ServiceItem) { } }
		public class chargeItem : BqlString.Constant<chargeItem> { public chargeItem() : base(ChargeItem) { } }
		public class expenseItem : BqlString.Constant<expenseItem> { public expenseItem() : base(ExpenseItem) { } }

		public class finishedGood : BqlString.Constant<finishedGood> { public finishedGood() : base(FinishedGood) { } }
		public class component : BqlString.Constant<component> { public component() : base(Component) { } }
		public class subAssembly : BqlString.Constant<subAssembly> { public subAssembly() : base(SubAssembly) { } }
	}

	public class INValMethod
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				Pair(Standard, Messages.Standard),
				Pair(Average, Messages.Average),
				Pair(FIFO, Messages.FIFO),
				Pair(Specific, Messages.Specific))
			{ }
		}

		public const string Standard = "T";
		public const string Average = "A";
		public const string FIFO = "F";
		public const string Specific = "S";

		public class standard : BqlString.Constant<standard> { public standard() : base(Standard) { } }
		public class average : BqlString.Constant<average> { public average() : base(Average) { } }
		public class fIFO : BqlString.Constant<fIFO> { public fIFO() : base(FIFO) { } }
		public class specific : BqlString.Constant<specific> { public specific() : base(Specific) { } }
	}

	public class INItemStatus
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				Pair(Active, Messages.Active),
				Pair(NoSales, Messages.NoSales),
				Pair(NoPurchases, Messages.NoPurchases),
				Pair(Inactive, Messages.Inactive),
				Pair(ToDelete, Messages.ToDelete))
			{ }
		}

		public const string Active = "AC";
		public const string Inactive = "IN";
		public const string NoSales = "NS";
		public const string NoPurchases = "NP";
		public const string ToDelete = "DE";

		public class active : BqlString.Constant<active> { public active() : base(Active) { } }
		public class inactive : BqlString.Constant<inactive> { public inactive() : base(Inactive) { } }
		public class noSales : BqlString.Constant<noSales> { public noSales() : base(NoSales) { } }
		public class noPurchases : BqlString.Constant<noPurchases> { public noPurchases() : base(NoPurchases) { } }
		public class toDelete : BqlString.Constant<toDelete> { public toDelete() : base(ToDelete) { } }
	}

	public class INPackageOption
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				Pair(Manual, Messages.Manual),
				Pair(Weight, Messages.Weight),
				Pair(Quantity, Messages.Quantity),
				Pair(WeightAndVolume, Messages.WeightAndVolume))
			{ }
		}

		public const string Manual = "N";
		public const string Weight = "W";
		public const string Quantity = "Q";
		public const string WeightAndVolume = "V";

		public class manual : BqlString.Constant<manual> { public manual() : base(Manual) { } }
		public class weight : BqlString.Constant<weight> { public weight() : base(Weight) { } }
		public class quantity : BqlString.Constant<quantity> { public quantity() : base(Quantity) { } }
		public class weightAndVolume : BqlString.Constant<weightAndVolume> { public weightAndVolume() : base(WeightAndVolume) { } }
	}

	public class CostBasisOption
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				Pair(StandardCost, Messages.StandardCost),
				Pair(PriceMarkupPercent, Messages.PriceMarkupPercent),
				Pair(PercentOfSalesPrice, Messages.PercentOfSalesPrice))
			{ }
		}

		public const string StandardCost = "S";
		public const string PriceMarkupPercent = "M";
		public const string PercentOfSalesPrice = "P";
		public const string UndefinedCostBasis = "U";

		public class standardCost : BqlString.Constant<standardCost> { public standardCost() : base(StandardCost) { } }
		public class priceMarkupPercent : BqlString.Constant<priceMarkupPercent> { public priceMarkupPercent() : base(PriceMarkupPercent) { } }
		public class percentOfSalesPrice : BqlString.Constant<percentOfSalesPrice> { public percentOfSalesPrice() : base(PercentOfSalesPrice) { } }
		public class undefinedCostBasis : BqlString.Constant<undefinedCostBasis> { public undefinedCostBasis() : base(UndefinedCostBasis) { } }
	}
	#endregion
}