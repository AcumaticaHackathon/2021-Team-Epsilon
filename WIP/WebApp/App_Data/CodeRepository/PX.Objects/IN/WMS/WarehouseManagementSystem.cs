using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using PX.Common;
using PX.Common.GS1;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.BarcodeProcessing;

namespace PX.Objects.IN.WMS
{
	public abstract class WarehouseManagementSystem<TSelf, TGraph> : BarcodeDrivenStateMachine<TSelf, TGraph>
		where TSelf : WarehouseManagementSystem<TSelf, TGraph>
		where TGraph : PXGraph, new()
	{
		public static bool IsActiveBase() => PXAccess.FeatureInstalled<CS.FeaturesSet.advancedFulfillment>();

		#region Extensions
		#region QtySupport
		protected QtySupport QtyExt => Graph.FindImplementation<QtySupport>();
		public abstract class QtySupport : BarcodeQtySupport<TSelf, TGraph>
		{
			public override bool UseQtyCorrectection => Basis.UseQtyCorrectection;
			public override bool CanOverrideQty => base.CanOverrideQty && Basis.CanOverrideQty;
			public override bool IsMandatoryQtyInput => Basis.HeaderView.Current.PrevScanState != QtyState.Value && Basis.SelectedInventoryItem?.WeightItem == true;
		}
		#endregion
		#region GS1Support
		protected GS1Support GS1Ext => Graph.FindImplementation<GS1Support>();
		public abstract class GS1Support : GS1BarcodeSupport<TSelf, TGraph>
		{
			protected override IEnumerable<BarcodeComponentApplicationStep> GetBarcodeComponentApplicationSteps()
			{
				return new[]
				{
					new BarcodeComponentApplicationStep(InventoryItemState.Value,   Codes.GTIN.Code,            data => data.String),
					new BarcodeComponentApplicationStep(InventoryItemState.Value,   Codes.Content.Code,         data => data.String),
					new BarcodeComponentApplicationStep(LotSerialState.Value,       Codes.BatchLot.Code,        data => data.String),
					new BarcodeComponentApplicationStep(LotSerialState.Value,       Codes.Serial.Code,          data => data.String),
					new BarcodeComponentApplicationStep(ExpireDateState.Value,      Codes.BestBeforeDate.Code,  data => data.Date.Value.ToString()),
					new BarcodeComponentApplicationStep(ExpireDateState.Value,      Codes.ExpireDate.Code,      data => data.Date.Value.ToString()),
				};
			}
		}
		#endregion
		#endregion

		#region State
		public WMSScanHeader WMSHeader => Header.Get<WMSScanHeader>() ?? new WMSScanHeader();
		public ValueSetter<ScanHeader>.Ext<WMSScanHeader> WMSSetter => HeaderSetter.With<WMSScanHeader>();

		#region RefNbr
		public string RefNbr
		{
			get => WMSHeader.RefNbr;
			set => WMSSetter.Set(h => h.RefNbr, value);
		}
		#endregion
		#region SiteID
		public int? SiteID
		{
			get => WMSHeader.SiteID;
			set => WMSSetter.Set(h => h.SiteID, value);
		}
		#endregion
		#region LocationID
		public int? LocationID
		{
			get => WMSHeader.LocationID;
			set => WMSSetter.Set(h => h.LocationID, value);
		}
		#endregion
		#region InventoryID
		public int? InventoryID
		{
			get => WMSHeader.InventoryID;
			set => WMSSetter.Set(h => h.InventoryID, value);
		}
		#endregion
		#region SubItemID
		public int? SubItemID
		{
			get => WMSHeader.SubItemID;
			set => WMSSetter.Set(h => h.SubItemID, value);
		}
		#endregion
		#region UOM
		public string UOM
		{
			get => WMSHeader.UOM;
			set => WMSSetter.Set(h => h.UOM, value);
		}
		#endregion
		#region Qty
		public decimal? Qty
		{
			get => WMSHeader.Qty;
			set => WMSSetter.Set(h => h.Qty, value);
		}
		#endregion
		#region BaseQty
		public decimal? BaseQty => WMSHeader.BaseQty;
		#endregion
		#region LotSerialNbr
		public string LotSerialNbr
		{
			get => WMSHeader.LotSerialNbr;
			set => WMSSetter.Set(h => h.LotSerialNbr, value);
		}
		#endregion
		#region ExpireDate
		public DateTime? ExpireDate
		{
			get => WMSHeader.ExpireDate;
			set => WMSSetter.Set(h => h.ExpireDate, value);
		}
		#endregion
		#region Remove
		public bool? Remove
		{
			get => WMSHeader.Remove;
			set => WMSSetter.Set(h => h.Remove, value);
		}
		#endregion
		#region TranDate
		public DateTime? TranDate
		{
			get => WMSHeader.TranDate;
			set => WMSSetter.Set(h => h.TranDate, value);
		}
		#endregion
		#endregion

		#region Selected entities
		public INSite SelectedSite => INSite.PK.Find(Graph, SiteID);
		public INLocation SelectedLocation => INLocation.PK.Find(Graph, LocationID);
		public InventoryItem SelectedInventoryItem => InventoryItem.PK.Find(Graph, InventoryID);
		public INLotSerClass SelectedLotSerialClass => GetLotSerialClassOf(SelectedInventoryItem);
		#endregion

		#region Configuration
		protected abstract bool UseQtyCorrectection { get; }
		protected virtual bool CanOverrideQty => DocumentIsEditable && SelectedLotSerialClass?.LotSerTrack != INLotSerTrack.SerialNumbered;

		public virtual bool DocumentLoaded => RefNbr != null;
		public virtual bool DocumentIsEditable => DocumentLoaded;
		protected virtual string DocumentIsNotEditableMessage => Msg.DocumentIsNotEditable;
		#endregion

		#region Buttons
		public PXAction<ScanHeader> Review;
		[PXButton, PXUIField(DisplayName = "Review")]
		protected virtual IEnumerable review(PXAdapter adapter) => adapter.Get();
		#endregion

		#region Event Handlers
		protected override void _(Events.RowSelected<ScanHeader> e)
		{
			base._(e);
			Review.SetVisible(Base.IsMobile);
		}
		#endregion

		#region Helpers
		public INLotSerClass GetLotSerialClassOf(InventoryItem inventoryItem)
			=> inventoryItem.With(ii => ii.StkItem == true
				? INLotSerClass.PK.Find(Graph, ii.LotSerClassID)
				: DefaultLotSerialClass);
		public bool ItemHasLotSerial => SelectedLotSerialClass?.LotSerTrack.IsIn(INLotSerTrack.LotNumbered, INLotSerTrack.SerialNumbered) == true;
		public bool ItemHasExpireDate => SelectedLotSerialClass?.LotSerTrackExpiration == true;

		public virtual bool IsEnterableLotSerial(bool isForIssue) => isForIssue
			? SelectedLotSerialClass?.LotSerAssign == INLotSerAssign.WhenUsed || SelectedLotSerialClass?.LotSerIssueMethod == INLotSerIssueMethod.UserEnterable
			: SelectedLotSerialClass?.LotSerAssign == INLotSerAssign.WhenReceived;

		protected virtual int? DefaultSiteID => UserPreferenceExt.GetDefaultSite(Graph);
		protected virtual INLotSerClass DefaultLotSerialClass
		{
			get
			{
				return new INLotSerClass
				{
					LotSerTrack = INLotSerTrack.NotNumbered,
					LotSerAssign = INLotSerAssign.WhenReceived,
					LotSerTrackExpiration = false,
					AutoNextNbr = true
				};
			}
		}

		public DateTime? EnsureExpireDateDefault() => LSSelect.ExpireDateByLot(Graph, GetLSMaster(), null);

		public ILSMaster GetLSMaster()
		{
			return new LSMasterDummy
			{
				SiteID = SiteID,
				LocationID = LocationID,
				InventoryID = InventoryID,
				SubItemID = SubItemID,
				LotSerialNbr = LotSerialNbr,
				ExpireDate = ExpireDate,
				UOM = UOM,
				Qty = Qty,
				TranDate = TranDate,
				InvtMult = Header.Get<WMSScanHeader>().InventoryMultiplicator
			};
		}
		#endregion

		#region Decoration
		protected override ScanMode<TSelf> LateDecorateScanMode(ScanMode<TSelf> original)
		{
			var mode = base.LateDecorateScanMode(original);
			RemoveCommand.InterceptResetMode(mode);
			return mode;
		}
		#endregion

		#region Overrides
		protected override bool CanHandleScan(string barcode)
		{
			if (!barcode.StartsWith(ScanMarkers.Redirect) &&
				!barcode.StartsWith(ScanMarkers.Command) &&
				Header.ScanState.IsNotIn(RefNbrState<object>.Value, BuiltinScanStates.Command) &&
				DocumentLoaded && !DocumentIsEditable)
			{
				Graph.Clear();
				Graph.SelectTimeStamp();
				ReportError(DocumentIsNotEditableMessage);
				return false;
			}

			return true;
		}
		#endregion

		#region States
		public abstract class RefNbrState<TDocument> : EntityState<TDocument>
		{
			public const string Value = "RNBR";
			public class value : BqlString.Constant<value> { public value() : base(RefNbrState<TDocument>.Value) { } }

			public override string Code => Value;
			protected override bool IsStateSkippable() => Basis.RefNbr != null && Basis.Header.ProcessingSucceeded != true;
		}

		public abstract class WarehouseState : WarehouseState<TSelf>
		{
			protected override sealed int? SiteID
			{
				get => Basis.SiteID;
				set => Basis.SiteID = value;
			}

			protected override Validation Validate(INSite site) => Basis.IsValid<WMSScanHeader.siteID>(site.SiteID, out string error) ? Validation.Ok : Validation.Fail(error);
		}

		public class LocationState : EntityState<INLocation>
		{
			public const string Value = "LOCN";
			public class value : BqlString.Constant<value> { public value() : base(LocationState.Value) { } }

			public override string Code => Value;
			protected override string StatePrompt => Msg.Prompt;
			protected override bool IsStateActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.warehouseLocation>();

			protected override INLocation GetByBarcode(string barcode)
			{
				return
					SelectFrom<INLocation>.
					Where<
						INLocation.siteID.IsEqual<@P.AsInt>.
						And<INLocation.locationCD.IsEqual<@P.AsString>>>.
					View.ReadOnly.Select(Basis, Basis.SiteID, barcode);
			}
			protected override void ReportMissing(string barcode) => Basis.Reporter.Error(Msg.Missing, barcode, Basis.SelectedSite.SiteCD);
			protected override Validation Validate(INLocation location) => location.Active == true ? Validation.Ok : Validation.Fail(Messages.InactiveLocation, location.LocationCD);
			protected override void Apply(INLocation location) => Basis.LocationID = location.LocationID;
			protected override void ReportSuccess(INLocation location) => Basis.Reporter.Info(Msg.Ready, location.LocationCD);
			protected override void ClearState() => Basis.LocationID = null;

			[PXLocalizable]
			public abstract class Msg
			{
				public const string Prompt = "Scan the barcode of the location.";
				public const string Ready = "The {0} location is selected.";
				public const string Missing = "The {0} location is not found in the {1} warehouse.";
				public const string NotSet = "The location is not selected.";
			}
		}

		public class InventoryItemState : EntityState<PXResult<INItemXRef, InventoryItem>>
		{
			public const string Value = "ITEM";
			public class value : BqlString.Constant<value> { public value() : base(InventoryItemState.Value) { } }

			public bool IsForIssue { get; set; } = false;
			public INPrimaryAlternateType? AlternateType { get; set; } = null;
			public string DefaultUOM(InventoryItem inventoryItem) =>
				Basis.GetLotSerialClassOf(inventoryItem)?.LotSerTrack == INLotSerTrack.SerialNumbered ? inventoryItem.BaseUnit :
				AlternateType == INPrimaryAlternateType.CPN ? inventoryItem.SalesUnit :
				AlternateType == INPrimaryAlternateType.VPN ? inventoryItem.PurchaseUnit :
				inventoryItem.BaseUnit;

			public override string Code => Value;
			protected override string StatePrompt => Msg.Prompt;

			protected override PXResult<INItemXRef, InventoryItem> GetByBarcode(string barcode) => ReadItemByBarcode(barcode, AlternateType);

			protected override void ReportMissing(string barcode) => Basis.Reporter.Error(Msg.Missing, barcode);

			protected override Validation Validate(PXResult<INItemXRef, InventoryItem> entity)
			{
				(var xref, var inventoryItem) = entity;
				string uom = xref.UOM ?? DefaultUOM(inventoryItem);

				INLotSerClass lsClass = Basis.GetLotSerialClassOf(inventoryItem);
				if (lsClass.LotSerTrack == INLotSerTrack.SerialNumbered &&
					(IsForIssue
						? lsClass.LotSerAssign == INLotSerAssign.WhenUsed
						: lsClass.LotSerAssign == INLotSerAssign.WhenReceived) &&
					uom != inventoryItem.BaseUnit)
				{
					return Validation.Fail(Msg.SerialItemNotComplexQty);
				}

				return Validation.Ok;
			}

			protected override void Apply(PXResult<INItemXRef, InventoryItem> entity)
			{
				(var xref, var inventoryItem) = entity;

				Basis.InventoryID = xref.InventoryID;
				Basis.SubItemID = xref.SubItemID;
				if (Basis.UOM == null)
					Basis.UOM = xref.UOM ?? DefaultUOM(inventoryItem);
			}

			protected override void ClearState()
			{
				Basis.InventoryID = null;
				Basis.SubItemID = null;
				Basis.UOM = null;
			}

			protected override void ReportSuccess(PXResult<INItemXRef, InventoryItem> entity) => Basis.Reporter.Info(Msg.Ready, entity.GetItem<InventoryItem>().InventoryCD.Trim());

			protected PXResult<INItemXRef, InventoryItem> ReadItemByBarcode(string barcode, INPrimaryAlternateType? additionalAlternateType = null)
			{
				var view = new
					SelectFrom<INItemXRef>.
					InnerJoin<InventoryItem>.On<INItemXRef.FK.InventoryItem>.
					Where<
						INItemXRef.alternateID.IsEqual<@P.AsString>.
						And<InventoryItem.itemStatus.IsNotIn<InventoryItemStatus.inactive, InventoryItemStatus.markedForDeletion>>>.
					OrderBy<INItemXRef.alternateType.Asc>.
					View.ReadOnly(Basis);

				if (additionalAlternateType == INPrimaryAlternateType.CPN)
					view.WhereAnd<Where<
						INItemXRef.alternateType.IsIn<INAlternateType.barcode, INAlternateType.cPN>.
						And<InventoryItem.itemStatus.IsNotEqual<InventoryItemStatus.noSales>>>>();
				if (additionalAlternateType == INPrimaryAlternateType.VPN)
					view.WhereAnd<Where<
						INItemXRef.alternateType.IsIn<INAlternateType.barcode, INAlternateType.vPN>.
						And<InventoryItem.itemStatus.IsNotEqual<InventoryItemStatus.noPurchases>>>>();
				else
					view.WhereAnd<Where<INItemXRef.alternateType.IsEqual<INAlternateType.barcode>>>();

				var item = view
					.Select(barcode).AsEnumerable()
					.OrderByDescending(r => r.GetItem<INItemXRef>().AlternateType == INAlternateType.Barcode)
					.Cast<PXResult<INItemXRef, InventoryItem>>()
					.FirstOrDefault();

				if (item == null || ((InventoryItem)item) == null)
					item = ReadItemById(barcode, additionalAlternateType);

				return item;
			}

			private PXResult<INItemXRef, InventoryItem> ReadItemById(string barcode, INPrimaryAlternateType? additionalAlternateType = null)
			{
				var inventory = InventoryItem.UK.Find(Basis, barcode);
				if (inventory != null &&
					inventory.ItemStatus.IsNotIn(InventoryItemStatus.Inactive, InventoryItemStatus.MarkedForDeletion) &&
					(additionalAlternateType == INPrimaryAlternateType.CPN).Implies(inventory.ItemStatus != InventoryItemStatus.NoSales) &&
					(additionalAlternateType == INPrimaryAlternateType.VPN).Implies(inventory.ItemStatus != InventoryItemStatus.NoPurchases))
				{
					var xref = new INItemXRef { InventoryID = inventory.InventoryID, AlternateType = INAlternateType.Barcode, AlternateID = barcode };
					Basis.Graph.Caches<INItemXRef>().RaiseFieldDefaulting<INItemXRef.subItemID>(xref, out object defaultSubItem);
					xref.SubItemID = (int?)defaultSubItem;

					return new PXResult<INItemXRef, InventoryItem>(xref, inventory);
				}

				return null;
			}

			[PXLocalizable]
			public abstract class Msg
			{
				public const string Prompt = "Scan the barcode of the item.";
				public const string Ready = "The {0} item is selected.";
				public const string Missing = "The {0} item barcode is not found.";
				public const string NotSet = "The item is not selected.";
				public const string SerialItemNotComplexQty = "Serialized items can be processed only with the base UOM and the 1.00 quantity.";
			}
		}

		public class LotSerialState : EntityState<string>
		{
			public const string Value = "LTSR";
			public class value : BqlString.Constant<value> { public value() : base(LotSerialState.Value) { } }

			public override string Code => Value;
			protected override string StatePrompt => Msg.Prompt;
			protected override bool IsStateActive() => Basis.ItemHasLotSerial;

			protected override string GetByBarcode(string barcode) => barcode.Trim();
			protected override Validation Validate(string lotSerial) => Basis.IsValid<WMSScanHeader.lotSerialNbr>(lotSerial, out string error) ? Validation.Ok : Validation.Fail(error);
			protected override void Apply(string lotSerial) => Basis.LotSerialNbr = lotSerial;
			protected override void ReportSuccess(string lotSerial) => Basis.Reporter.Info(Msg.Ready, lotSerial);
			protected override void ClearState() => Basis.LotSerialNbr = null;

			[PXLocalizable]
			public abstract class Msg
			{
				public const string Prompt = "Scan the lot/serial number.";
				public const string Ready = "The {0} lot/serial number is selected.";
				public const string NotSet = "The lot/serial number is not selected.";
			}
		}

		public class ExpireDateState : EntityState<DateTime?>
		{
			public const string Value = "EXPD";
			public class value : BqlString.Constant<value> { public value() : base(ExpireDateState.Value) { } }

			public bool IsForIssue { get; set; } = false;

			public override string Code => Value;
			protected override string StatePrompt => Msg.Prompt;

			protected override bool IsStateActive()
			{
				return
					Basis.Remove == false &&
					Basis.ItemHasLotSerial &&
					Basis.ItemHasExpireDate &&
					Basis.IsEnterableLotSerial(IsForIssue);
			}

			protected override DateTime? GetByBarcode(string barcode) => DateTime.TryParse(barcode.Trim(), out DateTime value) ? value : (DateTime?)null;
			protected override void ReportMissing(string barcode) => Basis.Reporter.Error(Msg.BadFormat);
			protected override Validation Validate(DateTime? expireDate) => Basis.IsValid<WMSScanHeader.expireDate>(expireDate, out string error) ? Validation.Ok : Validation.Fail(error);
			protected override void Apply(DateTime? expireDate) => Basis.ExpireDate = expireDate;
			protected override void ReportSuccess(DateTime? expireDate) => Basis.Reporter.Info(Msg.Ready, expireDate);
			protected override void ClearState() => Basis.ExpireDate = null;

			[PXLocalizable]
			public abstract class Msg
			{
				public const string Prompt = "Scan the lot/serial expiration date.";
				public const string Ready = "The expiration date is set to {0:d}.";
				public const string BadFormat = "The date format does not fit the locale settings.";
				public const string NotSet = "The expiration date is not selected.";
			}
		}
		#endregion

		#region Commands
		public class RemoveCommand : ScanCommand
		{
			public override string Code => "REMOVE";
			public override string ButtonName => "scanRemove";
			public override string DisplayName => Msg.DisplayName;
			protected override bool IsEnabled => Basis.Remove == false && Basis.DocumentLoaded && Basis.DocumentIsEditable;

			protected override bool Process()
			{
				Basis.CurrentMode.Reset(fullReset: false);
				Basis.Remove = true;
				Basis.SetScanState(Basis.CurrentMode.DefaultState.Code);
				Basis.Reporter.Info(Msg.RemoveMode);
				return true;
			}

			[PXLocalizable]
			public abstract class Msg
			{
				public const string DisplayName = "Remove";
				public const string RemoveMode = "Remove mode is activated.";
			}

			public static void InterceptResetMode(ScanMode<TSelf> mode)
			{
				if (mode.Commands.OfType<RemoveCommand>().Any())
					mode.Intercept.ResetMode.ByAppend(
						(basis, fullReset) => basis.Remove = false);
			}
		}
		#endregion

		#region Messages
		[PXLocalizable]
		public new abstract class Msg : BarcodeDrivenStateMachine<TSelf, TGraph>.Msg
		{
			public const string DocumentIsNotEditable = "The document became unavailable for editing. Contact your manager.";
		}
		#endregion
	}

