using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.Common;
using PX.Objects.Common.Extensions;
using PX.Objects.Extensions;
using PX.Objects.AR;
using PX.BarcodeProcessing;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.WMS;
using PX.SM;

namespace PX.Objects.SO.WMS
{
	using WMSBase = WarehouseManagementSystem<PickPackShip, PickPackShip.Host>;

	public partial class PickPackShip : WMSBase
	{
		public class Host : SOShipmentEntry
		{
			public PickPackShip WMS => FindImplementation<PickPackShip>();
		}

		public new class QtySupport : WMSBase.QtySupport { }
		public new class GS1Support : WMSBase.GS1Support { }
		public class UserSetup : PXUserSetup<UserSetup, Host, ScanHeader, SOPickPackShipUserSetup, SOPickPackShipUserSetup.userID> { }

		#region State
		#region BaseQty
		public new decimal BaseQty => INUnitAttribute.ConvertToBase(Graph.Transactions.Cache, InventoryID, UOM, Qty ?? 0, INPrecision.NOROUND);
		#endregion
		#endregion

		#region Configuration
		public override bool ExplicitConfirmation => Setup.Current.ExplicitLineConfirmation == true;

		public override bool DocumentIsEditable => base.DocumentIsEditable && !DocumentIsConfirmed;
		public virtual bool DocumentIsConfirmed => Shipment?.Confirmed == true;

		protected override bool UseQtyCorrectection => Setup.Current.UseDefaultQty != true;

		public virtual bool DefaultLocation => UserSetup.For(Base).DefaultLocationFromShipment == true;
		public virtual bool DefaultLotSerial => UserSetup.For(Base).DefaultLotSerialFromShipment == true;

		public virtual bool HasPick => Setup.Current.ShowPickTab == true;
		public virtual bool HasPack => Setup.Current.ShowPackTab == true;

		public virtual bool CannotConfirmPartialShipments => Setup.Current.ShortShipmentConfirmation == SOPickPackShipSetup.shortShipmentConfirmation.Forbid;
		public virtual bool PromptLocationForEveryLine => Setup.Current.RequestLocationForEachItem == true;
		#endregion

		#region Views
		public
			PXSetupOptional<SOPickPackShipSetup,
			Where<SOPickPackShipSetup.branchID.IsEqual<AccessInfo.branchID.FromCurrent>>>
			Setup;

		public
			SelectFrom<SOShipmentProcessedByUser>.
			Where<
				SOShipmentProcessedByUser.FK.Shipment.SameAsCurrent.
				And<SOShipmentProcessedByUser.userID.IsEqual<AccessInfo.userID.FromCurrent>>>.
			View ShipmentProcessedByUser;
		#endregion

		#region Buttons
		public PXAction<ScanHeader> ViewOrder;
		[PXButton, PXUIField(DisplayName = "View Order")]
		protected virtual IEnumerable viewOrder(PXAdapter adapter)
		{
			SOShipLineSplit currentSplit = (SOShipLineSplit)Graph.Caches<SOShipLineSplit>().Current;
			if (currentSplit == null)
				return adapter.Get();

			SOShipLine currentLine =
				SelectFrom<SOShipLine>.
				Where<
					SOShipLine.shipmentNbr.IsEqual<SOShipLineSplit.shipmentNbr.FromCurrent>.
					And<SOShipLine.lineNbr.IsEqual<SOShipLineSplit.lineNbr.FromCurrent>>>.
				View.SelectSingleBound(Graph, new[] { currentSplit });
			if (currentLine == null)
				return adapter.Get();

			var orderEntry = PXGraph.CreateInstance<SOOrderEntry>();
			orderEntry.Document.Current = orderEntry.Document.Search<SOOrder.orderType, SOOrder.orderNbr>(currentLine.OrigOrderType, currentLine.OrigOrderNbr);
			throw new PXRedirectRequiredException(orderEntry, true, nameof(ViewOrder)) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
		}
		#endregion

		#region Event Handlers
		protected override void _(Events.RowSelected<ScanHeader> e)
		{
			base._(e);

			if (e.Row == null)
				return;

			ScanConfirm.SetVisible(e.Row.Mode != ShipMode.Value && (ExplicitConfirmation || e.Row.Mode == PackMode.Value || Info.Current?.MessageType == WMSMessageTypes.Warning));

			if (DocumentIsConfirmed == true)
			{
				PXCache<SOShipLineSplit> splitsCache = Graph.Caches<SOShipLineSplit>();

				splitsCache.SetAllEditPermissions(false);
				splitsCache.AdjustUI().ForAllFields(a => a.Enabled = false);
			}

			if (String.IsNullOrEmpty(RefNbr))
				Graph.Document.Current = null;
			else
				Graph.Document.Current = Base.Document.Search<SOShipment.shipmentNbr>(RefNbr);
		}

