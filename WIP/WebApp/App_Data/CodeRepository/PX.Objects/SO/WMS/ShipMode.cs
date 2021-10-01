using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.Common;
using PX.Objects.Extensions;
using PX.Objects.AR;
using PX.BarcodeProcessing;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.WMS;

namespace PX.Objects.SO.WMS
{
	using WMSBase = WarehouseManagementSystem<PickPackShip, PickPackShip.Host>;

	public partial class PickPackShip : WMSBase
	{
		public sealed class ShipMode : ScanMode
		{
			public const string Value = "SHIP";
			public class value : BqlString.Constant<value> { public value() : base(ShipMode.Value) { } }

			public override string Code => Value;
			public override string Description => Msg.Description;

			#region State Machine
			protected override ScanState<PickPackShip> GetDefaultState() => Basis.RefNbr == null ? base.GetDefaultState() : FindState(BuiltinScanStates.Command);

			protected override IEnumerable<ScanState<PickPackShip>> CreateStates()
			{
				yield return new ShipmentState();
				yield return new CommandOrShipmentOnlyState();
			}

			protected override IEnumerable<ScanCommand<PickPackShip>> CreateCommands()
			{
				yield return new RefreshRatesCommand();
				yield return new GetLabelsCommand();
				yield return new ConfirmShipmentCommand();
			}

			protected override IEnumerable<ScanRedirect<PickPackShip>> CreateRedirects() => AllWMSRedirects.CreateFor<PickPackShip>();

			protected override void ResetMode(bool fullReset)
			{
				base.ResetMode(fullReset);
				Clear<ShipmentState>(when: fullReset && !Basis.IsWithinReset);
			}
			#endregion