	public sealed class WMSScanHeader : PXCacheExtension<QtyScanHeader, ScanHeader>
	{
		public static bool IsActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.advancedFulfillment>();

		#region RefNbr
		[PXString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Reference Nbr.", Enabled = false)]
		public string RefNbr { get; set; }
		public abstract class refNbr : BqlString.Field<refNbr> { }
		#endregion
		#region SiteID
		[Site(Enabled = false)]
		public int? SiteID { get; set; }
		public abstract class siteID : BqlInt.Field<siteID> { }
		#endregion
		#region LocationID
		[Location(Enabled = false)]
		public int? LocationID { get; set; }
		public abstract class locationID : BqlInt.Field<locationID> { }
		#endregion
		#region InventoryID
		[StockItem(Enabled = false)]
		public int? InventoryID { get; set; }
		public abstract class inventoryID : BqlInt.Field<inventoryID> { }
		#endregion
		#region SubItemID
		[SubItem(typeof(inventoryID), Enabled = false)]
		public int? SubItemID { get; set; }
		public abstract class subItemID : BqlInt.Field<subItemID> { }
		#endregion
		#region UOM
		[INUnit(typeof(inventoryID), Enabled = false)]
		public String UOM { get; set; }
		public abstract class uOM : BqlString.Field<uOM> { }
		#endregion
		#region Qty
		[PXQuantity(typeof(uOM), typeof(baseQty), HandleEmptyKey = true)]
		[PXUnboundDefault(TypeCode.Decimal, "1")]
		public decimal? Qty { get; set; }
		public abstract class qty : BqlDecimal.Field<qty> { }
		#endregion
		#region BaseQty
		[PXDecimal(6)]
		public Decimal? BaseQty { get; set; }
		public abstract class baseQty : BqlDecimal.Field<baseQty> { }
		#endregion
		#region LotSerialNbr
		[PXString]
		public string LotSerialNbr { get; set; }
		public abstract class lotSerialNbr : BqlString.Field<lotSerialNbr> { }
		#endregion
		#region ExpireDate
		[PXDate]
		public DateTime? ExpireDate { get; set; }
		public abstract class expireDate : BqlDateTime.Field<expireDate> { }
		#endregion
		#region Remove
		[PXBool, PXUnboundDefault(false)]
		[PXUIField(DisplayName = "Remove Mode", Enabled = false)]
		[PXUIVisible(typeof(remove))]
		public bool? Remove { get; set; }
		public abstract class remove : BqlBool.Field<remove> { }
		#endregion

		#region TranDate
		[PXDate]
		[PXUnboundDefault(typeof(AccessInfo.businessDate))]
		public DateTime? TranDate { get; set; }
		public abstract class tranDate : BqlDateTime.Field<tranDate> { }
		#endregion
		#region InventoryMultiplicator
		[PXShort]
		public short? InventoryMultiplicator { get; set; }
		public abstract class inventoryMultiplicator : BqlShort.Field<inventoryMultiplicator> { }
		#endregion
	}

