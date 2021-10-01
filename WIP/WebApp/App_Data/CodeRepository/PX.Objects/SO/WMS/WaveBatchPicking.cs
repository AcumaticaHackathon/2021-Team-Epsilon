using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.BarcodeProcessing;

using PX.Objects.Common;
using PX.Objects.Extensions;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.WMS;

namespace PX.Objects.SO.WMS
{
	using WMSBase = WarehouseManagementSystem<PickPackShip, PickPackShip.Host>;

	public class WaveBatchPicking : PickPackShip.ScanExtension
	{
		public static bool IsActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.wMSAdvancedPicking>();

		/// <summary>
		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.CreateScanModes"/>
		/// </summary>
		[PXOverride]
		public virtual IEnumerable<ScanMode<PickPackShip>> CreateScanModes(Func<IEnumerable<ScanMode<PickPackShip>>> base_CreateScanModes)
		{
			foreach (var mode in base_CreateScanModes())
				yield return mode;
			yield return new WavePickMode();
			yield return new BatchPickMode();
		}

		public sealed class WavePickMode : PickPackShip.ScanMode
		{
			public const string Value = "WAVE";
			public class value : BqlString.Constant<value> { public value() : base(WavePickMode.Value) { } }

			public WaveBatchPicking WBBasis => Get<WaveBatchPicking>();

			public override string Code => Value;
			public override string Description => Msg.DisplayName;

			#region State Machine
			protected override IEnumerable<ScanState<PickPackShip>> CreateStates()
			{
				yield return new PickListState() { WorksheetType = SOPickingWorksheet.worksheetType.Wave };
				yield return new AssignToteState();
				yield return new WMSBase.LocationState().With(WBBasis.DecorateLocationState);
				yield return new WMSBase.InventoryItemState() { AlternateType = INPrimaryAlternateType.CPN, IsForIssue = true }.With(WBBasis.DecorateInventoryState);
				yield return new WMSBase.LotSerialState().With(WBBasis.DecorateLotSerialState);
				yield return new WMSBase.ExpireDateState() { IsForIssue = true }.With(WBBasis.DecorateExpireDateState);
				yield return new ConfirmToteState();
				yield return new ConfirmState();

				// directly set state
				yield return new RemoveFromToteState();
			}

			protected override IEnumerable<ScanTransition<PickPackShip>> CreateTransitions()
			{
				return StateFlow(flow => flow
					.From<PickListState>()
					.NextTo<AssignToteState>()
					.NextTo<WMSBase.LocationState>()
					.NextTo<WMSBase.InventoryItemState>()
					.NextTo<WMSBase.LotSerialState>()
					.NextTo<WMSBase.ExpireDateState>()
					.NextTo<ConfirmToteState>());
			}

			protected override IEnumerable<ScanCommand<PickPackShip>> CreateCommands()
			{
				yield return new WMSBase.RemoveCommand()
					.Intercept.Process.ByOverride((basis, base_Process) =>
					{
						bool result = base_Process();
						basis.SetScanState<RemoveFromToteState>();
						return result;
					});
				yield return new WMSBase.QtySupport.SetQtyCommand();
				yield return new ConfirmPickListCommand();
			}

			protected override IEnumerable<ScanRedirect<PickPackShip>> CreateRedirects() => AllWMSRedirects.CreateFor<PickPackShip>();

			protected override void ResetMode(bool fullReset)
			{
				base.ResetMode(fullReset);
				Clear<PickListState>(when: fullReset && !Basis.IsWithinReset);
				Clear<WMSBase.LocationState>(when: fullReset || Basis.PromptLocationForEveryLine);
				Clear<WMSBase.InventoryItemState>();
				Clear<WMSBase.LotSerialState>();
				Clear<WMSBase.ExpireDateState>();
				Clear<RemoveFromToteState>();
			}
			#endregion

			#region Logic
			public class Logic : Extension<Logic>
			{
				public static bool IsActive() => WaveBatchPicking.IsActive(); // TODO: remove once N-nd level extensions start to be inactive if any of their "parents" are inactive

				public virtual SOPickerToShipmentLink NextShipmentWithoutTote => WBBasis.ShipmentsOfPicker.SelectMain().FirstOrDefault(s => s.ToteID == null);

				public virtual bool ConfirmToteForEveryLine =>
					Basis.Setup.Current.ConfirmToteForEachItem == true &&
					Basis.Remove == false &&
					WBBasis.WorksheetNbr != null &&
					WBBasis.Worksheet.Current?.WorksheetType == SOPickingWorksheet.worksheetType.Wave &&
					WBBasis.ShipmentsOfPicker.Select().Count > 1;

				public virtual INTote GetToteForPickListEntry(SOPickerListEntry selectedSplit)
				{
					if (selectedSplit == null)
						return null;

					INTote tote =
						SelectFrom<INTote>.
						InnerJoin<SOPickerToShipmentLink>.On<SOPickerToShipmentLink.FK.Tote>.
						Where<
							SOPickerToShipmentLink.worksheetNbr.IsEqual<@P.AsString>.
							And<SOPickerToShipmentLink.pickerNbr.IsEqual<@P.AsInt>>.
							And<SOPickerToShipmentLink.shipmentNbr.IsEqual<@P.AsString>>>.
						View.Select(Basis, selectedSplit.WorksheetNbr, selectedSplit.PickerNbr, selectedSplit.ShipmentNbr);
					return tote;
				}
			}
			#endregion

			#region States
			public abstract class ToteState : PickPackShip.EntityState<INTote>
			{
				public WaveBatchPicking WBBasis => Get<WaveBatchPicking>();
				public WavePickMode.Logic Mode => Get<WavePickMode.Logic>();

				protected override INTote GetByBarcode(string barcode) => INTote.UK.Find(Basis, Basis.SiteID, barcode);

				protected override Validation Validate(INTote tote)
				{
					if (tote.Active == false)
						return Validation.Fail(Msg.Inactive, tote.ToteCD);

					return base.Validate(tote);
				}

				protected override void ReportMissing(string barcode) => Basis.Reporter.Error(Msg.Missing, barcode);

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string Ready = "The {0} tote is selected.";
					public const string Missing = "The {0} tote is not found.";
					public const string Inactive = "The {0} tote is inactive.";
				}
				#endregion
			}

			public sealed class AssignToteState : ToteState
			{
				public const string Value = "ASST";
				public class value : BqlString.Constant<value> { public value() : base(AssignToteState.Value) { } }

				protected string shipmentJustAssignedWithTote;

				public override string Code => Value;
				protected override string StatePrompt => Basis.Localize(Msg.Prompt, Mode.NextShipmentWithoutTote?.ShipmentNbr);

				protected override bool IsStateSkippable() => Mode.NextShipmentWithoutTote == null;

				protected override AbsenceHandling.Of<INTote> HandleAbsence(string barcode)
				{
					if (Basis.Get<Logic>().TryAssignTotesFromCart(barcode))
						return AbsenceHandling.Done;

					return base.HandleAbsence(barcode);
				}

				protected override Validation Validate(INTote tote)
				{
					if (Basis.HasFault(tote, base.Validate, out var fault))
						return fault;

					if (tote.AssignedCartID != null)
						return Validation.Fail(Msg.CannotBeUsedSeparatly, tote.ToteCD);

					bool toteIsBusy =
						SelectFrom<SOPickerToShipmentLink>.
						InnerJoin<INTote>.On<SOPickerToShipmentLink.FK.Tote>.
						InnerJoin<SOShipment>.On<SOPickerToShipmentLink.FK.Shipment>.
						Where<
							INTote.siteID.IsEqual<@P.AsInt>.
							And<INTote.toteID.IsEqual<@P.AsInt>>.
							And<SOShipment.confirmed.IsEqual<False>>>.
						View.Select(Basis, tote.SiteID, tote.ToteID).Any();
					if (toteIsBusy)
						return Validation.Fail(Msg.Busy, tote.ToteCD);

					return Validation.Ok;
				}

