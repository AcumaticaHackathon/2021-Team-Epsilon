using System;
using System.Collections;
using System.Collections.Generic;

using PX.Common;
using PX.Common.GS1;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.BarcodeProcessing;

namespace PX.Objects.IN.WMS
{
	using WMSBase = BarcodeDrivenStateMachine<InventoryItemLookup, InventoryItemLookup.Host>;

	public class InventoryItemLookup : WMSBase
	{
		public class Host : InventorySummaryEnq { }

		#region GS1Support
		protected GS1Support GS1Ext => Graph.FindImplementation<GS1Support>();
		public class GS1Support : GS1BarcodeSupport<InventoryItemLookup, Host>
		{
			protected override IEnumerable<BarcodeComponentApplicationStep> GetBarcodeComponentApplicationSteps()
			{
				return new[]
				{
					new BarcodeComponentApplicationStep(InventoryItemState.Value,   Codes.GTIN.Code,        data => data.String),
					new BarcodeComponentApplicationStep(InventoryItemState.Value,   Codes.Content.Code,     data => data.String),
				};
			}
		}
		#endregion

		#region State
		public ItemLookupScanHeader ItemHeader => Header.Get<ItemLookupScanHeader>();
		public ValueSetter<ScanHeader>.Ext<ItemLookupScanHeader> ItemSetter => HeaderSetter.With<ItemLookupScanHeader>();

		#region SiteID
		public int? SiteID
		{
			get => ItemHeader.SiteID;
			set => ItemSetter.Set(h => h.SiteID, value);
		}
		#endregion
		#region InventoryID
		public int? InventoryID
		{
			get => ItemHeader.InventoryID;
			set => ItemSetter.Set(h => h.InventoryID, value);
		}
		#endregion
		#endregion

		#region Buttons
		public PXAction<ScanHeader> ReviewAvailability;
		[PXButton, PXUIField(DisplayName = "Review")]
		protected virtual IEnumerable reviewAvailability(PXAdapter adapter) => adapter.Get();
		#endregion

		#region DAC overrides
		[Common.Attributes.BorrowedNote(typeof(InventoryItem), typeof(InventorySummaryEnq))]
		protected virtual void _(Events.CacheAttached<ScanHeader.noteID> e) { }
		#endregion

		#region Event Handlers
		protected virtual void _(Events.RowSelected<InventorySummaryEnqFilter> e) => e.Cache.IsDirty = false;
		protected virtual void _(Events.FieldDefaulting<InventorySummaryEnqFilter, InventorySummaryEnqFilter.expandByLotSerialNbr> e) => e.NewValue = PXAccess.FeatureInstalled<CS.FeaturesSet.lotSerialTracking>();

		protected override void _(Events.RowSelected<ScanHeader> e)
		{
			base._(e);
			ReviewAvailability.SetVisible(Base.IsMobile);
		}
		#endregion

		#region Views
		public SelectFrom<InventoryItem>.Where<InventoryItem.inventoryID.IsEqual<ItemLookupScanHeader.inventoryID.FromCurrent>>.View.ReadOnly InventoryItem;
		public PXSetupOptional<INScanSetup, Where<INScanSetup.branchID.IsEqual<AccessInfo.branchID.FromCurrent>>> Setup;
		#endregion

		#region State Machine
		protected override IEnumerable<ScanMode<InventoryItemLookup>> CreateScanModes() => new[]
{
			new ScanMode.Simple("ITEM", Msg.ModeDescription)
				.Intercept.CreateStates.ByReplace(() => new ScanState<InventoryItemLookup>[]
				{
					new WarehouseState(),
					new InventoryItemState()
				})
				.Intercept.CreateRedirects.ByReplace(() => AllWMSRedirects.CreateFor<InventoryItemLookup>())
				.Intercept.ResetMode.ByReplace((basis, fullReset) =>
				{
					basis.Clear<WarehouseState>(when: fullReset);
					basis.Clear<InventoryItemState>();
				})
		};
		#endregion

		#region States
		public sealed class WarehouseState : WarehouseState<InventoryItemLookup>
		{
			protected override int? SiteID
			{
				get => Basis.SiteID;
				set => Basis.SiteID = value;
			}

			private void SetFilteringSite(int? siteID)
			{
				Basis.Graph.Filter.Cache.SetValueExt<InventorySummaryEnqFilter.siteID>(Basis.Graph.Filter.Current, siteID);
				Basis.Graph.Filter.UpdateCurrent();
			}

			protected override Validation Validate(INSite site) => Basis.IsValid<ItemLookupScanHeader.siteID>(site.SiteID, out string error) ? Validation.Ok : Validation.Fail(error);

			protected override void Apply(INSite site)
			{
				base.Apply(site);
				SetFilteringSite(site.SiteID);
			}
			protected override void ClearState()
			{
				base.ClearState();
				SetFilteringSite(null);
			}

			protected override void SetNextState() => Basis.SetScanState<InventoryItemState>();

			protected override bool UseDefaultWarehouse => Basis.Setup.Current.DefaultWarehouse == true;
		}

		public sealed class InventoryItemState : EntityState<InventoryItem>
		{
			public const string Value = "ITEM";
			public class value : BqlString.Constant<value> { public value() : base(InventoryItemState.Value) { } }

			public override string Code => Value;
			protected override string StatePrompt => Msg.Prompt;