	[PXHidden]
	public class LSMasterDummy : IBqlTable, ILSMaster
	{
		#region SiteID
		[Site]
		public int? SiteID { get; set; }
		public abstract class siteID : BqlInt.Field<siteID> { }
		#endregion
		#region LocationID
		[Location]
		public virtual int? LocationID { get; set; }
		public abstract class locationID : BqlInt.Field<locationID> { }
		#endregion

		#region InventoryID
		[StockItem]
		public virtual int? InventoryID { get; set; }
		public abstract class inventoryID : BqlInt.Field<inventoryID> { }
		#endregion
		#region SubItemID
		[SubItem(typeof(inventoryID))]
		public virtual int? SubItemID { get; set; }
		public abstract class subItemID : BqlInt.Field<subItemID> { }
		#endregion

		#region LotSerialNbr
		[INLotSerialNbr(typeof(inventoryID), typeof(subItemID), typeof(locationID), PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual string LotSerialNbr { get; set; }
		public abstract class lotSerialNbr : BqlString.Field<lotSerialNbr> { }
		#endregion
		#region ExpireDate
		[INExpireDate(typeof(inventoryID), PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual DateTime? ExpireDate { get; set; }
		public abstract class expireDate : BqlDateTime.Field<expireDate> { }
		#endregion

		#region UOM
		[INUnit(typeof(inventoryID))]
		public virtual String UOM { get; set; }
		public abstract class uOM : BqlString.Field<uOM> { }
		#endregion
		#region Qty
		[PXQuantity(typeof(uOM), typeof(baseQty), HandleEmptyKey = true)]
		public virtual decimal? Qty { get; set; }
		public abstract class qty : BqlDecimal.Field<qty> { }
		#endregion
		#region BaseQty
		[PXDecimal(6)]
		public virtual Decimal? BaseQty { get; set; }
		public abstract class baseQty : BqlDecimal.Field<baseQty> { }
		#endregion

		#region TranDate
		[PXDate]
		public virtual DateTime? TranDate { get; set; }
		public abstract class tranDate : BqlDateTime.Field<tranDate> { }
		#endregion
		#region InvtMult
		[PXShort]
		public virtual short? InvtMult { get; set; }
		public abstract class invtMult : BqlShort.Field<invtMult> { }
		#endregion

		#region ILSMaster implementation
		public string TranType => string.Empty;
		public int? ProjectID { get; set; }
		public int? TaskID { get; set; }
		public bool? IsIntercompany => false;
		#endregion
	}
}