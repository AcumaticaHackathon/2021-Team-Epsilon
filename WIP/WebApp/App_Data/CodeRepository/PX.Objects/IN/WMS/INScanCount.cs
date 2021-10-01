using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.BarcodeProcessing;
using PX.Objects.Common.Extensions;

namespace PX.Objects.IN.WMS
{
	using WMSBase = WarehouseManagementSystem<INScanCount, INScanCount.Host>;

	public class INScanCount : WMSBase
	{
		public class Host : INPICountEntry { }

		public new class QtySupport : WMSBase.QtySupport { }

		#region Configuration
		public virtual INPIHeader Document => DocumentView.Current;
		public virtual PXSelectBase<INPIHeader> DocumentView => Graph.PIHeader;
		public virtual PXSelectBase<INPIDetail> Details => Graph.PIDetail;

		public override bool ExplicitConfirmation => Setup.Current.ExplicitLineConfirmation == true;

		protected override bool UseQtyCorrectection => Setup.Current.UseDefaultQtyInCount != true;

		public override bool DocumentIsEditable => base.DocumentIsEditable && IsDocumentStatusEditable(Document.Status);
		protected virtual bool IsDocumentStatusEditable(string status) => status == INPIHdrStatus.Counting;
		#endregion

		#region Views
		public
			PXSetupOptional<INScanSetup,
			Where<INScanSetup.branchID.IsEqual<AccessInfo.branchID.FromCurrent>>>
			Setup;

		public
			SelectFrom<INPIClass>.
			Where<INPIClass.pIClassID.IsEqual<INPIHeader.pIClassID.FromCurrent>>.
			View.ReadOnly Class;
		#endregion

		#region Event Handlers
		protected override void _(Events.RowSelected<ScanHeader> e)
		{
			base._(e);
			Details.Cache.SetAllEditPermissions(false);
		}

		protected virtual void _(Events.FieldUpdated<ScanHeader, WMSScanHeader.refNbr> e)
			=> DocumentView.Current = e.NewValue == null ? null : DocumentView.Search<INPIHeader.pIID>(e.NewValue);

		protected virtual void _(Events.FieldVerifying<ScanHeader, WMSScanHeader.inventoryID> e)
		{
			var piHeader = Document;
			if (e.NewValue == null || e.Row == null || piHeader?.SiteID == null) return;

			var inspector = new PhysicalInventory.PILocksInspector(piHeader.SiteID.Value);
			// Acuminator disable once PX1045 PXGraphCreateInstanceInEventHandlers [Justification]
			if (!inspector.IsInventoryLocationIncludedInPI((int?)e.NewValue, e.Row.Get<WMSScanHeader>().LocationID, piHeader.PIID))
				throw new PXSetPropertyException(Messages.InventoryShouldBeUsedInCurrentPI);
		}

		[PXOverride]
		public virtual void INPIHeader_RowPersisting(PXCache sender, PXRowPersistingEventArgs e, PXRowPersisting baseHandler)
		{
			// We don't execute the base method (with the body: "e.Cancel = true") because we need to save a new value "LineCntr" a new detail row adding.
		}
		#endregion

		#region DAC overrides
		[Common.Attributes.BorrowedNote(typeof(INPIHeader), typeof(INPICountEntry))]
		protected virtual void _(Events.CacheAttached<ScanHeader.noteID> e) { }

		[PXMergeAttributes]
		[PXUnboundDefault(typeof(INPIDetail.pIID))]
		[PXSelector(typeof(Search<INPIHeader.pIID>))]
		protected virtual void _(Events.CacheAttached<WMSScanHeader.refNbr> e) { }