		protected virtual void _(Events.RowUpdated<SOPickPackShipUserSetup> e) => e.Row.IsOverridden = !e.Row.SameAs(Setup.Current);
		protected virtual void _(Events.RowInserted<SOPickPackShipUserSetup> e) => e.Row.IsOverridden = !e.Row.SameAs(Setup.Current);

		protected virtual void _(Events.FieldSelecting<SOShipLineSplit, SOShipLineSplit.lotSerialNbr> e)
		{
			if (e.Row != null && e.Row.IsUnassigned == true)
				e.ReturnValue = IN.Messages.Unassigned;
		}

		protected virtual void _(Events.RowSelected<SOShipLineSplit> e)
		{
			if (e.Row != null && e.Row.IsUnassigned == true)
				e.Cache.Adjust<PXUIFieldAttribute>(e.Row).ForAllFields(a => a.Enabled = false);
		}
		#endregion

		#region DAC overrides
		[Common.Attributes.BorrowedNote(typeof(SOShipment), typeof(SOShipmentEntry))]
		protected virtual void _(Events.CacheAttached<ScanHeader.noteID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Shipment Nbr.", Enabled = false)]
		[PXSelector(typeof(SOShipment.shipmentNbr))]
		protected virtual void _(Events.CacheAttached<WMSScanHeader.refNbr> e) { }

		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.Visible), true)]
		protected virtual void _(Events.CacheAttached<SOShipLineSplit.lineNbr> e) { }

		[PXCustomizeBaseAttribute(typeof(SOShipLotSerialNbrAttribute), nameof(SOShipLotSerialNbrAttribute.ForceDisable), true)]
		protected virtual void _(Events.CacheAttached<SOShipLineSplit.lotSerialNbr> e) { }

		[PXCustomizeBaseAttribute(typeof(SiteAttribute), nameof(SiteAttribute.Enabled), false)]
		protected virtual void _(Events.CacheAttached<SOShipLineSplit.siteID> e) { }

		[PXCustomizeBaseAttribute(typeof(SOLocationAvailAttribute), nameof(SOLocationAvailAttribute.Enabled), false)]
		protected virtual void _(Events.CacheAttached<SOShipLineSplit.locationID> e) { }

		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.Enabled), false)]
		protected virtual void _(Events.CacheAttached<SOShipLineSplit.qty> e) { }

		[PXMergeAttributes]
		[PXSelector(typeof(SearchFor<SOOrder.orderNbr>.Where<SOOrder.orderNbr.IsEqual<SOShipLine.origOrderType.FromCurrent>>))]
		protected virtual void _(Events.CacheAttached<SOShipLine.origOrderNbr> e) { }

		#endregion

		#region State Machine
		protected override ScanMode<PickPackShip> GetDefaultMode()
		{
			UserPreferences userPreferences =
				SelectFrom<UserPreferences>.
				Where<UserPreferences.userID.IsEqual<AccessInfo.userID.FromCurrent>>.
				View.Select(Base);
			var preferencesExt = userPreferences?.GetExtension<DefaultPickPackShipModeByUser>();

			var pickMode = ScanModes.OfType<PickMode>().FirstOrDefault();
			var packMode = ScanModes.OfType<PackMode>().FirstOrDefault();
			var shipMode = ScanModes.OfType<ShipMode>().FirstOrDefault();
			var returnMode = ScanModes.OfType<ReturnMode>().FirstOrDefault();

			return
				preferencesExt?.PPSMode == DefaultPickPackShipModeByUser.pPSMode.Pick && Setup.Current.ShowPickTab == true ? pickMode :
				preferencesExt?.PPSMode == DefaultPickPackShipModeByUser.pPSMode.Pack && Setup.Current.ShowPackTab == true ? packMode :
				preferencesExt?.PPSMode == DefaultPickPackShipModeByUser.pPSMode.Ship && Setup.Current.ShowShipTab == true ? shipMode :
				preferencesExt?.PPSMode == DefaultPickPackShipModeByUser.pPSMode.Return && Setup.Current.ShowReturningTab == true ? returnMode :
				Setup.Current.ShowPickTab == true ? pickMode :
				Setup.Current.ShowPackTab == true ? packMode :
				Setup.Current.ShowShipTab == true ? shipMode :
				Setup.Current.ShowReturningTab == true ? returnMode :
				base.GetDefaultMode();
		}

		protected override IEnumerable<ScanMode<PickPackShip>> CreateScanModes()
		{
			yield return new PickMode();
			yield return new PackMode();
			yield return new ShipMode();
			yield return new ReturnMode();
		}
		#endregion

		#region Logic
		public virtual SOShipment Shipment => SOShipment.PK.Find(Base, Base.Document.Current);

		public virtual IEnumerable<PXResult<SOShipLineSplit, SOShipLine, INLocation>> GetSplits(string shipmentNbr, bool includeUnassigned = false, Func<SOShipLineSplit, bool> processedSeparator = null)
		{
			var assignedOnly =
				SelectFrom<SOShipLineSplit>.
				InnerJoin<SOShipLine>.On<SOShipLineSplit.FK.ShipmentLine>.
				InnerJoin<INLocation>.On<SOShipLineSplit.locationID.IsEqual<INLocation.locationID>>.
				Where<SOShipLineSplit.shipmentNbr.IsEqual<@P.AsString>>.
				View.Select(Base, shipmentNbr)
				.AsEnumerable()
				.Cast<PXResult<SOShipLineSplit, SOShipLine, INLocation>>();

			IEnumerable<PXResult<SOShipLineSplit, SOShipLine, INLocation>> splits;
			if (includeUnassigned)
			{
				SOShipLineSplit MakeAssigned(Unassigned.SOShipLineSplit unassignedSplit) => PropertyTransfer.Transfer(unassignedSplit, new SOShipLineSplit());

				var unassignedOnly =
					SelectFrom<Unassigned.SOShipLineSplit>.
					InnerJoin<SOShipLine>.On<Unassigned.SOShipLineSplit.FK.ShipmentLine>.
					InnerJoin<INLocation>.On<Unassigned.SOShipLineSplit.locationID.IsEqual<INLocation.locationID>>.
					Where<Unassigned.SOShipLineSplit.shipmentNbr.IsEqual<@P.AsString>>.
					View.Select(Base, shipmentNbr)
					.AsEnumerable()
					.Cast<PXResult<Unassigned.SOShipLineSplit, SOShipLine, INLocation>>()
					.Select(r => new PXResult<SOShipLineSplit, SOShipLine, INLocation>(MakeAssigned(r), r, r));

				splits = assignedOnly.Concat(unassignedOnly);
			}
			else
				splits = assignedOnly;

			(var processed, var notProcessed) = processedSeparator != null
				? splits.DisuniteBy(s => processedSeparator(s.GetItem<SOShipLineSplit>()))
				: (Array.Empty<PXResult<SOShipLineSplit, SOShipLine, INLocation>>(), splits);

			var result = new List<PXResult<SOShipLineSplit, SOShipLine, INLocation>>();

			result.AddRange(
				notProcessed
				.OrderBy(
					r => Setup.Current.ShipmentLocationOrdering == SOPickPackShipSetup.shipmentLocationOrdering.Pick
						? r.GetItem<INLocation>().PickPriority
						: r.GetItem<INLocation>().PathPriority)
				.ThenBy(r => r.GetItem<SOShipLineSplit>().IsUnassigned == false) // unassigned first
				.ThenBy(r => r.GetItem<SOShipLineSplit>().InventoryID)
				.ThenBy(r => r.GetItem<SOShipLineSplit>().LotSerialNbr));

			result.AddRange(
				processed
				.OrderByDescending(
					r => Setup.Current.ShipmentLocationOrdering == SOPickPackShipSetup.shipmentLocationOrdering.Pick
						? r.GetItem<INLocation>().PickPriority
						: r.GetItem<INLocation>().PathPriority)
				.ThenByDescending(r => r.GetItem<SOShipLineSplit>().InventoryID)
				.ThenByDescending(r => r.GetItem<SOShipLineSplit>().LotSerialNbr));

			return result;
		}

		public virtual bool IsLocationMissing(PXSelectBase<SOShipLineSplit> splitView, INLocation location, out Validation error)
		{
			if (splitView.SelectMain().All(t => t.LocationID != location.LocationID))
			{
				error = Validation.Fail(Msg.LocationMissingInShipment, location.LocationCD);
				return true;
			}
			else
			{
				error = Validation.Ok;
				return false;
			}
		}

		public virtual bool IsItemMissing(PXSelectBase<SOShipLineSplit> splitView, PXResult<INItemXRef, InventoryItem> item, out Validation error)
		{
			(INItemXRef xref, InventoryItem inventoryItem) = item;
			if (splitView.SelectMain().All(t => t.InventoryID != inventoryItem.InventoryID))
			{
				error = Validation.Fail(Msg.InventoryMissingInShipment, inventoryItem.InventoryCD);
				return true;
			}
			else
			{
				error = Validation.Ok;
				return false;
			}
		}

		public virtual bool IsLotSerialMissing(PXSelectBase<SOShipLineSplit> splitView, string lotSerialNbr, out Validation error)
		{
			if (IsEnterableLotSerial(isForIssue: Header.Mode != ReturnMode.Value) == false && splitView.SelectMain().All(t => t.LotSerialNbr != lotSerialNbr))
			{
				error = Validation.Fail(Msg.LotSerialMissingInShipment, lotSerialNbr);
				return true;
			}
			else
			{
				error = Validation.Ok;
				return false;
			}
		}

		public void EnsureAssignedSplitEditing(SOShipLineSplit split)
		{
			if (split.IsUnassigned == true)
				throw new InvalidOperationException("Unassigned splits should not be edited directly by WMS screen");
		}

		public void EnsureShipmentUserLink()
		{
			var pickingTimeout = TimeSpan.FromMinutes(10);

			DateTime businessNow = Base.Accessinfo.BusinessDate.Value.Add(DateTime.Now.TimeOfDay); // TODO: change it to server time
			var shipByUserLinks = ShipmentProcessedByUser.Select().RowCast<SOShipmentProcessedByUser>().ToArray();

			bool isInitialChange =
				HasPick.Implies(Header.Mode == PickMode.Value)
				&& Remove == false
				&& !shipByUserLinks.Any();

			if (isInitialChange)
				ShipmentProcessedByUser.Insert().Apply(r => r.StartDateTime = r.LastModifiedDateTime = businessNow);
			else
			{
				var currentLink = shipByUserLinks.FirstOrDefault(s => s.EndDateTime == null)
					?? ShipmentProcessedByUser.Insert().Apply(r => r.StartDateTime = r.LastModifiedDateTime = businessNow);

				if (currentLink.LastModifiedDateTime.Value.Add(pickingTimeout) > businessNow)
				{
					currentLink.LastModifiedDateTime = currentLink.LastModifiedDateTime.Value.Add(pickingTimeout);
					ShipmentProcessedByUser.Update(currentLink);
				}
				else
				{
					currentLink.EndDateTime = currentLink.LastModifiedDateTime.Value.Add(pickingTimeout);
					ShipmentProcessedByUser.Update(currentLink);
					ShipmentProcessedByUser.Insert().Apply(r => r.StartDateTime = r.LastModifiedDateTime = businessNow);
				}
			}
		}
		#endregion

		#region States
		public abstract class ShipmentState : RefNbrState<SOShipment>
		{
			protected override string StatePrompt => Msg.Prompt;

			protected override SOShipment GetByBarcode(string barcode)
			{
				SOShipment shipment =
					SelectFrom<SOShipment>.
					InnerJoin<INSite>.On<SOShipment.FK.Site>.
					LeftJoin<Customer>.On<SOShipment.customerID.IsEqual<Customer.bAccountID>>.SingleTableOnly.
					Where<
						SOShipment.shipmentNbr.IsEqual<@P.AsString>.
						And<Match<INSite, AccessInfo.userName.FromCurrent>>.
						And<
							Customer.bAccountID.IsNull.
							Or<Match<Customer, AccessInfo.userName.FromCurrent>>>>.
					View.ReadOnly.Select(Basis, barcode);
				return shipment;
			}

			protected override void Apply(SOShipment shipment)
			{
				Basis.Graph.Document.Current = shipment;

				Basis.RefNbr = shipment.ShipmentNbr;
				Basis.SiteID = shipment.SiteID;
				Basis.TranDate = shipment.ShipDate;
				Basis.NoteID = shipment.NoteID;
			}

			protected override void ClearState()
			{
				Basis.Graph.Document.Current = null;

				Basis.RefNbr = null;
				Basis.SiteID = null;
				Basis.TranDate = null;
				Basis.NoteID = null;
			}

			protected override void ReportMissing(string barcode) => Basis.ReportError(Msg.Missing, barcode);
			protected override void ReportSuccess(SOShipment shipment) => Basis.ReportInfo(Msg.Ready, shipment.ShipmentNbr);

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string Prompt = "Scan the shipment number.";
				public const string Ready = "The {0} shipment is loaded and ready to be processed.";
				public const string Missing = "The {0} shipment is not found.";
				public const string Invalid = "The {0} shipment cannot be processed because it has the {1} status.";
			}
			#endregion
		}

		public sealed class CommandOrShipmentOnlyState : CommandOnlyStateBase<PickPackShip>
		{
			public override void MoveToNextState() { }
			public override string Prompt => Msg.UseCommandOrShipmentToContinue;
			public override bool Process(string barcode)
			{
				if (Basis.TryProcessBy<ShipmentState>(barcode))
				{
					Basis.Clear<ShipmentState>();
					Basis.Reset(fullReset: false);
					Basis.SetScanState<ShipmentState>();
					Basis.CurrentMode.FindState<ShipmentState>().Process(barcode);
					return true;
				}
				else
				{
					Basis.Reporter.Error(Msg.OnlyCommandsAndShipmentsAreAllowed);
					return false;
				}
			}

			#region Messages
			[PXLocalizable]
			public new abstract class Msg
			{
				public const string UseCommandOrShipmentToContinue = "Use any command or scan the next shipment to continue.";
				public const string OnlyCommandsAndShipmentsAreAllowed = "Only commands or a shipment can be used to continue.";
			}
			#endregion
		}
		#endregion

		#region Commands
		public sealed class ConfirmShipmentCommand : ScanCommand
		{
			public override string Code => "CONFIRM*SHIPMENT";
			public override string ButtonName => "scanConfirmShipment";
			public override string DisplayName => Msg.DisplayName;
			protected override bool IsEnabled => Basis.DocumentIsEditable;

			protected override bool Process() => Basis.Get<Logic>().ConfirmShipment(confirmAsIs: !Basis.HasPick && !Basis.HasPack);

			#region Logic
			public class Logic : ScanExtension
			{
				public virtual bool ConfirmShipment(bool confirmAsIs)
				{
					if (!CanConfirm(confirmAsIs))
						return true;

					PackMode.Logic packLogic = Basis.Get<PackMode.Logic>();
					if (packLogic.SelectedPackage?.Confirmed == false)
						if (packLogic.AutoConfirmPackage(Basis.Setup.Current.ConfirmEachPackageWeight == false) == false || Basis.Header.ScanState == PackMode.BoxWeightState.Value)
							return true;

					int? packageLineNbr = packLogic.PackageLineNbr;
					Basis.CurrentMode.Reset(fullReset: false);
					packLogic.PackageLineNbr = packageLineNbr;

					string shipmentNbr = Basis.RefNbr;
					var (setup, userSetup) = (Basis.Setup.Current, UserSetup.For(Basis));
					SOPackageDetailEx autoPackageToConfirm = null;
					if (!confirmAsIs && Basis.Header.Mode.IsIn(PackMode.Value, ShipMode.Value))
						packLogic.HasSingleAutoPackage(shipmentNbr, out autoPackageToConfirm);

					Basis.SaveChanges();

					Basis
					.WaitFor<SOShipment>((basis, doc) => ConfirmShipmentHandler(doc.ShipmentNbr, confirmAsIs, setup, userSetup, autoPackageToConfirm))
					.WithDescription(Msg.InProcess, Basis.RefNbr)
					.ActualizeDataBy((basis, doc) => SOShipment.PK.Find(basis, doc))
					.OnSuccess(x => x.Say(Msg.Success).ChangeStateTo<ShipmentState>())
					.OnFail(x => x.Say(Msg.Fail))
					.BeginAwait(Basis.Shipment);

					return true;
				}

				protected static void ConfirmShipmentHandler(string shipmentNbr, bool confirmAsIs, SOPickPackShipSetup setup, SOPickPackShipUserSetup userSetup, SOPackageDetailEx autoPackageToConfirm)
				{
					SOShipmentEntry shipmentEntry = PXGraph.CreateInstance<SOShipmentEntry>();
					shipmentEntry.Document.Current =
						SelectFrom<SOShipment>.
						Where<SOShipment.shipmentNbr.IsEqual<@P.AsString>>.
						View.Select(shipmentEntry, shipmentNbr);

					var kitSpecHelper = new NonStockKitSpecHelper(shipmentEntry);

					CloseShipmentUserLinks(shipmentEntry, shipmentNbr);

					var RequireShipping = Func.Memorize((int inventoryID) => InventoryItem.PK.Find(shipmentEntry, inventoryID).With(item => item.StkItem == true || item.NonStockShip == true));

					if (!confirmAsIs && (setup.ShowPickTab == true || setup.ShowPackTab == true))
					{
						PXSelectBase<SOShipLine> lines = shipmentEntry.Transactions;
						PXSelectBase<SOShipLineSplit> splits = shipmentEntry.splits;

						foreach (SOShipLine line in lines.Select())
						{
							lines.Current = line;
							decimal lineQty = 0;

							decimal GetNewQty(SOShipLineSplit split) => setup.ShowPickTab == true ? split.PickedQty ?? 0 : Math.Max(split.PickedQty ?? 0, split.PackedQty ?? 0);
							if (kitSpecHelper.IsNonStockKit(line.InventoryID))
							{
								// kitInventoryID -> compInventory -> qty
								var nonStockKitSpec = kitSpecHelper.GetNonStockKitSpec(line.InventoryID.Value).Where(pair => RequireShipping(pair.Key)).ToDictionary();
								var nonStockKitSplits = splits.SelectMain().GroupBy(r => r.InventoryID.Value).ToDictionary(g => g.Key, g => g.Sum(s => GetNewQty(s)));

								lineQty = nonStockKitSpec.Keys.Count() == 0 || nonStockKitSpec.Keys.Except(nonStockKitSplits.Keys).Count() > 0
									? 0
									: (from split in nonStockKitSplits
									   join spec in nonStockKitSpec on split.Key equals spec.Key
									   select Math.Floor(decimal.Divide(split.Value, spec.Value))).Min();
							}
							else
							{
								foreach (SOShipLineSplit split in splits.Select())
								{
									splits.Current = split;

									decimal newQty = GetNewQty(splits.Current);
									if (newQty != splits.Current.Qty)
									{
										splits.Current.Qty = newQty;
										splits.UpdateCurrent();
									}

									if (splits.Current.Qty != 0)
										lineQty += splits.Current.Qty ?? 0;
								}
								lineQty = INUnitAttribute.ConvertFromBase(lines.Cache, lines.Current.InventoryID, lines.Current.UOM, lineQty, INPrecision.NOROUND);
							}

							lines.Current.Qty = lineQty;
							lines.UpdateCurrent();

							if (lines.Current.Qty == 0)
								lines.DeleteCurrent();
						}

						foreach (SOPackageDetailEx package in shipmentEntry.Packages.SelectMain())
							if (package.PackageType == SOPackageType.Manual && shipmentEntry.PackageDetailExt.PackageDetailSplit.Select(package.ShipmentNbr, package.LineNbr).Count == 0)
								shipmentEntry.Packages.Delete(package);
					}

					foreach (SOCartShipment cartLink in SelectFrom<SOCartShipment>.Where<SOCartShipment.shipmentNbr.IsEqual<SOShipment.shipmentNbr.FromCurrent>>.View.Select(shipmentEntry))
						shipmentEntry.Caches<SOCartShipment>().Delete(cartLink);

					if (autoPackageToConfirm?.Confirmed == false)
					{
						autoPackageToConfirm.Confirmed = true;
						shipmentEntry.Packages.Update(autoPackageToConfirm);
					}

					if (PXAccess.FeatureInstalled<FeaturesSet.autoPackaging>())
					{
						var packages = shipmentEntry.Packages.SelectMain();
						if (confirmAsIs)
						{
							foreach (var package in packages.Where(x => x.Confirmed != true))
							{
								package.Confirmed = true;
								shipmentEntry.Packages.Cache.Update(package);
							}
						}
						if (shipmentEntry.Document.Current.IsPackageValid == false && packages.Any(p => p.PackageType == SOPackageType.Auto))
						{
							shipmentEntry.Document.Current.IsPackageValid = true;
							shipmentEntry.Document.UpdateCurrent();
						}
					}

					if (shipmentEntry.IsDirty)
					{
						shipmentEntry.Document.Current.IsPackageValid = true;
						shipmentEntry.Document.UpdateCurrent();
						shipmentEntry.Save.Press();
					}

					if (UseExternalShippingApplication(shipmentEntry, shipmentEntry.Document.Current, out Carrier carrier))
					{
						// Shipping Tool will confirm the shipment.
						throw new PXRedirectToUrlException(
							$"../../Frames/ShipmentAppLauncher.html?ShipmentApplicationType={carrier.ShippingApplicationType}&ShipmentNbr={shipmentNbr}",
							PXBaseRedirectException.WindowMode.NewWindow, true, string.Empty);
					}

					shipmentEntry.confirmShipmentAction.Press();

					shipmentEntry.Clear();
					shipmentEntry.Document.Current = shipmentEntry.Document.Search<SOShipment.shipmentNbr>(shipmentNbr);

					if (PXAccess.FeatureInstalled<FeaturesSet.deviceHub>())
					{
						PXView.Dummy view = PXView.Dummy.For<SOShipment>(shipmentEntry);
						PXAdapter deviceHubAdapter = new PXAdapter(view)
						{
							MassProcess = true, //Device Hub require this flag to know if supported
							Arguments =
						{
							[nameof(IPrintable.PrintWithDeviceHub)] = true,
							[nameof(IPrintable.DefinePrinterManually)] = false
						}
						};

						//Labels should ALWAYS be printer first because they go out faster, and that gives time to user to peel/stick them while shipment confirmation is spooling
						if (userSetup.PrintShipmentLabels == true)
							WithSuppressedRedirects(() => shipmentEntry.PrintCarrierLabels(new List<SOShipment>() { shipmentEntry.Document.Current }, deviceHubAdapter));

						if (userSetup.PrintShipmentConfirmation == true)
							WithSuppressedRedirects(() => shipmentEntry.PrintShipmentConfirmation(deviceHubAdapter));
					}
				}

				protected static void CloseShipmentUserLinks(PXGraph graph, string shipmentNbr)
				{
					var pickingTimeout = TimeSpan.FromMinutes(10);

					DateTime businessNow = graph.Accessinfo.BusinessDate.Value.Add(DateTime.Now.TimeOfDay); // TODO: change it to server time
					foreach (SOShipmentProcessedByUser shipByUser in
						SelectFrom<SOShipmentProcessedByUser>.
						Where<SOShipmentProcessedByUser.shipmentNbr.IsEqual<@P.AsString>>.
						View.Select(graph, shipmentNbr))
					{
						shipByUser.Confirmed = true;
						if (shipByUser.EndDateTime == null)
							shipByUser.EndDateTime = Tools.Min(businessNow, shipByUser.LastModifiedDateTime.Value.Add(pickingTimeout));

						graph.Caches<SOShipmentProcessedByUser>().Update(shipByUser);
					}
				}

				protected virtual bool UseExternalShippingApplication(SOShipment shipment, out Carrier carrier) => UseExternalShippingApplication(Basis, shipment, out carrier);
				protected static bool UseExternalShippingApplication(PXGraph graph, SOShipment shipment, out Carrier carrier)
				{
					carrier = Carrier.PK.Find(graph, shipment.ShipVia);
					return graph.IsMobile == false && carrier != null && carrier.IsExternalShippingApplication == true;
				}

				protected virtual bool CanConfirm(bool confirmAsIs)
				{
					if (confirmAsIs)
						return true;

					if (Basis.HasPick && !CanConfirmPicked())
						return false;

					if (Basis.HasPack && !CanConfirmPacked())
						return false;

					return true;
				}

				protected virtual bool CanConfirmPicked()
				{
					var splits = Basis.Get<PickMode.Logic>().Picked.SelectMain();
					if (splits.All(s => s.PickedQty == 0))
					{
						Basis.ReportError(Msg.ShipmentCannotBeConfirmed);
						return false;
					}

					if (Basis.Info.Current.MessageType != WMSMessageTypes.Warning && splits.Any(s => s.PickedQty < s.Qty * Basis.Graph.GetMinQtyThreshold(s)))
					{
						if (Basis.CannotConfirmPartialShipments)
							Basis.ReportError(Msg.ShipmentCannotBeConfirmedInPart);
						else
							Basis.ReportWarning(Msg.ShipmentShouldNotBeConfirmedInPart);
						return false;
					}

					if (HasIncompleteLinesBy<SOShipLineSplit.pickedQty>())
					{
						Basis.ReportError(Msg.ShipmentCannotBeConfirmedInPart);
						return false;
					}

					return true;
				}

				protected virtual bool CanConfirmPacked()
				{
					var splits = Basis.Get<PackMode.Logic>().PickedForPack.SelectMain();
					if (splits.All(s => s.PackedQty == 0))
						return true;

					if (Basis.Info.Current.MessageType != WMSMessageTypes.Warning && splits.Any(s => s.PackedQty < s.Qty * Basis.Graph.GetMinQtyThreshold(s)))
					{
						if (Basis.CannotConfirmPartialShipments)
							Basis.ReportError(Msg.ShipmentCannotBeConfirmedInPart);
						else
							Basis.ReportWarning(Msg.ShipmentShouldNotBeConfirmedInPart);
						return false;
					}

					if (HasIncompleteLinesBy<SOShipLineSplit.packedQty>())
					{
						Basis.ReportError(Msg.ShipmentCannotBeConfirmedInPart);
						return false;
					}

					return true;
				}

				protected virtual bool HasIncompleteLinesBy<TQtyField>()
					where TQtyField : class, IBqlField, IImplement<IBqlDecimal>
				{
					bool hasIncompleteLines =
						SelectFrom<SOLine>.
						InnerJoin<SOOrder>.On<SOLine.FK.Order>.
						InnerJoin<SOShipLine>.On<SOShipLine.FK.OrderLine>.
						InnerJoin<SOShipLineSplit>.On<SOShipLineSplit.FK.ShipmentLine>.
						Where<
							SOShipLine.FK.Shipment.SameAsCurrent.
							And<SOShipLineSplit.qty.Multiply<SOLine.completeQtyMin.Divide<decimal100>>.IsGreater<TQtyField>>.
							And<
								SOOrder.shipComplete.IsEqual<SOShipComplete.shipComplete>.
								Or<SOLine.shipComplete.IsEqual<SOShipComplete.shipComplete>>>>.
						View.SelectMultiBound(Basis, new[] { Basis.Shipment }).Any();
					return hasIncompleteLines;
				}
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string DisplayName = "Confirm Shipment";
				public const string InProcess = "The {0} shipment is being confirmed.";
				public const string Success = "The shipment has been successfully confirmed.";
				public const string Fail = "The shipment confirmation failed.";

				public const string ShipmentCannotBeConfirmed = "The shipment cannot be confirmed because no items have been picked.";
				public const string ShipmentCannotBeConfirmedNoPacked = "The shipment cannot be confirmed because no items have been packed.";
				public const string ShipmentCannotBeConfirmedInPart = "The shipment cannot be confirmed because it is not complete.";
				public const string ShipmentShouldNotBeConfirmedInPart = "The shipment is incomplete and should not be confirmed. Do you want to confirm the shipment?";
			}
			#endregion
		}

		public sealed class ConfirmShipmentAsIsCommand : ScanCommand
		{
			public override string Code => "CONFIRM*SHIPMENT*ALL";
			public override string ButtonName => "scanConfirmShipmentAll";
			public override string DisplayName => Msg.DisplayName;
			protected override bool IsEnabled => Basis.DocumentIsEditable;

			protected override bool Process() => Basis.Get<ConfirmShipmentCommand.Logic>().ConfirmShipment(confirmAsIs: true);

			#region Messages
			[PXLocalizable]
			public new abstract class Msg : ConfirmShipmentCommand.Msg
			{
				public new const string DisplayName = "Confirm Shipment As Is";
			}
			#endregion
		}
		#endregion

		#region Messages
		[PXLocalizable]
		public new abstract class Msg : WMSBase.Msg
		{
			public const string ShipmentIsNotEditable = "The shipment became unavailable for editing. Contact your manager.";

			public const string InventoryMissingInShipment = "The {0} inventory item is not present in the shipment.";
			public const string LocationMissingInShipment = "The {0} location is not present in the shipment.";
			public const string LotSerialMissingInShipment = "The {0} lot/serial number is not present in the shipment.";
		}
		#endregion

		#region Attached Fields
		public static class FieldAttached
		{
			public abstract class To<TTable> : PXFieldAttachedTo<TTable>.By<Host>
				where TTable : class, IBqlTable, new()
			{ }

			[PXUIField(DisplayName = Msg.Fits)]
			public class Fits : FieldAttached.To<SOShipLineSplit>.AsBool.Named<Fits>
			{
				public override bool? GetValue(SOShipLineSplit row)
				{
					bool fits = true;
					if (Base.WMS.LocationID != null)
						fits &= Base.WMS.LocationID == row.LocationID;
					if (Base.WMS.InventoryID != null)
						fits &= Base.WMS.InventoryID == row.InventoryID && Base.WMS.SubItemID == row.SubItemID;
					if (Base.WMS.LotSerialNbr != null)
						fits &= Base.WMS.LotSerialNbr == row.LotSerialNbr || Base.WMS.Header.Mode == PickMode.Value && Base.WMS.IsEnterableLotSerial(isForIssue: Base.WMS.Header.Mode != ReturnMode.Value) && row.PickedQty == 0;
					return fits;
				}
			}

			[PXUIField(Visible = false)]
			public class ShowLog : FieldAttached.To<ScanHeader>.AsBool.Named<ShowLog>
			{
				public override bool? GetValue(ScanHeader row) => Base.WMS.Setup.Current.ShowScanLogTab == true;
			}
		}
		#endregion
	}
}