				protected override void Apply(INTote tote)
				{
					var shipment = Mode.NextShipmentWithoutTote;
					shipment.ToteID = tote.ToteID;
					WBBasis.ShipmentsOfPicker.Update(shipment);
					shipmentJustAssignedWithTote = shipment.ShipmentNbr;

					if (WBBasis.Picker.Current.UserID == null)
					{
						WBBasis.Picker.Current.UserID = Basis.Graph.Accessinfo.UserID;
						WBBasis.Picker.UpdateCurrent();
					}
					Basis.SaveChanges();
				}

				protected override void ReportSuccess(INTote tote) => Basis.Reporter.Info(Msg.Ready, tote.ToteCD, shipmentJustAssignedWithTote);

				protected override void SetNextState()
				{
					if (Mode.NextShipmentWithoutTote != null)
						Basis.SetScanState<AssignToteState>(); // set the same state to change the prompt message
					else
						base.SetNextState();
				}

				#region Logic
				public class Logic : Extension<Logic>
				{
					public static bool IsActive() => WaveBatchPicking.IsActive(); // TODO: remove once N-nd level extensions start to be inactive if any of their "parents" are inactive

					public virtual bool TryAssignTotesFromCart(string barcode)
					{
						INCart cart =
							SelectFrom<INCart>.
							InnerJoin<INSite>.On<INCart.FK.Site>.
							Where<
								INCart.siteID.IsEqual<WMSScanHeader.siteID.FromCurrent>.
								And<INCart.cartCD.IsEqual<@P.AsString>>.
								And<Match<INSite, AccessInfo.userName.FromCurrent>>>.
							View.Select(Basis, barcode);
						if (cart == null)
							return false;

						var shipmentsOfPicker = WBBasis.ShipmentsOfPicker.SelectMain();
						if (shipmentsOfPicker.Any(s => s.ToteID != null))
						{
							Basis.Reporter.Error(Msg.ToteAlreadyAssignedCannotAssignCart, cart.CartCD);
							return true;
						}

						bool cartIsBusy =
							SelectFrom<SOPickerToShipmentLink>.
							InnerJoin<SOPickingWorksheet>.On<SOPickerToShipmentLink.FK.Worksheet>.
							InnerJoin<INTote>.On<SOPickerToShipmentLink.FK.Tote>.
							InnerJoin<INCart>.On<INTote.FK.Cart>.
							InnerJoin<SOShipment>.On<SOPickerToShipmentLink.FK.Shipment>.
							Where<
								SOPickingWorksheet.worksheetType.IsEqual<SOPickingWorksheet.worksheetType.wave>.
								And<SOShipment.confirmed.IsEqual<False>>.
								And<INCart.siteID.IsEqual<@P.AsInt>>.
								And<INCart.cartID.IsEqual<@P.AsInt>>>.
							View.ReadOnly.Select(Base, cart.SiteID, cart.CartID).Any();
						if (cartIsBusy)
						{
							Basis.Reporter.Error(PPSCartSupport.CartState.Msg.IsOccupied, cart.CartCD);
							return true;
						}

						var totes = INTote.FK.Cart.SelectChildren(Base, cart).Where(t => t.Active == true).ToArray();
						if (shipmentsOfPicker.Length > totes.Length)
						{
							Basis.Reporter.Error(Msg.TotesAreNotEnoughInCart, cart.CartCD);
							return true;
						}

						foreach (var (link, tote) in shipmentsOfPicker.Zip(totes, (link, tote) => (link, tote)))
						{
							link.ToteID = tote.ToteID;
							WBBasis.ShipmentsOfPicker.Update(link);
						}
						WBBasis.Picker.Current.CartID = cart.CartID;
						WBBasis.Picker.UpdateCurrent();

						Basis.SaveChanges();

						if (Basis.Get<PPSCartSupport>() is PPSCartSupport cartSup)
							cartSup.CartID = cart.CartID;

						Basis.DispatchNext(Msg.TotesFromCartAreAssigned, shipmentsOfPicker.Length, cart.CartCD);
						return true;
					}
				}
				#endregion

				#region Messages
				[PXLocalizable]
				public new abstract class Msg : ToteState.Msg
				{
					public const string Prompt = "Scan the tote barcode for the {0} shipment.";
					public new const string Ready = "The {0} tote is selected for the {1} shipment.";

					public const string CannotBeUsedSeparatly = "The {0} tote cannot be used separately from the cart.";
					public const string Busy = "The {0} tote cannot be selected because it is already assigned to another shipment.";

					public const string ToteAlreadyAssignedCannotAssignCart = "Totes from the {0} cart cannot be auto assigned to the pick list because it already has manual assignments.";
					public const string TotesAreNotEnoughInCart = "There are not enough active totes in the {0} cart to assign them to all of the shipments of the pick list.";
					public const string TotesFromCartAreAssigned = "The {0} first totes from the {1} cart were automatically assigned to the shipments of the pick list.";
				}
				#endregion
			}

			public sealed class RemoveFromToteState : ToteState
			{
				public const string Value = "RMFT";
				public class value : BqlString.Constant<value> { public value() : base(RemoveFromToteState.Value) { } }

				public override string Code => Value;
				protected override string StatePrompt => Msg.Prompt;

				protected override void Apply(INTote tote) => WBBasis.RemoveFromToteID = tote.ToteID;
				protected override void ClearState() => WBBasis.RemoveFromToteID = null;

				protected override void ReportSuccess(INTote tote) => Basis.Reporter.Info(Msg.Ready, tote.ToteCD);

				// TODO: replace with transition
				protected override void SetNextState() => Basis.SetScanState<PickPackShip.LocationState>();

				#region Messages
				[PXLocalizable]
				public new abstract class Msg : ToteState.Msg
				{
					public const string Prompt = "Scan the barcode of a tote from which you want to remove the items.";
				}
				#endregion
			}

			public sealed class ConfirmToteState : ToteState
			{
				public const string Value = "CNFT";
				public class value : BqlString.Constant<value> { public value() : base(ConfirmToteState.Value) { } }

				private INTote ProperTote => WBBasis.GetSelectedPickListEntry().With(Mode.GetToteForPickListEntry);

				public override string Code => Value;
				protected override string StatePrompt => Basis.Localize(Msg.Prompt, ProperTote?.ToteCD);

				protected override bool IsStateActive() => Mode.ConfirmToteForEveryLine;
				protected override bool IsStateSkippable() => ProperTote == null;

				protected override Validation Validate(INTote tote)
				{
					if (Basis.HasFault(tote, base.Validate, out var fault))
						return fault;

					if (ProperTote.ToteID != tote.ToteID)
						return Validation.Fail(Msg.Mismatch, tote.ToteCD);

					return Validation.Ok;
				}

				protected override void ReportSuccess(INTote tote) => Basis.Reporter.Info(Msg.Ready, tote.ToteCD);

				#region Messages
				[PXLocalizable]
				public new abstract class Msg : ToteState.Msg
				{
					public const string Prompt = "Scan the barcode of the {0} tote to confirm picking of the items.";
					public const string Mismatch = "Incorrect tote barcode ({0}) has been scanned.";
				}
				#endregion
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg : PickPackShip.ScanMode.Msg
			{
				public const string DisplayName = "Wave Pick";
			}
			#endregion
		}

		public sealed class BatchPickMode : PickPackShip.ScanMode
		{
			public const string Value = "BTCH";
			public class value : BqlString.Constant<value> { public value() : base(BatchPickMode.Value) { } }

			public WaveBatchPicking WBBasis => Get<WaveBatchPicking>();

			public override string Code => Value;
			public override string Description => Msg.DisplayName;