		[PXMergeAttributes]
		[PXUnboundDefault(typeof(InventoryMultiplicator.increase))]
		protected virtual void _(Events.CacheAttached<WMSScanHeader.inventoryMultiplicator> e) { }

		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.Enabled), false)]
		protected virtual void _(Events.CacheAttached<INPIDetail.physicalQty> e) { }
		#endregion

		protected override IEnumerable<ScanMode<INScanCount>> CreateScanModes() { yield return new CountMode(); }
		public sealed class CountMode : ScanMode
		{
			public const string Value = "INCO";
			public class value : BqlString.Constant<value> { public value() : base(CountMode.Value) { } }

			public override string Code => Value;
			public override string Description => Msg.Description;

			#region State Machine
			protected override IEnumerable<ScanState<INScanCount>> CreateStates()
			{
				yield return new RefNbrState();
				yield return new LocationState();
				yield return new InventoryItemState();
				yield return new LotSerialState()
					.Intercept.IsStateActive.ByConjoin(basis => basis.IsEnterableLotSerial(isForIssue: false));
				yield return new ConfirmState();
			}

			protected override IEnumerable<ScanTransition<INScanCount>> CreateTransitions()
			{
				return StateFlow(flow => flow
					.From<RefNbrState>()
					.NextTo<LocationState>()
					.NextTo<InventoryItemState>()
					.NextTo<LotSerialState>());
			}

			protected override IEnumerable<ScanCommand<INScanCount>> CreateCommands()
			{
				return new ScanCommand<INScanCount>[]
				{
					new RemoveCommand(),
					new QtySupport.SetQtyCommand(),
					new ConfirmCommand()
				};
			}

			protected override IEnumerable<ScanRedirect<INScanCount>> CreateRedirects() => AllWMSRedirects.CreateFor<INScanCount>();

			protected override void ResetMode(bool fullReset)
			{
				Clear<RefNbrState>(when: fullReset && !Basis.IsWithinReset);
				Clear<LocationState>(when: fullReset);
				Clear<InventoryItemState>();
				Clear<LotSerialState>();
			}
			#endregion

			#region States
			public sealed class RefNbrState : RefNbrState<INPIHeader>
			{
				protected override string StatePrompt => Msg.Prompt;

				protected override bool IsStateSkippable() => Basis.RefNbr != null;

				protected override INPIHeader GetByBarcode(string barcode) => INPIHeader.PK.Find(Basis, barcode);
				protected override void ReportMissing(string barcode) => Basis.Reporter.Error(Msg.Missing, barcode);

				protected override Validation Validate(INPIHeader entity) => Basis.IsDocumentStatusEditable(entity.Status)
					? Validation.Ok
					: Validation.Fail(Msg.InvalidStatus, Basis.DocumentView.Cache.GetStateExt<INPIHeader.status>(entity));

				protected override void Apply(INPIHeader entity)
				{
					Basis.RefNbr = entity.PIID;
					Basis.SiteID = entity.SiteID;
					Basis.NoteID = entity.NoteID;
					Basis.DocumentView.Current = entity;
				}
				protected override void ClearState()
				{
					Basis.RefNbr = null;
					Basis.SiteID = null;
					Basis.NoteID = null;
					Basis.DocumentView.Current = null;
				}

				protected override void ReportSuccess(INPIHeader entity) => Basis.Reporter.Info(Msg.Ready, entity.PIID);

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string Prompt = "Scan or enter a reference number of the PI count.";
					public const string Ready = "The {0} PI count has been loaded and is ready for processing.";
					public const string Missing = "The {0} PI count was not found.";
					public const string NotSet = "Document number is not selected.";
					public const string InvalidStatus = "Document has the {0} status, cannot be used for count.";
				}
				#endregion
			}

			public new sealed class InventoryItemState : WMSBase.InventoryItemState
			{
				protected override AbsenceHandling.Of<PXResult<INItemXRef, InventoryItem>> HandleAbsence(string barcode)
				{
					if (Basis.TryProcessBy<LocationState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication))
						return AbsenceHandling.Done;
					else
						return base.HandleAbsence(barcode);
				}

				protected override Validation Validate(PXResult<INItemXRef, InventoryItem> entity)
				{
					InventoryItem item = entity;
					if (Basis.IsValid<WMSScanHeader.inventoryID>(item.InventoryID, out string errorInventory) == false)
						return Validation.Fail(Msg.NotPresent, item.InventoryCD);
					else
						return base.Validate(entity);
				}

				#region Messages
				public new abstract class Msg : WMSBase.InventoryItemState.Msg
				{
					public const string NotPresent = "The {0} item is not in the list and cannot be added.";
				}
				#endregion
			}

			public new sealed class LocationState : WMSBase.LocationState
			{
				protected override bool IsStateSkippable() => Basis.LocationID != null;

				protected override Validation Validate(INLocation location)
				{
					INPIStatusLoc statusLocation =
						SelectFrom<INPIStatusLoc>.
						Where<
							INPIStatusLoc.pIID.IsEqual<WMSScanHeader.refNbr.FromCurrent>.
							And<
								INPIStatusLoc.locationID.IsEqual<@P.AsInt>.
								Or<INPIStatusLoc.locationID.IsNull>>>.
						View.ReadOnly
						.Select(Basis, location.LocationID);

					if (statusLocation == null)
						return Validation.Fail(Msg.NotPresent, location.LocationCD);
					else
						return base.Validate(location);
				}

				#region Messages
				[PXLocalizable]
				public new abstract class Msg
				{
					public const string NotPresent = "The {0} location is not in the list and cannot be added.";
				}
				#endregion
			}

			public sealed class ConfirmState : ConfirmationState
			{
				public override string Prompt => Basis.Localize(Msg.Prompt, Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM);

				protected override FlowStatus PerformConfirmation() => Get<Logic>().Confirm();

				#region Logic
				public class Logic : ScanExtension
				{
					public virtual FlowStatus Confirm()
					{
						if (!CanConfirm(out var error))
							return error;

						return ConfirmRow();
					}

					protected virtual bool CanConfirm(out FlowStatus error)
					{
						if (Basis.Document == null)
						{
							error = FlowStatus.Fail(RefNbrState.Msg.NotSet);
							return false;
						}

						if (Basis.DocumentIsEditable == false)
						{
							error = FlowStatus.Fail(RefNbrState.Msg.InvalidStatus, Basis.DocumentView.Cache.GetStateExt<INPIHeader.status>(Basis.Document));
							return false;
						}

						if (Basis.InventoryID == null)
						{
							error = FlowStatus.Fail(InventoryItemState.Msg.NotSet);
							return false;
						}

						if (Basis.CurrentMode.HasActive<LotSerialState>() && Basis.LotSerialNbr == null)
						{
							error = FlowStatus.Fail(LotSerialState.Msg.NotSet);
							return false;
						}

						if (Basis.CurrentMode.HasActive<LotSerialState>() &&
							Basis.SelectedLotSerialClass.LotSerTrack == INLotSerTrack.SerialNumbered &&
							Basis.BaseQty != 1)
						{
							error = FlowStatus.Fail(InventoryItemState.Msg.SerialItemNotComplexQty);
							return false;
						}

						error = FlowStatus.Ok;
						return true;
					}

					protected virtual FlowStatus ConfirmRow()
					{
						INPIDetail row = FindDetailRow();

						if (Basis.Class.SelectSingle()?.IncludeZeroItems == false && row?.BookQty == 0)
							return FlowStatus
								.Warn(Msg.InventoryQtyZero, Basis.SelectedInventoryItem.InventoryCD)
								.WithPrompt(Msg.NewLinePrompt);

						decimal? newQty = row?.PhysicalQty ?? 0;
						if (Basis.Remove == true)
							newQty -= Basis.BaseQty;
						else
							newQty += Basis.BaseQty;

						if (Basis.CurrentMode.HasActive<LotSerialState>() &&
							Basis.SelectedLotSerialClass.LotSerTrack == INLotSerTrack.SerialNumbered &&
							newQty.IsNotIn(0, 1))
						{
							return FlowStatus.Fail(InventoryItemState.Msg.SerialItemNotComplexQty);
						}

						if (row == null)
						{
							row = (INPIDetail)Basis.Details.Cache.CreateInstance();
							row.PIID = Basis.RefNbr;
							row.LineNbr = (int)PXLineNbrAttribute.NewLineNbr<INPIDetail.lineNbr>(Basis.Details.Cache, Basis.Document);
							row.InventoryID = Basis.InventoryID;
							row.SubItemID = Basis.SubItemID;
							row.SiteID = Basis.SiteID;
							row.LocationID = Basis.LocationID;
							row.LotSerialNbr = Basis.LotSerialNbr;
							row.PhysicalQty = Basis.BaseQty;
							row.BookQty = 0;
							row = Basis.Details.Insert(row);

							Basis.SaveChanges();

							row = Basis.Details.Locate(row) ?? row;
						}

						Basis.Details.SetValueExt<INPIDetail.physicalQty>(row, newQty);
						row = Basis.Details.Update(row);
						Basis.SaveChanges();

						Basis.DispatchNext(
							Basis.Remove == true ? Msg.InventoryRemoved : Msg.InventoryAdded,
							Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM);

						return FlowStatus.Ok;
					}

					protected virtual INPIDetail FindDetailRow()
					{
						var findDetailCmd = BqlCommand.CreateInstance(Basis.Details.View.BqlSelect.GetType());

						findDetailCmd = findDetailCmd.WhereAnd<Where<
							INPIDetail.inventoryID.IsEqual<WMSScanHeader.inventoryID.FromCurrent>.
							And<INPIDetail.siteID.IsEqual<WMSScanHeader.siteID.FromCurrent>>
						>>();

						if (Basis.CurrentMode.HasActive<LocationState>() && Basis.LocationID != null)
							findDetailCmd = findDetailCmd.WhereAnd<Where<INPIDetail.locationID.IsEqual<WMSScanHeader.locationID.FromCurrent>>>();

						if (Basis.CurrentMode.HasActive<LotSerialState>() && Basis.LotSerialNbr != null)
							findDetailCmd = findDetailCmd.WhereAnd<Where<INPIDetail.lotSerialNbr.IsEqual<WMSScanHeader.lotSerialNbr.FromCurrent>>>();

						var findDetailView = Basis.Graph.TypedViews.GetView(findDetailCmd, false);
						var findResultSet = (PXResult<INPIDetail>)findDetailView.SelectSingle();

						return findResultSet;
					}
				}
				#endregion

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string Prompt = "Confirm counting {0} x {1} {2}.";
					public const string InventoryAdded = "{0} x {1} {2} has been added.";
					public const string InventoryRemoved = "{0} x {1} {2} has been removed.";
					public const string InventoryQtyZero = "The {0} item is not in the list.";
					public const string NewLinePrompt = "Would you like to add the item?";
				}
				#endregion
			}
			#endregion

			#region Commands
			public sealed class ConfirmCommand : ScanCommand
			{
				public override string Code => "CONFIRM";
				public override string ButtonName => "scanConfirmDocument";
				public override string DisplayName => Msg.DisplayName;
				protected override bool IsEnabled => Basis.DocumentIsEditable;

				protected override bool Process()
				{
					Get<Logic>().ConfirmDocument();
					return true;
				}

				#region Logic
				public class Logic : ScanExtension
				{
					public virtual void ConfirmDocument()
					{
						if (Basis.Document == null)
							Basis.ReportError(RefNbrState.Msg.NotSet);

						if (Basis.DocumentIsEditable == false)
							Basis.ReportError(RefNbrState.Msg.InvalidStatus, Basis.DocumentView.Cache.GetStateExt<INPIHeader.status>(Basis.Document));

						BqlCommand nullPhysicalQtyCmd = BqlCommand.CreateInstance(Basis.Details.View.BqlSelect.GetType());
						nullPhysicalQtyCmd = nullPhysicalQtyCmd.WhereAnd<Where<INPIDetail.physicalQty.IsNull>>();
						PXView nullPhysicalQtyView = Basis.Graph.TypedViews.GetView(nullPhysicalQtyCmd, false);

						foreach (INPIDetail detail in nullPhysicalQtyView.SelectMulti().RowCast<INPIDetail>())
						{
							Basis.Details.SetValueExt<INPIDetail.physicalQty>(detail, 0m);
							Basis.Details.Update(detail);
						}

						Basis.SaveChanges();

						Basis.DocumentView.Current = null;
						Basis.CurrentMode.Reset(fullReset: true);
						Basis.ReportInfo(Msg.CountConfirmed);
					}
				}
				#endregion

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string DisplayName = "Confirm";
					public const string CountConfirmed = "The count has been saved.";
				}
				#endregion
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg
			{
				public const string Description = "Scan and Count";
			}
			#endregion
		}

		#region Redirect
		public new sealed class RedirectFrom<TForeignBasis> : WMSBase.RedirectFrom<TForeignBasis>
			where TForeignBasis : PXGraphExtension, IBarcodeDrivenStateMachine
		{
			public override string Code => "COUNT";
			public override string DisplayName => Msg.DisplayName;

			public override bool IsPossible => PXAccess.FeatureInstalled<CS.FeaturesSet.wMSInventory>();

			[PXLocalizable]
			public abstract class Msg
			{
				public const string DisplayName = "PI Count";
			}
		}
		#endregion
	}
}