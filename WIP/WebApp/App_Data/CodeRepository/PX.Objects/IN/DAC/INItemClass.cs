using System;

using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;

using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.DR;
using PX.Objects.TX;
using PX.TM;

// TODO: move to the namespace-level once AC-183872 is fixed
using SelectParentItemClass = PX.Data.BQL.Fluent.SelectFrom<PX.Objects.IN.INItemClass>.Where<PX.Objects.IN.INItemClass.itemClassID.IsEqual<PX.Objects.IN.INItemClass.parentItemClassID.FromCurrent>>;

namespace PX.Objects.IN
{
	[PXPrimaryGraph(typeof(INItemClassMaint))]
	[PXCacheName(Messages.ItemClass, PXDacType.Catalogue, CacheGlobal = true)]
	public class INItemClass : IBqlTable, PX.SM.IIncludable
	{
		public const string Dimension = "INITEMCLASS";
		public class dimension : BqlString.Constant<dimension> { public dimension() : base(Dimension) { } }

		#region Keys
		public class PK : PrimaryKeyOf<INItemClass>.By<itemClassID>
		{
			public static INItemClass Find(PXGraph graph, int? itemClassID) => FindBy(graph, itemClassID);
		}
		public static class FK
		{
			public class DefaultSite : INSite.PK.ForeignKeyOf<INItemClass>.By<dfltSiteID> { }
			public class AvailabilityScheme : INAvailabilityScheme.PK.ForeignKeyOf<INItemClass>.By<availabilitySchemeID> { }
			public class PostClass : INPostClass.PK.ForeignKeyOf<INItemClass>.By<postClassID> { }
			public class PriceClass : INPriceClass.PK.ForeignKeyOf<INItemClass>.By<priceClassID> { }
			public class LotSerialClass : INLotSerClass.PK.ForeignKeyOf<INItemClass>.By<lotSerClassID> { }
			public class TaxCategory : TX.TaxCategory.PK.ForeignKeyOf<INItemClass>.By<taxCategoryID> { }
			public class DeferredCode : DRDeferredCode.PK.ForeignKeyOf<INItemClass>.By<deferredCode> { }
			public class PriceWorkgroup : EPCompanyTree.PK.ForeignKeyOf<INItemClass>.By<priceWorkgroupID> { }
			public class PriceManager : CR.Standalone.EPEmployee.PK.ForeignKeyOf<INItemClass>.By<priceManagerID> { }
			public class ContryOfOrigin : Country.PK.ForeignKeyOf<INItemClass>.By<countryOfOrigin> { }
			//todo public class BaseUnitOfMeasure : INUnit.UK.ByGlobal.ForeignKeyOf<INItemClass>.By<baseUnit> { }
			//todo public class SalesUnitOfMeasure : INUnit.UK.ByGlobal.ForeignKeyOf<INItemClass>.By<salesUnit> { }
			//todo public class PurchaseUnitOfMeasure : INUnit.UK.ByGlobal.ForeignKeyOf<INItemClass>.By<purchaseUnit> { }
		}
		#endregion
		#region ItemClassID
		[PXDBIdentity]
		[PXUIField(Visible = false, Visibility = PXUIVisibility.Invisible)]
		public virtual int? ItemClassID { get; set; }
		public abstract class itemClassID : BqlInt.Field<itemClassID> { }
		#endregion
		#region ItemClassCD
		[PXDefault]
		[PXDBString(30, IsUnicode = true, IsKey = true, InputMask = "")]
		[PXUIField(DisplayName = "Class ID", Visibility = PXUIVisibility.SelectorVisible)]
		[PXDimensionSelector(Dimension, typeof(SearchFor<itemClassCD>.Where<stkItem.IsEqual<False>.Or<stkItem.IsEqual<True>.And<FeatureInstalled<FeaturesSet.distributionModule>>>>), DescriptionField = typeof(descr))]
		[PX.Data.EP.PXFieldDescription]
		public virtual String ItemClassCD { get; set; }
		public abstract class itemClassCD : BqlString.Field<itemClassCD> { }
		#endregion
		#region Descr
		[PXDBLocalizableString(Common.Constants.TranDescLength, IsUnicode = true)]
		[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
		[PX.Data.EP.PXFieldDescription]
		public virtual String Descr { get; set; }
		public abstract class descr : BqlString.Field<descr> { }
		#endregion
		#region StkItem
		[PXDBBool]
		[PXDefault]
		[PXUIField(DisplayName = "Stock Item")]
		public virtual Boolean? StkItem { get; set; }
		public abstract class stkItem : BqlBool.Field<stkItem> { }
		#endregion
		#region ParentItemClassID
		/// <summary>
		/// The field is used to populate standard settings of <see cref="INItemClass">Item Class</see> from it's parent or default one.
		/// </summary>
		[PXInt]
		public virtual int? ParentItemClassID { get; set; }
		public abstract class parentItemClassID : BqlInt.Field<parentItemClassID> { }
		#endregion
		#region NegQty
		[PXDBBool]
		[PXDefault(false, typeof(SelectParentItemClass), SourceField = typeof(negQty), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Allow Negative Quantity")]
		public virtual Boolean? NegQty { get; set; }
		public abstract class negQty : BqlBool.Field<negQty> { }
		#endregion
		#region AvailabilitySchemeID
		[PXDBString(10, IsUnicode = true)]
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(availabilitySchemeID), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Availability Calculation Rule")]
		[PXSelector(typeof(INAvailabilityScheme.availabilitySchemeID), DescriptionField = typeof(INAvailabilityScheme.description))]
		public virtual string AvailabilitySchemeID { get; set; }
		public abstract class availabilitySchemeID : BqlString.Field<availabilitySchemeID> { }
		#endregion
		#region ValMethod
		[PXDBString(1, IsFixed = true)]
		[PXDefault(INValMethod.Average, typeof(SelectParentItemClass), SourceField = typeof(valMethod), PersistingCheck = PXPersistingCheck.Nothing)]
		[INValMethod.List]
		[PXUIEnabled(typeof(stkItem))]
		[PXFormula(typeof(IIf<Where<stkItem.IsNotEqual<True>>, INValMethod.standard, valMethod>))]
		[PXUIField(DisplayName = "Valuation Method")]
		public virtual String ValMethod { get; set; }
		public abstract class valMethod : BqlString.Field<valMethod> { }
		#endregion
		#region BaseUnit
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(baseUnit))]
		[INSyncUoms(typeof(salesUnit), typeof(purchaseUnit))]
		[INUnit(DisplayName = "Base Unit")]
		public virtual String BaseUnit { get; set; }
		public abstract class baseUnit : BqlString.Field<baseUnit> { }
		#endregion
		#region SalesUnit
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(salesUnit))]
		[INUnit(null, typeof(baseUnit), DisplayName = "Sales Unit", Visibility = PXUIVisibility.Visible)]
		public virtual String SalesUnit { get; set; }
		public abstract class salesUnit : BqlString.Field<salesUnit> { }
		#endregion
		#region PurchaseUnit
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(purchaseUnit))]
		[INUnit(null, typeof(baseUnit), DisplayName = "Purchase Unit", Visibility = PXUIVisibility.Visible)]
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
		[PXDefault(true)]
		[INSyncUoms(typeof(decimalSalesUnit), typeof(decimalPurchaseUnit))]
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
		[PXDefault(true)]
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
		[PXDefault(true)]
		[PXUIField(DisplayName = "Divisible Unit", Visibility = PXUIVisibility.Visible)]
		public virtual bool? DecimalPurchaseUnit { get; set; }
		public abstract class decimalPurchaseUnit : BqlBool.Field<decimalPurchaseUnit> { }
		#endregion
		#region PostClassID
		[PXDBString(10, IsUnicode = true)]
		[PXSelector(typeof(Search<INPostClass.postClassID>), DescriptionField = typeof(INPostClass.descr))]
		[PXUIField(DisplayName = "Posting Class")]
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(postClassID), PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual String PostClassID { get; set; }
		public abstract class postClassID : BqlString.Field<postClassID> { }
		#endregion
		#region LotSerClassID
		[PXDBString(10, IsUnicode = true)]
		[PXSelector(typeof(Search<INLotSerClass.lotSerClassID>), DescriptionField = typeof(INLotSerClass.descr))]
		[PXUIField(DisplayName = "Lot/Serial Class")]
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(lotSerClassID), PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual String LotSerClassID { get; set; }
		public abstract class lotSerClassID : BqlString.Field<lotSerClassID> { }
		#endregion
		#region TaxCategoryID
		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Tax Category")]
		[PXSelector(typeof(TaxCategory.taxCategoryID), DescriptionField = typeof(TaxCategory.descr))]
		[PXRestrictor(typeof(Where<TaxCategory.active.IsEqual<True>>), TX.Messages.InactiveTaxCategory, typeof(TaxCategory.taxCategoryID))]
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(taxCategoryID), PersistingCheck = PXPersistingCheck.Nothing)]
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
		[PXDefault(TaxCalculationMode.TaxSetting)]
		[TaxCalculationMode.List]
		[PXUIField(DisplayName = "Tax Calculation Mode")]
		public virtual string TaxCalcMode { get; set; }
		public abstract class taxCalcMode : BqlString.Field<taxCalcMode> { }
		#endregion
		#region DeferredCode
		[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
		[PXUIField(DisplayName = "Deferral Code")]
		[PXSelector(typeof(Search<DRDeferredCode.deferredCodeID>))]
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(deferredCode), PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual String DeferredCode { get; set; }
		public abstract class deferredCode : BqlString.Field<deferredCode> { }
		#endregion
		#region ItemType
		[PXDBString(1, IsFixed = true)]
		[INItemTypes.List]
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(itemType), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Item Type")]
		public virtual String ItemType { get; set; }
		public abstract class itemType : BqlString.Field<itemType> { }
		#endregion
		#region PriceClassID
		[PXDBString(10, IsUnicode = true)]
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(priceClassID), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXSelector(typeof(INPriceClass.priceClassID), DescriptionField = typeof(INPriceClass.description))]
		[PXUIField(DisplayName = "Price Class", Visibility = PXUIVisibility.Visible)]
		public virtual String PriceClassID { get; set; }
		public abstract class priceClassID : BqlString.Field<priceClassID> { }
		#endregion
		#region PriceWorkgroupID
		[PXDBInt]
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(priceWorkgroupID), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXCompanyTreeSelector]
		[PXUIField(DisplayName = "Price Workgroup")]
		public virtual Int32? PriceWorkgroupID { get; set; }
		public abstract class priceWorkgroupID : BqlInt.Field<priceWorkgroupID> { }
		#endregion
		#region PriceManagerID
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(priceManagerID), PersistingCheck = PXPersistingCheck.Nothing)]
		[Owner(typeof(InventoryItem.priceWorkgroupID), DisplayName = "Price Manager")]
		public virtual int? PriceManagerID { get; set; }
		public abstract class priceManagerID : BqlInt.Field<priceManagerID> { }
		#endregion
		#region DfltSiteID
		[IN.Site(DisplayName = "Default Warehouse", DescriptionField = typeof(INSite.descr))]
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(dfltSiteID), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXForeignReference(typeof(FK.DefaultSite))]
		public virtual Int32? DfltSiteID { get; set; }
		public abstract class dfltSiteID : BqlInt.Field<dfltSiteID> { }
		#endregion
		#region MinGrossProfitPct
		[PXDefault(TypeCode.Decimal, "0.0", typeof(SelectParentItemClass), SourceField = typeof(minGrossProfitPct), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXDBDecimal(2, MinValue = 0)]
		[PXUIField(DisplayName = "Min. Markup %", Visibility = PXUIVisibility.Visible)]
		public virtual Decimal? MinGrossProfitPct { get; set; }
		public abstract class minGrossProfitPct : BqlDecimal.Field<minGrossProfitPct> { }
		#endregion
		#region MarkupPct
		[PXDBDecimal(6, MinValue = 0, MaxValue = 1000)]
		[PXDefault(TypeCode.Decimal, "0.0", typeof(SelectParentItemClass), SourceField = typeof(markupPct), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Markup %")]
		public virtual Decimal? MarkupPct { get; set; }
		public abstract class markupPct : BqlDecimal.Field<markupPct> { }
		#endregion
		#region NoteID
		[PXNote(DescriptionField = typeof(itemClassCD), Selector = typeof(itemClassCD))]
		public virtual Guid? NoteID { get; set; }
		public abstract class noteID : BqlGuid.Field<noteID> { }
		#endregion
		#region DemandCalculation
		[PXDBString(1, IsFixed = true)]
		[PXUIField(DisplayName = "Demand Calculation")]
		[PXDefault(INDemandCalculation.ItemClassSettings, typeof(SelectParentItemClass), SourceField = typeof(demandCalculation))]
		[INDemandCalculation.List]
		public virtual string DemandCalculation { get; set; }
		public abstract class demandCalculation : BqlString.Field<demandCalculation> { }
		#endregion
		#region HSTariffCode
		[PXDBString(30, IsUnicode = true)]
		[PXUIField(DisplayName = "Tariff Code")]
		public virtual string HSTariffCode { get; set; }
		public abstract class hSTariffCode : BqlString.Field<hSTariffCode> { }
		#endregion

		#region UndershipThreshold
		[PXDBDecimal(2, MinValue = 0.0, MaxValue = 100.0)]
		[PXDefault(TypeCode.Decimal, "100.0")]
		[PXUIField(DisplayName = "Undership Threshold (%)")]
		public virtual decimal? UndershipThreshold { get; set; }
		public abstract class undershipThreshold : BqlDecimal.Field<undershipThreshold> { }
		#endregion
		#region OvershipThreshold
		[PXDBDecimal(2, MinValue = 100.0, MaxValue = 999.0)]
		[PXDefault(TypeCode.Decimal, "100.0")]
		[PXUIField(DisplayName = "Overship Threshold (%)")]
		public virtual decimal? OvershipThreshold { get; set; }
		public abstract class overshipThreshold : BqlDecimal.Field<overshipThreshold> { }
		#endregion

		#region CountryOfOrigin
		[PXDBString(100, IsUnicode = true)]
		[PXUIField(DisplayName = "Country Of Origin")]
		[Country]
		public virtual string CountryOfOrigin { get; set; }
		public abstract class countryOfOrigin : BqlString.Field<countryOfOrigin> { }
		#endregion
		#region AccrueCost
		/// <summary>
		/// When set to <c>true</c>, indicates that cost will be processed using expense accrual account.
		/// </summary>
		[PXDBBool]
		[PXDefault(false)]
		[PXUIEnabled(typeof(Where<stkItem.IsEqual<False>>))]
		[PXFormula(typeof(IIf<Where<stkItem.IsEqual<True>>, False, accrueCost>))]
		[PXUIField(DisplayName = "Accrue Cost")]
		public virtual Boolean? AccrueCost { get; set; }
		public abstract class accrueCost : BqlBool.Field<accrueCost> { }
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
		[PXDBGroupMask]
		[PXDefault(typeof(SelectParentItemClass), SourceField = typeof(groupMask), PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Byte[] GroupMask { get; set; }
		public abstract class groupMask : BqlByteArray.Field<groupMask> { }
		#endregion

		#region Included
		[PXBool]
		[PXUIField(DisplayName = "Included")]
		[PXUnboundDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual bool? Included { get; set; }
		public abstract class included : BqlBool.Field<included> { }
		#endregion
		#region ItemClassStrID
		[PXString]
		[PXUIField(Visible = false, Visibility = PXUIVisibility.Invisible)]
		public virtual string ItemClassStrID => ItemClassID.ToString();
		public abstract class itemClassStrID : BqlString.Field<itemClassStrID> { }
		#endregion
		#region ItemClassCDWildcard
		[PXString(IsUnicode = true)]
		[PXUIField(Visible = false, Visibility = PXUIVisibility.Invisible)]
		[PXDimension(Dimension, ParentSelect = typeof(Select<INItemClass>), ParentValueField = typeof(itemClassCD))]
		public virtual string ItemClassCDWildcard
		{
			get => ItemClassTree.MakeWildcard(ItemClassCD);
			set { }
		}
		public abstract class itemClassCDWildcard : BqlString.Field<itemClassCDWildcard> { }
		#endregion
		#region Matrix
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
		#region DefaultRowMatrixAttributeID
		/// <summary>
		/// References to Attribute which will be put as Row Attribute in Inventory Matrix by default.
		/// </summary>
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Default Row Attribute ID", FieldClass = nameof(FeaturesSet.MatrixItem))]
		[PXSelector(typeof(
			SearchFor<CSAttributeGroup.attributeID>.
			Where<
				CSAttributeGroup.entityClassID.IsEqual<PX.Data.BQL.RTrim<Use<itemClassID.FromCurrent>.AsString>>.
				And<CSAttributeGroup.entityType.IsEqual<Common.Constants.DACName<InventoryItem>>>.
				And<CSAttributeGroup.attributeCategory.IsEqual<CSAttributeGroup.attributeCategory.variant>>.
				And<
					CSAttributeGroup.attributeID.IsNotEqual<defaultColumnMatrixAttributeID.FromCurrent>.
					Or<defaultColumnMatrixAttributeID.FromCurrent.IsNull>>>),
			typeof(CSAttributeGroup.attributeID), DescriptionField = typeof(CSAttributeGroup.description), DirtyRead = true)]
		[PXRestrictor(typeof(Where<CSAttributeGroup.isActive.IsEqual<True>>), Messages.AttributeIsInactive, typeof(CSAttributeGroup.attributeID))]
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
		[PXSelector(typeof(
			SearchFor<CSAttributeGroup.attributeID>.
			Where<
				CSAttributeGroup.entityClassID.IsEqual<PX.Data.BQL.RTrim<Use<itemClassID.FromCurrent>.AsString>>.
				And<CSAttributeGroup.entityType.IsEqual<Common.Constants.DACName<InventoryItem>>>.
				And<CSAttributeGroup.attributeCategory.IsEqual<CSAttributeGroup.attributeCategory.variant>>.
				And<
					CSAttributeGroup.attributeID.IsNotEqual<defaultRowMatrixAttributeID.FromCurrent>.
					Or<defaultRowMatrixAttributeID.FromCurrent.IsNull>>>),
			typeof(CSAttributeGroup.attributeID), DescriptionField = typeof(CSAttributeGroup.description), DirtyRead = true)]
		[PXRestrictor(typeof(Where<CSAttributeGroup.isActive.IsEqual<True>>), Messages.AttributeIsInactive, typeof(CSAttributeGroup.attributeID))]
		public virtual string DefaultColumnMatrixAttributeID { get; set; }
		public abstract class defaultColumnMatrixAttributeID : BqlString.Field<defaultColumnMatrixAttributeID> { }
		#endregion
		#region GenerationRuleCntr
		[PXDBInt]
		[PXDefault(0)]
		public virtual int? GenerationRuleCntr { get; set; }
		public abstract class generationRuleCntr : BqlInt.Field<generationRuleCntr> { }
		#endregion
		#endregion // Matrix

		#region ExportToExternal
		public abstract class exportToExternal : IBqlField { }
		protected bool? _ExportToExternal;
		[PXDBBool()]
		[PXUIField(DisplayName = "Export to External System")]
		[PXDefault(true)]
		public virtual bool? ExportToExternal
		{
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

	public class INDemandCalculation
	{
		public const string ItemClassSettings = "I";
		public const string HardDemand = "H";

		public class List : PXStringListAttribute
		{
			public List() : base(
				Pair(ItemClassSettings, Messages.ItemClassSettings),
				Pair(HardDemand, Messages.HardDemand))
			{ }
		}

		public class itemClassSettings : BqlString.Constant<itemClassSettings> { public itemClassSettings() : base(ItemClassSettings) { } }
		public class hardDemand : BqlString.Constant<hardDemand> { public hardDemand() : base(HardDemand) { } }
	}

	// Used in AR674000.rpx
	public class FilterItemByClass : IBqlTable
	{
		#region ItemClassID
		[PXDBInt]
		[PXUIField(DisplayName = "Item Class", Visibility = PXUIVisibility.SelectorVisible)]
		[PXDimensionSelector(INItemClass.Dimension, typeof(Search<INItemClass.itemClassID>), typeof(INItemClass.itemClassCD), DescriptionField = typeof(INItemClass.descr))]
		public virtual int? ItemClassID { get; set; }
		public abstract class itemClassID : BqlInt.Field<itemClassID> { }
		#endregion
		#region InventoryID
		[AnyInventory(typeof(
			SearchFor<InventoryItem.inventoryID>.
			Where<MatchUser.
				And<
					InventoryItem.itemClassID.IsEqual<FilterItemByClass.itemClassID.AsOptional>.
					Or<FilterItemByClass.itemClassID.AsOptional.IsNull>>>),
			typeof(InventoryItem.inventoryCD), typeof(InventoryItem.descr))]
		public virtual Int32? InventoryID { get; set; }
		public abstract class inventoryID : BqlInt.Field<inventoryID> { }
		#endregion
	}
}