			#region State Machine
			protected override IEnumerable<ScanState<PickPackShip>> CreateStates()
			{
				yield return new PickListState() { WorksheetType = SOPickingWorksheet.worksheetType.Batch };
				yield return new WMSBase.LocationState().With(WBBasis.DecorateLocationState);
				yield return new WMSBase.InventoryItemState() { AlternateType = INPrimaryAlternateType.CPN, IsForIssue = true }.With(WBBasis.DecorateInventoryState);
				yield return new WMSBase.LotSerialState().With(WBBasis.DecorateLotSerialState);
				yield return new WMSBase.ExpireDateState().With(WBBasis.DecorateExpireDateState);
				yield return new ConfirmState();

				if (Get<PPSCartSupport>() is PPSCartSupport cartSupport && cartSupport.IsCartRequired())
				{
					yield return new PPSCartSupport.CartState()
						.Intercept.Apply.ByAppend((basis, cart) =>
						{
							var mode = basis.Get<WaveBatchPicking>();
							if (mode.Picker.Current != null)
							{
								mode.Picker.Current.CartID = cart.CartID;
								mode.Picker.UpdateCurrent();
							}
						});
				}

				// directly set state
				yield return new SortingLocationState();
			}

			protected override IEnumerable<ScanTransition<PickPackShip>> CreateTransitions()
			{
				var cartSupport = Get<PPSCartSupport>();
				if (cartSupport != null && cartSupport.IsCartRequired())
				{ // With Cart
					return StateFlow(flow => flow
						.From<PickListState>()
						.NextTo<PPSCartSupport.CartState>()
						.NextTo<PickPackShip.LocationState>()
						.NextTo<PickPackShip.InventoryItemState>()
						.NextTo<PickPackShip.LotSerialState>()
						.NextTo<PickPackShip.ExpireDateState>());
				}
				else
				{ // No Cart
					return StateFlow(flow => flow
						.From<PickListState>()
						.NextTo<PickPackShip.LocationState>()
						.NextTo<PickPackShip.InventoryItemState>()
						.NextTo<PickPackShip.LotSerialState>()
						.NextTo<PickPackShip.ExpireDateState>());
				}
			}

			protected override IEnumerable<ScanCommand<PickPackShip>> CreateCommands()
			{
				yield return new PickPackShip.RemoveCommand();
				yield return new PickPackShip.QtySupport.SetQtyCommand();
				yield return new ConfirmPickListCommand();
			}

			protected override IEnumerable<ScanRedirect<PickPackShip>> CreateRedirects() => AllWMSRedirects.CreateFor<PickPackShip>();

			protected override void ResetMode(bool fullReset)
			{
				base.ResetMode(fullReset);
				Clear<PickListState>(when: fullReset && !Basis.IsWithinReset);
				Clear<PPSCartSupport.CartState>(when: fullReset && !Basis.IsWithinReset);
				Clear<PickPackShip.LocationState>(when: fullReset || Basis.PromptLocationForEveryLine);
				Clear<PickPackShip.InventoryItemState>();
				Clear<PickPackShip.LotSerialState>();
				Clear<PickPackShip.ExpireDateState>();
			}
			#endregion

			#region States
			public sealed class SortingLocationState : PickPackShip.EntityState<INLocation>
			{
				public const string Value = "SLOC";
				public class value : BqlString.Constant<value> { public value() : base(SortingLocationState.Value) { } }

				public override string Code => Value;
				protected override string StatePrompt => Msg.Prompt;

				protected override INLocation GetByBarcode(string barcode)
				{
					return
						SelectFrom<INLocation>.
						Where<
							INLocation.siteID.IsEqual<@P.AsInt>.
							And<INLocation.locationCD.IsEqual<@P.AsString>>>.
						View.Select(Basis, Basis.SiteID, barcode);
				}

				protected override Validation Validate(INLocation location)
				{
					if (location.Active != true)
						return Validation.Fail(IN.Messages.InactiveLocation, location.LocationCD);

					if (location.IsSorting != true)
						return Validation.Fail(Msg.NotSorting, location.LocationCD);

					return Validation.Ok;
				}

				protected override void Apply(INLocation location) => Basis.Get<ConfirmPickListCommand.Logic>().ConfirmPickList(location.LocationID.Value);

				protected override void ReportSuccess(INLocation location) => Basis.Reporter.Info(Msg.Ready, location.LocationCD);
				protected override void ReportMissing(string barcode) => Basis.Reporter.Error(Msg.Missing, barcode, Basis.SightOf<WMSScanHeader.siteID>());

				protected override void SetNextState() { }

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string Prompt = "Scan the sorting location.";
					public const string Ready = "The {0} sorting location is selected.";
					public const string Missing = PickPackShip.LocationState.Msg.Missing;
					public const string NotSorting = "The {0} location cannot be selected because it is not a sorting location.";
				}
				#endregion
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg : PickPackShip.ScanMode.Msg
			{
				public const string DisplayName = "Batch Pick";
			}
			#endregion
		}

		#region Views
		public
			SelectFrom<SOPickingWorksheet>.
			Where<SOPickingWorksheet.worksheetNbr.IsEqual<WaveBatchScanHeader.worksheetNbr.FromCurrent.NoDefault>>.
			View Worksheet;

		public
			SelectFrom<SOPicker>.
			Where<
				SOPicker.worksheetNbr.IsEqual<WaveBatchScanHeader.worksheetNbr.FromCurrent.NoDefault>.
				And<SOPicker.pickerNbr.IsEqual<WaveBatchScanHeader.pickerNbr.FromCurrent.NoDefault>>>.
			View Picker;

		public
			SelectFrom<SOPickerToShipmentLink>.
			Where<SOPickerToShipmentLink.FK.Picker.SameAsCurrent>.
			View ShipmentsOfPicker;

		public
			SelectFrom<SOPickerListEntry>.
			InnerJoin<SOPicker>.On<SOPickerListEntry.FK.Picker>.
			InnerJoin<INLocation>.On<SOPickerListEntry.FK.Location>.
			LeftJoin<SOPickerToShipmentLink>.On<
				SOPickerToShipmentLink.FK.Picker.
				And<SOPickerToShipmentLink.shipmentNbr.IsEqual<SOPickerListEntry.shipmentNbr>>>.
			LeftJoin<INTote>.On<SOPickerToShipmentLink.FK.Tote>.
			Where<SOPickerListEntry.FK.Picker.SameAsCurrent>.
			OrderBy<
				INLocation.pathPriority.Asc,
				INLocation.locationCD.Asc>.
			View PickListOfPicker;
		#endregion

		#region Buttons
		public PXAction<ScanHeader> ReviewPickWS;
		[PXButton, PXUIField(DisplayName = "Review")]
		protected virtual IEnumerable reviewPickWS(PXAdapter adapter) => adapter.Get();
		#endregion

		#region State
		public WaveBatchScanHeader WBHeader => Basis.Header.Get<WaveBatchScanHeader>() ?? new WaveBatchScanHeader();
		public ValueSetter<ScanHeader>.Ext<WaveBatchScanHeader> WBSetter => Basis.HeaderSetter.With<WaveBatchScanHeader>();

		#region WorksheetNbr
		public string WorksheetNbr
		{
			get => WBHeader.WorksheetNbr;
			set => WBSetter.Set(h => h.WorksheetNbr, value);
		}
		#endregion
		#region PickerNbr
		public Int32? PickerNbr
		{
			get => WBHeader.PickerNbr;
			set => WBSetter.Set(h => h.PickerNbr, value);
		}
		#endregion
		#region RemoveFromToteID
		public int? RemoveFromToteID
		{
			get => WBHeader.RemoveFromToteID;
			set => WBSetter.Set(h => h.RemoveFromToteID, value);
		}
		#endregion
		#endregion