			#region Logic
			public class Logic : ScanExtension
			{
				[Obsolete(PickPackShip.ObsoleteMsg.ScanMember)]
				public PXAction<ScanHeader> GetReturnLabels;
				[Obsolete(PickPackShip.ObsoleteMsg.ScanMember)]
				[PXButton(VisibleOnDataSource = false), PXUIField(DisplayName = SOShipmentEntryActionsAttribute.Messages.GetReturnLabels, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
				public virtual IEnumerable getReturnLabels(PXAdapter adapter) => Base.Actions["scanGetLabels"].Press(adapter);

				[Obsolete(PickPackShip.ObsoleteMsg.ScanMember)]
				public PXAction<ScanHeader> RefreshRates;
				[Obsolete(PickPackShip.ObsoleteMsg.ScanMember)]
				[PXButton(VisibleOnDataSource = false), PXUIField(DisplayName = Messages.RefreshRatesButton, MapViewRights = PXCacheRights.Select, MapEnableRights = PXCacheRights.Select)]
				public virtual IEnumerable refreshRates(PXAdapter adapter) => Base.Actions["scanRefreshRates"].Press(adapter);
			}
			#endregion

			#region States
			public new sealed class ShipmentState : PickPackShip.ShipmentState
			{
				protected override Validation Validate(SOShipment shipment)
				{
					if (shipment.Operation != SOOperation.Issue)
						return Validation.Fail(Msg.InvalidOperation, shipment.ShipmentNbr, Basis.SightOf<SOShipment.operation>(shipment));

					if (shipment.Status != SOShipmentStatus.Open)
						return Validation.Fail(Msg.InvalidStatus, shipment.ShipmentNbr, Basis.SightOf<SOShipment.status>(shipment));

					return Validation.Ok;
				}

				protected override void ReportSuccess(SOShipment shipment) => Basis.ReportInfo(Msg.Ready, shipment.ShipmentNbr);

				protected override void SetNextState()
				{
					Basis.Get<ShipMode.RefreshRatesCommand.Logic>().UpdateRates();
				}

				#region Messages
				[PXLocalizable]
				public new abstract class Msg : PickPackShip.ShipmentState.Msg
				{
					public new const string Ready = "The {0} shipment is loaded and ready to be shipped.";
					public const string InvalidStatus = "The {0} shipment cannot be processed in ship mode because it has the {1} status.";
					public const string InvalidOperation = "The {0} shipment cannot be processed in ship mode because it has the {1} operation.";
				}
				#endregion
			}
			#endregion

			#region Commands
			public sealed class GetLabelsCommand : ScanCommand
			{
				public override string Code => "GET*LABELS";
				public override string ButtonName => "scanGetLabels";
				public override string DisplayName => SOShipmentEntryActionsAttribute.Messages.GetReturnLabels;
				protected override bool IsEnabled => Basis.DocumentIsEditable;

				protected override bool Process() => Get<Logic>().GetLabels();

				#region Logic
				public class Logic : ScanExtension
				{
					public virtual bool GetLabels()
					{
						Basis.Save.Press();
						var clone = Graph.Clone();
						PXLongOperation.StartOperation(Basis.Graph, () =>
						{
							PXLongOperation.SetCustomInfo(clone); // Redirect

							SOShipment shipment =
								SelectFrom<SOShipment>.
								Where<SOShipment.shipmentNbr.IsEqual<@P.AsString>>.
								View.Select(Basis, Basis.RefNbr);

							Graph.GetReturnLabels(shipment);
						});
						return true;
					}
				}
				#endregion
			}

			public sealed class RefreshRatesCommand : ScanCommand
			{
				public override string Code => "REFRESH*RATES";
				public override string ButtonName => "scanRefreshRates";
				public override string DisplayName => Messages.RefreshRatesButton;
				protected override bool IsEnabled => Basis.DocumentIsEditable;

				protected override bool Process() => Get<Logic>().PerformRatesRefresh();

				#region Logic
				public class Logic : ScanExtension
				{
					public virtual bool PerformRatesRefresh()
					{
						if (!string.IsNullOrEmpty(Basis.RefNbr))
						{
							Basis.Save.Press();
							var clone = Graph.Clone();

							PXLongOperation.StartOperation(Graph, () =>
							{
								PXLongOperation.SetCustomInfo(clone); // Redirect
								UpdateRates(clone);
							});

							Basis.Graph.RowSelected.AddHandler<SOCarrierRate>((cache, args) =>
							{
								if (args.Row != null)
									cache.AdjustUI(args.Row).For<SOCarrierRate.amount>(a =>
									{
										if (a.ErrorLevel == PXErrorLevel.Error)
											((IPXInterfaceField)a).ErrorLevel = PXErrorLevel.RowError;
									});
							});
						}
						return true;
					}

					public static void UpdateRates(PickPackShip.Host graph)
					{
						var carrierRateErrors = new Dictionary<SOCarrierRate, PXSetPropertyException>();
						void saveCarrierRateError(PXCache cache, PXExceptionHandlingEventArgs args)
						{
							if (args.Exception is PXSetPropertyException ex)
								carrierRateErrors[(SOCarrierRate)args.Row] = ex;
						};

						try
						{
							graph.ExceptionHandling.AddHandler<SOCarrierRate.method>(saveCarrierRateError);
							graph.CarrierRatesExt.UpdateRates();
						}
						finally
						{
							graph.ExceptionHandling.RemoveHandler<SOCarrierRate.method>(saveCarrierRateError);
						}

						var carrierRateCache = graph.Caches<SOCarrierRate>();
						foreach (var eInfo in carrierRateErrors)
						{
							var carrierRate = eInfo.Key;
							var error = eInfo.Value;
							error = new PXSetPropertyException(error.Message, PXErrorLevel.Error) { ErrorValue = carrierRate.Amount };
							carrierRateCache.RaiseExceptionHandling<SOCarrierRate.amount>(carrierRate, carrierRate.Method, error);
						}
					}

					public virtual void UpdateRates()
					{
						if ((SOPackageDetailEx)Basis.Graph.Packages.SelectWindowed(0, 1) == null)
							return;

						try
						{
							Basis.Graph.CarrierRatesExt.UpdateRates();
						}
						catch (PXException exception)
						{
							Basis.ReportError(exception.MessageNoPrefix);
						}
					}
				}
				#endregion
			}
			#endregion

			#region Redirect
			public sealed class RedirectFrom<TForeignBasis> : WMSBase.RedirectFrom<TForeignBasis>.SetMode<ShipMode>
				where TForeignBasis : PXGraphExtension, IBarcodeDrivenStateMachine
			{
				public override string Code => ShipMode.Value;
				public override string DisplayName => Msg.Description;

				private string RefNbr { get; set; }

				public override bool IsPossible
				{
					get
					{
						if (Basis.Graph.IsMobile)
							return false;

						bool wmsFulfillment = PXAccess.FeatureInstalled<CS.FeaturesSet.wMSFulfillment>();
						var ppsSetup = SOPickPackShipSetup.PK.Find(Basis.Graph, Basis.Graph.Accessinfo.BranchID);
						return wmsFulfillment && ppsSetup?.ShowShipTab == true;
					}
				}

				protected override bool PrepareRedirect()
				{
					if (Basis is PickPackShip pps && pps.RefNbr != null && pps.DocumentIsConfirmed == false)
					{
						if (pps.FindMode<ShipMode>().TryValidate(pps.Shipment).By<ShipmentState>() is Validation valid && valid.IsError == true)
						{
							pps.ReportError(valid.Message, valid.MessageArgs);
							return false;
						}
						else
							RefNbr = pps.RefNbr;
					}

					return true;
				}

				protected override void CompleteRedirect()
				{
					if (Basis is PickPackShip pps && pps.CurrentMode.Code != ReturnMode.Value && this.RefNbr != null)
					{
						if (pps.TryProcessBy(PickPackShip.ShipmentState.Value, RefNbr, StateSubstitutionRule.KeepAll & ~StateSubstitutionRule.KeepPositiveReports))
						{
							pps.SetScanState(pps.CurrentMode.DefaultState.Code);
							RefNbr = null;

							bool needToConfirmPackage = pps.Get<PackMode.Logic>().HasSingleAutoPackage(pps.RefNbr, out SOPackageDetailEx autoPackage) && autoPackage.Confirmed != true;
							if (needToConfirmPackage)
							{
								autoPackage.Confirmed = true;
								pps.Graph.Packages.Update(autoPackage);
								pps.Graph.Document.Current.IsPackageValid = true;
								pps.Graph.Document.UpdateCurrent();
								pps.Reset(fullReset: false);
								pps.SaveChanges();
							}
							pps.Get<ShipMode.RefreshRatesCommand.Logic>().UpdateRates();
						}
					}
				}
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg : ScanMode.Msg
			{
				public const string Description = "Ship";
			}
			#endregion

			#region Attached Fields
			[PXUIField(Visible = false)]
			public class ShowShip : FieldAttached.To<ScanHeader>.AsBool.Named<ShowShip>
			{
				public override bool? GetValue(ScanHeader row) => Base.WMS.Setup.Current.ShowShipTab == true && row.Mode == ShipMode.Value;
			}
			#endregion
		}
	}
}