			protected override InventoryItem GetByBarcode(string barcode)
			{
				InventoryItem item =
					SelectFrom<InventoryItem>.
					InnerJoin<INItemXRef>.On<INItemXRef.FK.InventoryItem>.
					Where<
						INItemXRef.alternateID.IsEqual<@P.AsString>.
						And<INItemXRef.alternateType.IsEqual<INAlternateType.barcode>>.
						And<InventoryItem.itemStatus.IsNotIn<InventoryItemStatus.inactive, InventoryItemStatus.markedForDeletion>>>.
					OrderBy<INItemXRef.alternateType.Asc>.
					View.ReadOnly
					.Select(Basis, barcode);

				if (item == null)
				{
					var inventory = IN.InventoryItem.UK.Find(Basis, barcode);
					if (inventory != null && inventory.ItemStatus.IsNotIn(InventoryItemStatus.Inactive, InventoryItemStatus.MarkedForDeletion))
						return inventory;
				}

				return item;
			}

			protected override AbsenceHandling.Of<InventoryItem> HandleAbsence(string barcode)
			{
				if (Basis.TryProcessBy<WarehouseState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication))
					return AbsenceHandling.Done;
				return base.HandleAbsence(barcode);
			}

			protected override void ReportMissing(string barcode) => Basis.Reporter.Error(Msg.Missing, barcode);

			protected override void Apply(InventoryItem entity)
			{
				Basis.InventoryID = entity.InventoryID;
				Basis.NoteID = entity.NoteID;

				Basis.Graph.Filter.Cache.SetValueExt<InventorySummaryEnqFilter.inventoryID>(Basis.Graph.Filter.Current, entity.InventoryID);
				Basis.Graph.Filter.UpdateCurrent();
			}
			protected override void ClearState()
			{
				Basis.InventoryID = null;
				Basis.NoteID = null;

				Basis.Graph.Filter.Cache.SetValueExt<InventorySummaryEnqFilter.inventoryID>(Basis.Graph.Filter.Current, null);
				Basis.Graph.Filter.UpdateCurrent();
			}

			protected override void ReportSuccess(InventoryItem entity) => Basis.Reporter.Info(Msg.Ready, entity.InventoryCD.Trim());
			protected override void SetNextState() { }

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string Prompt = "Scan the barcode of the item.";
				public const string Ready = "The {0} item is selected.";
				public const string Missing = "The {0} item barcode is not found.";
			}
			#endregion
		}
		#endregion

		#region Redirect
		public new sealed class RedirectFrom<TForeignBasis> : WMSBase.RedirectFrom<TForeignBasis>
			where TForeignBasis : PXGraphExtension, IBarcodeDrivenStateMachine
		{
			public override string Code => "ITEM";
			public override string DisplayName => Msg.ModeDescription;

			public override bool IsPossible => PXAccess.FeatureInstalled<CS.FeaturesSet.wMSInventory>();
		}
		#endregion

		#region Messages
		[PXLocalizable]
		public new abstract class Msg : WMSBase.Msg
		{
			public const string ModeDescription = "Item Lookup";
		}
		#endregion
	}

	public sealed class ItemLookupScanHeader : PXCacheExtension<ScanHeader>
	{
		#region SiteID
		[Site(Enabled = false)]
		public int? SiteID { get; set; }
		public abstract class siteID : BqlInt.Field<siteID> { }
		#endregion
		#region InventoryID
		[StockItem(Enabled = false)]
		public int? InventoryID { get; set; }
		public abstract class inventoryID : BqlInt.Field<inventoryID> { }
		#endregion
	}

	public class InventoryLookupUnassignedLocationFix : PXGraphExtension<InventoryItemLookup.Host>
	{
		[PXOverride]
		public virtual void InventorySummaryEnquiryResult_LocationID_FieldSelecting(PXCache cache, PXFieldSelectingEventArgs e, Action<PXCache, PXFieldSelectingEventArgs> baseMethod)
		{
			string locationName = null;

			switch (e.ReturnValue)
			{
				case null:
					locationName = PXMessages.LocalizeNoPrefix(Messages.Unassigned);
					break;
				case InventorySummaryEnquiryResult.TotalLocationID:
					locationName = PXMessages.LocalizeNoPrefix(Messages.TotalLocation);
					break;
				default:
					locationName = INLocation.PK.Find(Base, e.ReturnValue as int?).With(loc => Base.IsMobile ? (loc.Descr ?? loc.LocationCD) : loc.LocationCD);
					break;
			}

			if (locationName != null)
			{
				e.ReturnState = PXFieldState.CreateInstance(locationName, typeof(string), false, null, null, null, null, null,
					nameof(InventorySummaryEnquiryResult.locationID), null, GetLocationDisplayName(), null, PXErrorLevel.Undefined, null, null, null, PXUIVisibility.Undefined, null, null, null);
				e.Cancel = true;
			}

			string GetLocationDisplayName()
			{
				var displayName = PXUIFieldAttribute.GetDisplayName<InventorySummaryEnquiryResult.locationID>(cache);
				if (displayName != null) displayName = PXMessages.LocalizeNoPrefix(displayName);

				return displayName;
			}
		}

		[PXDBInt]
		[PXUIField(DisplayName = "Location", Visibility = PXUIVisibility.Visible, FieldClass = LocationAttribute.DimensionName)]
		protected virtual void _(Events.CacheAttached<InventorySummaryEnquiryResult.locationID> e) { }
	}
}