		#region Event Handlers
		protected virtual void _(Events.RowSelected<ScanHeader> e)
		{
			if (e.Row == null)
				return;

			ReviewPickWS.SetVisible(Base.IsMobile && e.Row.Mode.IsIn(WavePickMode.Value, BatchPickMode.Value));

			PickListOfPicker.Cache.AdjustUI()
				.For<SOPickerListEntry.shipmentNbr>(a => a.Visible = Worksheet.Current?.WorksheetType == SOPickingWorksheet.worksheetType.Wave);

			if (String.IsNullOrEmpty(WorksheetNbr))
			{
				Worksheet.Current = null;
				Picker.Current = null;
			}
			else
			{
				Worksheet.Current = Worksheet.Search<SOPickingWorksheet.worksheetNbr>(WorksheetNbr);
				Picker.Current = Picker.Search<SOPicker.worksheetNbr>(WorksheetNbr);
			}
		}
		#endregion

		#region Logic
		public virtual SOPicker PickList => SOPicker.PK.Find(Basis, WorksheetNbr, PickerNbr);

		public string ShipmentSpecialPickType
		{
			get
			{
				return
					Basis.Shipment is SOShipment sh &&
					sh.PickedViaWorksheet == true &&
					sh.CurrentWorksheetNbr != null &&
					SOPickingWorksheet.PK.Find(Base, sh.CurrentWorksheetNbr) is SOPickingWorksheet ws
					? ws.WorksheetType
					: null;
			}
		}

		public virtual bool IsLocationMissing(INLocation location, out Validation error)
		{
			if (PickListOfPicker.SelectMain().All(t => t.LocationID != location.LocationID))
			{
				error = Validation.Fail(Msg.LocationMissingInPickList, location.LocationCD);
				return true;
			}
			else
			{
				error = Validation.Ok;
				return false;
			}
		}

		public virtual bool IsItemMissing(PXResult<INItemXRef, InventoryItem> item, out Validation error)
		{
			(INItemXRef xref, InventoryItem inventoryItem) = item;
			if (PickListOfPicker.SelectMain().All(t => t.InventoryID != inventoryItem.InventoryID))
			{
				error = Validation.Fail(Msg.InventoryMissingInPickList, inventoryItem.InventoryCD);
				return true;
			}
			else
			{
				error = Validation.Ok;
				return false;
			}
		}

		public virtual bool IsLotSerialMissing(string lotSerialNbr, out Validation error)
		{
			if (Basis.IsEnterableLotSerial(isForIssue: true) == false && PickListOfPicker.SelectMain().All(t => t.LotSerialNbr != lotSerialNbr))
			{
				error = Validation.Fail(Msg.LotSerialMissingInPickList, lotSerialNbr);
				return true;
			}
			else
			{
				error = Validation.Ok;
				return false;
			}
		}

		public virtual SOPickerListEntry GetSelectedPickListEntry()
		{
			bool remove = Basis.Remove == true;
			var pickedSplit = PickListOfPicker
				.Select().AsEnumerable()
				.With(view => remove && RemoveFromToteID != null
					? view.Where(e => e.GetItem<SOPickerToShipmentLink>().ToteID == RemoveFromToteID)
					: view)
				.Select(row =>
				(
					Split: row.GetItem<SOPickerListEntry>(),
					Location: row.GetItem<INLocation>()
				))
				.Where(r => IsSelectedSplit(r.Split))
				.OrderByDescending(r => r.Split.IsUnassigned == false && remove ? r.Split.PickedQty > 0 : r.Split.Qty > r.Split.PickedQty)
				.ThenByDescending(r => remove ? r.Split.PickedQty > 0 : r.Split.Qty > r.Split.PickedQty)
				.ThenByDescending(r => r.Split.LotSerialNbr == (Basis.LotSerialNbr ?? r.Split.LotSerialNbr))
				.ThenByDescending(r => string.IsNullOrEmpty(r.Split.LotSerialNbr))
				.ThenByDescending(r => (r.Split.Qty > r.Split.PickedQty || remove) && r.Split.PickedQty > 0)
				.ThenBy(r => Sign.MinusIf(remove) * r.Location.PathPriority)
				.With(view => remove
					? view.ThenByDescending(r => r.Location.LocationCD)
					: view.ThenBy(r => r.Location.LocationCD))
				.ThenByDescending(r => Sign.MinusIf(remove) * (r.Split.Qty - r.Split.PickedQty))
				.Select(r => r.Split)
				.FirstOrDefault();
			return pickedSplit;
		}

		public virtual bool IsSelectedSplit(SOPickerListEntry split)
		{
			return
				split.InventoryID == Basis.InventoryID &&
				split.SubItemID == Basis.SubItemID &&
				split.SiteID == Basis.SiteID &&
				split.LocationID == (Basis.LocationID ?? split.LocationID) &&
				(split.LotSerialNbr == (Basis.LotSerialNbr ?? split.LotSerialNbr) ||
					Basis.Remove == false &&
					Basis.IsEnterableLotSerial(isForIssue: true));
		}

		public virtual bool SetLotSerialNbrAndQty(SOPickerListEntry pickedSplit, decimal qty)
		{
			var existingAssignedSplit = pickedSplit.IsUnassigned == true && Basis.SelectedLotSerialClass.LotSerTrack != INLotSerTrack.SerialNumbered
				? PickListOfPicker.SelectMain().FirstOrDefault(s => s.IsUnassigned == false && s.LotSerialNbr == (Basis.LotSerialNbr ?? s.LotSerialNbr) && IsSelectedSplit(s))
				: null;

			if (existingAssignedSplit != null)
			{
				existingAssignedSplit.PickedQty += qty;
				if (existingAssignedSplit.PickedQty > existingAssignedSplit.Qty)
					existingAssignedSplit.Qty = existingAssignedSplit.PickedQty;

				existingAssignedSplit = PickListOfPicker.Update(existingAssignedSplit);
			}
			else
			{
				var newSplit = PXCache<SOPickerListEntry>.CreateCopy(pickedSplit);

				newSplit.EntryNbr = null;
				newSplit.LotSerialNbr = Basis.LotSerialNbr;
				newSplit.Qty = qty;
				newSplit.PickedQty = qty;
				newSplit.IsUnassigned = false;
				if (Basis.SelectedLotSerialClass.LotSerTrackExpiration == true && Basis.ExpireDate != null)
					newSplit.ExpireDate = Basis.ExpireDate;

				newSplit = PickListOfPicker.Insert(newSplit);
			}

			return true;
		}

		public virtual bool CanWBPick => PickListOfPicker.SelectMain().Any(s => s.PickedQty < s.Qty);
		#endregion

		#region States
		public sealed class PickListState : PickPackShip.RefNbrState<PXResult<SOPickingWorksheet, SOPicker>>
		{
			private int _pickerNbr;

			public WaveBatchPicking WBBasis => Basis.Get<WaveBatchPicking>();

			public string WorksheetType { get; set; }

			protected override string StatePrompt => Msg.Prompt;
			protected override bool IsStateSkippable() => base.IsStateSkippable() || (WBBasis.WorksheetNbr != null && Basis.Header.ProcessingSucceeded != true);

			protected override PXResult<SOPickingWorksheet, SOPicker> GetByBarcode(string barcode)
			{
				if (barcode.Contains("/") == false)
					return null;

				(string worksheetNbr, string pickerNbrStr) = barcode.Split('/');
				_pickerNbr = int.Parse(pickerNbrStr);

				var doc = (PXResult<SOPickingWorksheet, INSite, SOPicker>)
					SelectFrom<SOPickingWorksheet>.
					InnerJoin<INSite>.On<SOPickingWorksheet.FK.Site>.
					LeftJoin<SOPicker>.On<SOPicker.FK.Worksheet.And<SOPicker.pickerNbr.IsEqual<@P.AsInt>>>.
					Where<
						SOPickingWorksheet.worksheetNbr.IsEqual<@P.AsString>.
						And<SOPickingWorksheet.worksheetType.IsEqual<@P.AsString>>.
						And<Match<INSite, AccessInfo.userName.FromCurrent>>>.
					View.Select(Basis, _pickerNbr, worksheetNbr, WorksheetType);

				if (doc != null)
					return new PXResult<SOPickingWorksheet, SOPicker>(doc, doc);
				else
					return null;
			}

			protected override AbsenceHandling.Of<PXResult<SOPickingWorksheet, SOPicker>> HandleAbsence(string barcode)
			{
				if (Basis.FindMode<PickPackShip.PickMode>().TryProcessBy<PickPackShip.PickMode.ShipmentState>(barcode))
				{
					Basis.SetScanMode<PickPackShip.PickMode>();
					Basis.FindState<PickPackShip.PickMode.ShipmentState>().Process(barcode);
					return AbsenceHandling.Done;
				}
				else
					return base.HandleAbsence(barcode);
			}

			protected override Validation Validate(PXResult<SOPickingWorksheet, SOPicker> pickList)
			{
				(var worksheet, var picker) = pickList;

				if (worksheet.Status.IsNotIn(SOPickingWorksheet.status.Picking, SOPickingWorksheet.status.Open))
					return Validation.Fail(Msg.InvalidStatus, worksheet.WorksheetNbr, Basis.SightOf<SOPickingWorksheet.status>(worksheet));

				if (Basis.Get<PPSCartSupport>() is PPSCartSupport cartSup && cartSup.CartID != null && worksheet.SiteID != Basis.SiteID)
					return Validation.Fail(Msg.InvalidSite, worksheet.WorksheetNbr);

				if (picker?.PickerNbr == null)
					return Validation.Fail(Msg.PickerPositionMissing, _pickerNbr, worksheet.WorksheetNbr);

				if (picker.UserID.IsNotIn(null, Basis.Graph.Accessinfo.UserID))
					return Validation.Fail(Msg.PickerPositionOccupied, picker.PickerNbr, worksheet.WorksheetNbr);

				return base.Validate(pickList);
			}

			protected override void Apply(PXResult<SOPickingWorksheet, SOPicker> pickList)
			{
				(var worksheet, var picker) = pickList;

				Basis.RefNbr = null;
				Basis.Graph.Document.Current = null;

				WBBasis.WorksheetNbr = worksheet.WorksheetNbr;
				WBBasis.Worksheet.Current = worksheet;
				WBBasis.PickerNbr = picker.PickerNbr;
				WBBasis.Picker.Current = picker;

				Basis.SiteID = worksheet.SiteID;
				Basis.TranDate = worksheet.PickDate;
				Basis.NoteID = worksheet.NoteID;
			}

			protected override void ClearState()
			{
				WBBasis.WorksheetNbr = null;
				WBBasis.Worksheet.Current = null;
				WBBasis.PickerNbr = null;
				WBBasis.Picker.Current = null;

				Basis.SiteID = null;
				Basis.TranDate = null;
				Basis.NoteID = null;
			}

			protected override void ReportMissing(string barcode) => Basis.ReportError(Msg.Missing, barcode);
			protected override void ReportSuccess(PXResult<SOPickingWorksheet, SOPicker> pickList) => Basis.ReportInfo(Msg.Ready, pickList.GetItem<SOPicker>().PickListNbr);

			protected override void SetNextState()
			{
				if (Basis.Remove == false && WBBasis.CanWBPick == false)
					Basis.SetScanState(BuiltinScanStates.Command, WaveBatchPicking.Msg.Completed, WBBasis.Picker.Current.PickListNbr);
				else
					base.SetNextState();
			}

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string Prompt = "Scan the picking worksheet number.";
				public const string Ready = "The {0} picking worksheet is loaded and ready to be processed.";
				public const string Missing = "The {0} picking worksheet is not found.";

				public const string InvalidStatus = "The {0} picking worksheet cannot be processed because it has the {1} status.";
				public const string InvalidSite = "The warehouse specified in the {0} picking worksheet differs from the warehouse assigned to the selected cart.";

				public const string PickerPositionMissing = "The picker slot {0} is not found in the {1} picking worksheet.";
				public const string PickerPositionOccupied = "The picker slot {0} is already assigned to another user in the {1} picking worksheet.";
			}
			#endregion
		}

		public sealed class ConfirmState : PickPackShip.ConfirmationState
		{
			public override string Prompt => Basis.Localize(Msg.Prompt, Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM);
			protected override FlowStatus PerformConfirmation() => Basis.Get<Logic>().Confirm();

			#region Logic
			public class Logic : Extension<Logic>
			{
				public static bool IsActive() => WaveBatchPicking.IsActive(); // TODO: remove once N-nd level extensions start to be inactive if any of their "parents" are inactive

				public
					SelectFrom<SOPickListEntryToCartSplitLink>.
					InnerJoin<INCartSplit>.On<SOPickListEntryToCartSplitLink.FK.CartSplit>.
					Where<SOPickListEntryToCartSplitLink.FK.Cart.SameAsCurrent>.
					View PickerCartSplitLinks;

				public virtual FlowStatus Confirm()
				{
					bool remove = Basis.Remove == true;

					SOPickerListEntry pickedSplit = WBBasis.GetSelectedPickListEntry();
					if (pickedSplit == null)
						return FlowStatus.Fail(remove ? Msg.NothingToRemove : Msg.NothingToPick).WithModeReset;

					if (!remove && Basis.IsEnterableLotSerial(isForIssue: true) && Basis.SelectedLotSerialClass.LotSerTrack == INLotSerTrack.SerialNumbered && Basis.LotSerialNbr != null)
					{
						bool existingSerial =
							SelectFrom<SOPickerListEntry>.
							Where<
								SOPickerListEntry.inventoryID.IsEqual<@P.AsInt>.
								And<SOPickerListEntry.subItemID.IsEqual<@P.AsInt>>.
								And<SOPickerListEntry.lotSerialNbr.IsEqual<@P.AsString>>>.
							View.Select(Base, Basis.InventoryID, Basis.SubItemID, Basis.LotSerialNbr).Any();

						if (existingSerial == false)
						{
							existingSerial |=
								SelectFrom<INItemLotSerial>.
								Where<
									INItemLotSerial.inventoryID.IsEqual<@P.AsInt>.
									And<INItemLotSerial.lotSerialNbr.IsEqual<@P.AsString>>>.
								View.Select(Base, Basis.InventoryID, Basis.LotSerialNbr).Any();
						}

						if (existingSerial)
							return
								FlowStatus.Fail(
									IN.Messages.SerialNumberAlreadyIssued,
									Basis.LotSerialNbr,
									Basis.SightOf<WMSScanHeader.inventoryID>());
					}

					decimal qty = Basis.BaseQty;
					//decimal threshold = Basis.Graph.GetQtyThreshold(pickedSplit);

					if (qty != 0)
					{
						bool splitUpdated = false;

						if (remove)
						{
							if (pickedSplit.PickedQty - qty < 0)
								return FlowStatus.Fail(Msg.Underpicking);
						}
						else
						{
							if (pickedSplit.PickedQty + qty > pickedSplit.Qty/* * threshold*/)
								return FlowStatus.Fail(Msg.Overpicking);

							if (pickedSplit.LotSerialNbr != Basis.LotSerialNbr && Basis.IsEnterableLotSerial(isForIssue: true))
							{
								if (!WBBasis.SetLotSerialNbrAndQty(pickedSplit, qty))
									return FlowStatus.Fail(Msg.Overpicking);
								splitUpdated = true;
							}
						}

						if (WBBasis.Picker.Current.UserID == null)
						{
							WBBasis.Picker.Current.UserID = Base.Accessinfo.UserID;
							WBBasis.Picker.UpdateCurrent();
						}

						if (SOPickingWorksheet.PK.Find(Base, WBBasis.Worksheet.Current).Status == SOPickingWorksheet.status.Open)
						{
							WBBasis.Worksheet.Current.Status = SOPickingWorksheet.status.Picking;
							WBBasis.Worksheet.UpdateCurrent();
						}

						if (!splitUpdated)
						{
							//EnsureAssignedSplitEditing(pickedSplit);

							pickedSplit.PickedQty += remove ? -qty : qty;

							if (remove && Basis.IsEnterableLotSerial(isForIssue: true))
							{
								if (pickedSplit.PickedQty + qty == pickedSplit.Qty)
								{
									if (pickedSplit.PickedQty == 0)
									{
										WBBasis.PickListOfPicker.Delete(pickedSplit);
									}
									else
									{
										pickedSplit.Qty = pickedSplit.PickedQty;
										WBBasis.PickListOfPicker.Update(pickedSplit);
									}
								}
							}
							else
								WBBasis.PickListOfPicker.Update(pickedSplit);
						}
					}

					// should be aware of cart extension
					if (Basis.Get<PPSCartSupport>() is PPSCartSupport cartSupport && cartSupport.CartID != null)
					{
						FlowStatus cartStatus = SyncWithCart(cartSupport, pickedSplit, Sign.MinusIf(remove) * qty);
						if (cartStatus.IsError != false)
							return cartStatus;
					}

					bool wave = WBBasis.Worksheet.Current.WorksheetType == SOPickingWorksheet.worksheetType.Wave;
					INTote targetTote = wave
						? Basis.Get<WavePickMode.Logic>().GetToteForPickListEntry(pickedSplit)
						: null;

					Basis.DispatchNext(
						remove
							? wave ? Msg.InventoryRemovedFromTote : Msg.InventoryRemoved
							: wave ? Msg.InventoryAddedToTote : Msg.InventoryAdded,
						Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM, targetTote?.ToteCD);

					return FlowStatus.Ok;
				}

				protected virtual FlowStatus SyncWithCart(PPSCartSupport cartSupport, SOPickerListEntry entry, decimal qty)
				{
					INCartSplit[] linkedSplits =
						SelectFrom<SOPickListEntryToCartSplitLink>.
						InnerJoin<INCartSplit>.On<SOPickListEntryToCartSplitLink.FK.CartSplit>.
						Where<SOPickListEntryToCartSplitLink.FK.PickListEntry.SameAsCurrent.
							And<SOPickListEntryToCartSplitLink.siteID.IsEqual<@P.AsInt>>.
							And<SOPickListEntryToCartSplitLink.cartID.IsEqual<@P.AsInt>>>.
						View
						.SelectMultiBound(Basis, new object[] { entry }, Basis.SiteID, cartSupport.CartID)
						.RowCast<INCartSplit>()
						.ToArray();

					INCartSplit[] appropriateSplits =
						SelectFrom<INCartSplit>.
						Where<INCartSplit.cartID.IsEqual<@P.AsInt>.
							And<INCartSplit.inventoryID.IsEqual<SOPickerListEntry.inventoryID.FromCurrent>>.
							And<INCartSplit.subItemID.IsEqual<SOPickerListEntry.subItemID.FromCurrent>>.
							And<INCartSplit.siteID.IsEqual<SOPickerListEntry.siteID.FromCurrent>>.
							And<INCartSplit.fromLocationID.IsEqual<SOPickerListEntry.locationID.FromCurrent>>.
							And<INCartSplit.lotSerialNbr.IsEqual<SOPickerListEntry.lotSerialNbr.FromCurrent>>>.
						View
						.SelectMultiBound(Basis, new object[] { entry }, cartSupport.CartID)
						.RowCast<INCartSplit>()
						.ToArray();

					INCartSplit[] existingINSplits = linkedSplits.Concat(appropriateSplits).ToArray();

					INCartSplit cartSplit = existingINSplits.FirstOrDefault(s => s.LotSerialNbr == (Basis.LotSerialNbr ?? s.LotSerialNbr));
					if (cartSplit == null)
					{
						cartSplit = cartSupport.CartSplits.Insert(new INCartSplit
						{
							CartID = cartSupport.CartID,
							InventoryID = entry.InventoryID,
							SubItemID = entry.SubItemID,
							LotSerialNbr = entry.LotSerialNbr,
							ExpireDate = entry.ExpireDate,
							UOM = entry.UOM,
							SiteID = entry.SiteID,
							FromLocationID = entry.LocationID,
							Qty = qty
						});
					}
					else
					{
						cartSplit.Qty += qty;
						cartSplit = cartSupport.CartSplits.Update(cartSplit);
					}

					if (cartSplit.Qty == 0)
					{
						cartSupport.CartSplits.Delete(cartSplit);
						return FlowStatus.Ok;
					}
					else
						return EnsurePickerCartSplitLink(cartSupport, entry, cartSplit, qty);
				}

				protected virtual FlowStatus EnsurePickerCartSplitLink(PPSCartSupport cartSupport, SOPickerListEntry entry, INCartSplit cartSplit, decimal deltaQty)
				{
					var allLinks =
						SelectFrom<SOPickListEntryToCartSplitLink>.
						Where<SOPickListEntryToCartSplitLink.FK.CartSplit.SameAsCurrent.
							Or<SOPickListEntryToCartSplitLink.FK.PickListEntry.SameAsCurrent>>.
						View
						.SelectMultiBound(Base, new object[] { cartSplit, entry })
						.RowCast<SOPickListEntryToCartSplitLink>()
						.ToArray();

					SOPickListEntryToCartSplitLink currentLink = allLinks.FirstOrDefault(
						link => SOPickListEntryToCartSplitLink.FK.CartSplit.Match(Base, cartSplit, link)
							&& SOPickListEntryToCartSplitLink.FK.PickListEntry.Match(Base, entry, link));

					decimal cartQty = allLinks.Where(link => SOPickListEntryToCartSplitLink.FK.CartSplit.Match(Base, cartSplit, link)).Sum(_ => _.Qty ?? 0);

					if (cartQty + deltaQty > cartSplit.Qty)
					{
						return FlowStatus.Fail(PPSCartSupport.Msg.LinkCartOverpicking);
					}
					if (currentLink == null ? deltaQty < 0 : currentLink.Qty + deltaQty < 0)
					{
						return FlowStatus.Fail(PPSCartSupport.Msg.LinkUnderpicking);
					}

					if (currentLink == null)
					{
						currentLink = PickerCartSplitLinks.Insert(new SOPickListEntryToCartSplitLink
						{
							WorksheetNbr = entry.WorksheetNbr,
							PickerNbr = entry.PickerNbr,
							EntryNbr = entry.EntryNbr,
							SiteID = cartSplit.SiteID,
							CartID = cartSplit.CartID,
							CartSplitLineNbr = cartSplit.SplitLineNbr,
							Qty = deltaQty
						});
					}
					else
					{
						currentLink.Qty += deltaQty;
						currentLink = PickerCartSplitLinks.Update(currentLink);
					}

					if (currentLink.Qty == 0)
						PickerCartSplitLinks.Delete(currentLink);

					return FlowStatus.Ok;
				}
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg : PickPackShip.ConfirmationState.Msg
			{
				public const string Prompt = PickPackShip.PickMode.ConfirmState.Msg.Prompt;

				public const string NothingToPick = "No items to pick.";
				public const string NothingToRemove = "No items to remove from the shipment.";

				public const string Overpicking = "The picked quantity cannot be greater than the quantity in the pick list line.";
				public const string Underpicking = "The picked quantity cannot become negative.";

				public const string InventoryAdded = "{0} x {1} {2} has been added to the pick list.";
				public const string InventoryRemoved = "{0} x {1} {2} has been removed from the pick list.";

				public const string InventoryAddedToTote = "{0} x {1} {2} has been added to the {3} tote.";
				public const string InventoryRemovedFromTote = "{0} x {1} {2} has been removed from the {3} tote.";
			}
			#endregion
		}
		#endregion

		#region Commands
		public sealed class ConfirmPickListCommand : PickPackShip.ScanCommand
		{
			public override string Code => "CONFIRM*PICK";
			public override string ButtonName => "scanConfirmPickList";
			public override string DisplayName => Msg.DisplayName;
			protected override bool IsEnabled => Basis.DocumentIsEditable;

			protected override bool Process()
			{
				Basis.Get<Logic>().ConfirmPickList();
				return true;
			}

			#region Logic
			public class Logic : Extension<Logic>
			{
				public static bool IsActive() => WaveBatchPicking.IsActive(); // TODO: remove once N-nd level extensions start to be inactive if any of their "parents" are inactive

				public virtual void ConfirmPickList()
				{
					if (WBBasis.PickListOfPicker.SelectMain().All(s => s.PickedQty == 0))
					{
						Basis.ReportError(Msg.CannotBeConfirmed);
					}
					else if (Basis.Info.Current.MessageType != WMSMessageTypes.Warning && WBBasis.PickListOfPicker.SelectMain().Any(s => s.PickedQty < s.Qty))
					{
						if (Basis.CannotConfirmPartialShipments)
							Basis.ReportError(Msg.CannotBeConfirmedInPart);
						else
							Basis.ReportWarning(Msg.ShouldNotBeConfirmedInPart);
					}
					else if (WBBasis.Worksheet.Current.WorksheetType == SOPickingWorksheet.worksheetType.Batch)
						Basis.SetScanState<BatchPickMode.SortingLocationState>();
					else if (WBBasis.Worksheet.Current.WorksheetType == SOPickingWorksheet.worksheetType.Wave)
						ConfirmPickList(sortingLocationID: null);
				}

				public virtual void ConfirmPickList(int? sortingLocationID)
				{
					SOPickingWorksheet worksheet = WBBasis.Worksheet.Current;
					SOPicker picker = WBBasis.Picker.Current;

					Basis.Reset(fullReset: false);

					Basis
					.WaitFor<SOPicker>((basis, doc) => ConfirmPickListHandler(worksheet, doc, sortingLocationID))
					.WithDescription(Msg.InProcess, picker.PickListNbr)
					.ActualizeDataBy((basis, doc) => SOPicker.PK.Find(basis, doc))
					.OnSuccess(x => x.Say(Msg.Success).ChangeStateTo<PickListState>())
					.OnFail(x => x.Say(Msg.Fail))
					.BeginAwait(picker);
				}

				protected static void ConfirmPickListHandler(SOPickingWorksheet worksheet, SOPicker pickList, int? sortingLocationID)
				{
					using (var ts = new PXTransactionScope())
					{
						PickPackShip.WithSuppressedRedirects(() =>
						{
							var wsGraph = PXGraph.CreateInstance<SOPickingWorksheetReview>();
							wsGraph.PickListConfirmation.ConfirmPickList(pickList, sortingLocationID);
							wsGraph.PickListConfirmation.FulfillShipmentsAndConfirmWorksheet(worksheet);
						});
						ts.Complete();
					}
				}
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string DisplayName = "Confirm Pick List";
				public const string InProcess = "The {0} pick list is being confirmed.";
				public const string Success = "The pick list has been successfully confirmed.";
				public const string Fail = "The pick list confirmation failed.";

				public const string CannotBeConfirmed = "The pick list cannot be confirmed because no items have been picked.";
				public const string CannotBeConfirmedInPart = "The pick list cannot be confirmed because it is not complete.";
				public const string ShouldNotBeConfirmedInPart = "The pick list is incomplete and should not be confirmed. Do you want to confirm the pick list?";
			}
			#endregion
		}
		#endregion

		#region Base States Decoration
		public virtual WMSBase.LocationState DecorateLocationState(WMSBase.LocationState locationState)
		{
			locationState
				.Intercept.IsStateActive.ByConjoin(basis => !basis.DefaultLocation)
				.Intercept.IsStateSkippable.ByDisjoin(basis => !basis.PromptLocationForEveryLine && basis.LocationID != null)
				.Intercept.Validate.ByAppend((basis, location) => basis.Get<WaveBatchPicking>().IsLocationMissing(location, out var error) ? error : Validation.Ok);
			return locationState;
		}

		public virtual WMSBase.InventoryItemState DecorateInventoryState(WMSBase.InventoryItemState inventoryState)
		{
			inventoryState
				.Intercept.Validate.ByAppend((basis, item) => basis.Get<WaveBatchPicking>().IsItemMissing(item, out var error) ? error : Validation.Ok)
				.Intercept.HandleAbsence.ByAppend((basis, barcode) => basis.TryProcessBy<WMSBase.LocationState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication)
					? AbsenceHandling.Done
					: AbsenceHandling.Skipped);
			return inventoryState;
		}

		public virtual WMSBase.LotSerialState DecorateLotSerialState(WMSBase.LotSerialState lotSerialState)
		{
			lotSerialState
				.Intercept.IsStateActive.ByConjoin(basis => !basis.DefaultLotSerial || basis.IsEnterableLotSerial(isForIssue: true))
				.Intercept.Validate.ByAppend((basis, lotSerialNbr) => basis.Get<WaveBatchPicking>().IsLotSerialMissing(lotSerialNbr, out var error) ? error : Validation.Ok);
			return lotSerialState;
		}

		public virtual WMSBase.ExpireDateState DecorateExpireDateState(WMSBase.ExpireDateState expireDateState)
		{
			expireDateState
				.Intercept.IsStateActive.ByConjoin(basis =>
					basis.Get<WaveBatchPicking>().PickListOfPicker.SelectMain().Any(t =>
						t.IsUnassigned == true ||
						t.LotSerialNbr == basis.LotSerialNbr && t.PickedQty == 0));
			return expireDateState;
		}
		#endregion

		#region DAC Overrides
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXUIVisible(typeof(ScanHeader.mode.IsNotIn<WavePickMode.value, BatchPickMode.value>))]
		protected virtual void _(Events.CacheAttached<WMSScanHeader.refNbr> e) { }
		#endregion

		#region Overrides
		/// <summary>
		/// Overrides <see cref="PickPackShip.DocumentIsConfirmed"/>
		/// </summary>
		[PXOverride]
		public virtual bool get_DocumentIsConfirmed(Func<bool> base_DocumentIsConfirmed) => (Basis.CurrentMode is WavePickMode || Basis.CurrentMode is BatchPickMode)
			? Basis.Get<WaveBatchPicking>().PickList?.Confirmed == true
			: base_DocumentIsConfirmed();

		/// <summary>
		/// Overrides <see cref="WarehouseManagementSystem{TSelf, TGraph}.DocumentLoaded"/>
		/// </summary>
		[PXOverride]
		public virtual bool get_DocumentLoaded(Func<bool> base_DocumentLoaded) => (Basis.CurrentMode is WavePickMode || Basis.CurrentMode is BatchPickMode)
			? Basis.Get<WaveBatchPicking>().WorksheetNbr != null
			: base_DocumentLoaded();

		/// <summary>
		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanState"/>
		/// </summary>
		[PXOverride]
		public virtual ScanState<PickPackShip> DecorateScanState(ScanState<PickPackShip> original, Func<ScanState<PickPackShip>, ScanState<PickPackShip>> base_DecorateScanState)
		{
			var state = base_DecorateScanState(original);

			if (state is PickPackShip.PickMode.ShipmentState pick)
			{
				pick.Intercept.Validate.ByAppend(
					(basis, shipment) =>
					{
						if (shipment.CurrentWorksheetNbr != null)
							return Validation.Fail(Msg.ShipmentCannotBePickedSeparately, shipment.ShipmentNbr, shipment.CurrentWorksheetNbr);

						return Validation.Ok;
					});

				pick.Intercept.HandleAbsence.ByAppend(
					(basis, barcode) =>
					{
						if (barcode.Contains("/"))
						{
							(string worksheetNbr, string pickerNbrStr) = barcode.Split('/');
							if (int.TryParse(pickerNbrStr, out int _))
							{
								if (basis.FindMode<WavePickMode>().TryProcessBy<PickListState>(barcode))
								{
									basis.SetScanMode<WavePickMode>();
									basis.FindState<PickListState>().Process(barcode);
									return AbsenceHandling.Done;
								}
								else if (basis.FindMode<BatchPickMode>().TryProcessBy<PickListState>(barcode))
								{
									basis.SetScanMode<BatchPickMode>();
									basis.FindState<PickListState>().Process(barcode);
									return AbsenceHandling.Done;
								}
							}
						}

						return AbsenceHandling.Skipped;
					});
			}

			if (state is PickPackShip.PackMode.ShipmentState pack)
				InjectValidationPickFirst(pack);

			if (state is PickPackShip.ShipMode.ShipmentState ship)
				InjectValidationPickFirst(ship);

			if (state is PickPackShip.LocationState locState && locState.ModeCode == PickPackShip.PackMode.Value)
			{
				locState.Intercept.IsStateActive.ByConjoin(basis =>
				{
					switch (basis.Get<WaveBatchPicking>().ShipmentSpecialPickType)
					{
						case SOPickingWorksheet.worksheetType.Batch: return true;
						case SOPickingWorksheet.worksheetType.Wave: return false;
						case null: return true;
						default: throw new ArgumentOutOfRangeException();
					}
				});
			}

			return state;

			void InjectValidationPickFirst(PickPackShip.ShipmentState refNbrState)
			{
				refNbrState.Intercept.Validate.ByAppend(
					(basis, shipment) =>
					{
						if (shipment.CurrentWorksheetNbr != null && shipment.Picked == false)
							return Validation.Fail(PickPackShip.PackMode.ShipmentState.Msg.ShouldBePickedFirst, shipment.ShipmentNbr);

						return Validation.Ok;
					});
			}
		}

		/// <summary>
		/// Overrides <see cref="PickPackShip.PackMode.ConfirmState.Logic"/>
		/// </summary>
		public class AlterPackConfirmLogic : PXGraphExtension<PickPackShip.PackMode.ConfirmState.Logic, PickPackShip, PickPackShip.Host>
		{
			public static bool IsActive() => WaveBatchPicking.IsActive();

			protected PickPackShip.Host Graph => Base;
			protected PickPackShip Basis => Base1;
			protected WaveBatchPicking WBBasis => Basis.Get<WaveBatchPicking>();

			/// <summary>
			/// Overrides <see cref="PickPackShip.PackMode.ConfirmState.Logic.TargetQty(SOShipLineSplit)"/>
			/// </summary>
			[PXOverride]
			public virtual decimal? TargetQty(SOShipLineSplit split, Func<SOShipLineSplit, decimal?> base_TargetQty)
			{
				switch (WBBasis.ShipmentSpecialPickType)
				{
					case SOPickingWorksheet.worksheetType.Batch: return split.PickedQty * Graph.GetQtyThreshold(split);
					case SOPickingWorksheet.worksheetType.Wave: return split.PickedQty;
					case null: return base_TargetQty(split);
					default: return 0m;
				}
			}
		}

		/// <summary>
		/// Overrides <see cref="PPSCartSupport"/>
		/// </summary>
		public class AlterCartSupport : PXGraphExtension<PPSCartSupport, PickPackShip, PickPackShip.Host>
		{
			public static bool IsActive() => WaveBatchPicking.IsActive() && PPSCartSupport.IsActive();

			protected PickPackShip Basis => Base1;

			/// <summary>
			/// Overrides <see cref="PPSCartSupport.IsCartRequired()"/>
			/// </summary>
			[PXOverride]
			public virtual bool IsCartRequired(Func<bool> base_IsCartRequired)
			{
				return base_IsCartRequired() ||
					Basis.Setup.Current.UseCartsForPick == true &&
					Basis.Header.Mode == BatchPickMode.Value;
			}
		}
		#endregion

		#region Messages
		[PXLocalizable]
		public abstract class Msg : PickPackShip.Msg
		{
			public const string Completed = "The {0} pick list is picked.";

			public const string InventoryMissingInPickList = "The {0} inventory item is not present in the pick list.";
			public const string LocationMissingInPickList = "The {0} location is not present in the pick list.";
			public const string LotSerialMissingInPickList = "The {0} lot/serial number is not present in the pick list.";

			public const string ShipmentCannotBePacked = "The {0} shipment cannot be processed in the Pack mode because the shipment has two or more packages assigned.";
			public const string ShipmentCannotBePickedSeparately = "The {0} shipment cannot be picked individually because the shipment is assigned to the {1} picking worksheet.";
		}
		#endregion

		#region Attached Fields
		[PXUIField(DisplayName = PickPackShip.Msg.Fits)]
		public class FitsWS : PickPackShip.FieldAttached.To<SOPickerListEntry>.AsBool.Named<FitsWS>
		{
			public static bool IsActive() => WaveBatchPicking.IsActive();
			public override bool? GetValue(SOPickerListEntry row)
			{
				bool fits = true;
				if (Base.WMS.LocationID != null)
					fits &= Base.WMS.LocationID == row.LocationID;
				if (Base.WMS.InventoryID != null)
					fits &= Base.WMS.InventoryID == row.InventoryID && Base.WMS.SubItemID == row.SubItemID;
				if (Base.WMS.LotSerialNbr != null)
					fits &= Base.WMS.LotSerialNbr == row.LotSerialNbr || Base.WMS.Header.Mode.IsIn(WavePickMode.Value, BatchPickMode.Value) && Base.WMS.IsEnterableLotSerial(isForIssue: true) && row.PickedQty == 0;
				return fits;
			}
		}

		[PXUIField(Visible = false)]
		public class ShowPickWS : PickPackShip.FieldAttached.To<ScanHeader>.AsBool.Named<ShowPickWS>
		{
			public static bool IsActive() => WaveBatchPicking.IsActive();
			public override bool? GetValue(ScanHeader row) => Base.WMS.Setup.Current.ShowPickTab == true && row.Mode.IsIn(WavePickMode.Value, BatchPickMode.Value);
		}
		#endregion

		#region Extensibility
		public abstract class Extension<TSelfExt> : PXGraphExtension<WaveBatchPicking, PickPackShip, PickPackShip.Host>
			where TSelfExt : Extension<TSelfExt>
		{
			public PickPackShip.Host Graph => Base;
			public PickPackShip Basis => Base1;
			public WaveBatchPicking WBBasis => Base2;
		}
		#endregion
	}

	public sealed class WaveBatchScanHeader : PXCacheExtension<WMSScanHeader, QtyScanHeader, ScanHeader>
	{
		public static bool IsActive() => WaveBatchPicking.IsActive();

		#region WorksheetNbr
		[PXString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Worksheet Nbr.", Enabled = false)]
		[PXSelector(typeof(SOPickingWorksheet.worksheetNbr))]
		[PXUIVisible(typeof(ScanHeader.mode.IsIn<WaveBatchPicking.WavePickMode.value, WaveBatchPicking.BatchPickMode.value>))]
		public string WorksheetNbr { get; set; }
		public abstract class worksheetNbr : BqlString.Field<worksheetNbr> { }
		#endregion
		#region PickerNbr
		[PXInt]
		[PXUIField(DisplayName = "Picker Nbr.", Enabled = false)]
		[PXUIVisible(typeof(ScanHeader.mode.IsIn<WaveBatchPicking.WavePickMode.value, WaveBatchPicking.BatchPickMode.value>))]
		public Int32? PickerNbr { get; set; }
		public abstract class pickerNbr : BqlInt.Field<pickerNbr> { }
		#endregion
		#region RemoveFromToteID
		[PXInt]
		public int? RemoveFromToteID { get; set; }
		public abstract class removeFromToteID : BqlInt.Field<removeFromToteID> { }
		#endregion
	}
}