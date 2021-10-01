using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.BarcodeProcessing;
using PX.Objects.CS;
using PX.Objects.Common.Extensions;

namespace PX.Objects.IN.WMS
{
	public abstract class INScanRegisterBase<TSelf, TGraph, TDocType> : WarehouseManagementSystem<TSelf, TGraph>
		where TSelf : INScanRegisterBase<TSelf, TGraph, TDocType>
		where TGraph : INRegisterEntryBase, new()
		where TDocType : IConstant, IBqlOperand, new()
	{
		#region State
		public RegisterScanHeader RegisterHeader => Header.Get<RegisterScanHeader>() ?? new RegisterScanHeader();
		public ValueSetter<ScanHeader>.Ext<RegisterScanHeader> RegisterSetter => HeaderSetter.With<RegisterScanHeader>();

		#region DocType
		public string DocType => RegisterHeader.DocType;
		#endregion
		#region ReasonCodeID
		public string ReasonCodeID
		{
			get => RegisterHeader.ReasonCodeID;
			set => RegisterSetter.Set(h => h.ReasonCodeID, value);
		}
		#endregion
		#endregion

		#region Selected Entities
		public ReasonCode SelectedReasonCode => ReasonCode.PK.Find(Graph, ReasonCodeID);
		#endregion

		public INRegister Document => DocumentView.Current;
		public PXSelectBase<INRegister> DocumentView => Graph.INRegisterDataMember;
		public PXSelectBase<INTran> Details => Graph.INTranDataMember;

		public bool NotReleasedAndHasLines => Document?.Released != true && Details.SelectMain().Any();

		#region Configuration
		public abstract bool PromptLocationForEveryLine { get; }
		public abstract bool UseDefaultReasonCode { get; }
		public abstract bool UseDefaultWarehouse { get; }

		public override bool DocumentLoaded => Document != null;
		public override bool DocumentIsEditable => base.DocumentIsEditable && INRegister.PK.Find(Base, Document)?.Released != true;
		#endregion

		#region Scan Setup (Common/User's)
		public PXSetupOptional<INScanSetup, Where<INScanSetup.branchID.IsEqual<AccessInfo.branchID.FromCurrent>>> Setup;
		public abstract class UserSetup : PXUserSetupPerMode<UserSetup, TGraph, ScanHeader, INScanUserSetup, INScanUserSetup.userID, INScanUserSetup.mode, TDocType> { }
		#endregion

		#region Event Handlers
		protected override void _(Events.RowSelected<ScanHeader> e)
		{
			base._(e);

			if (Document == null && !string.IsNullOrEmpty(RefNbr))
				RefNbr = null;

			Details.Cache.SetAllEditPermissions(Document == null || Document.Released != true);
			Details.Cache.AllowInsert = false;
		}

		protected virtual void _(Events.FieldDefaulting<ScanHeader, RegisterScanHeader.docType> e)
			=> e.NewValue = new TDocType().Value;

		protected virtual void _(Events.FieldUpdated<ScanHeader, WMSScanHeader.refNbr> e)
			=> DocumentView.Current = e.NewValue == null ? null : DocumentView.Search<INRegister.refNbr>(e.NewValue);

		protected virtual void _(Events.RowSelected<INTran> e)
		{
			bool isMobileAndNotReleased = Graph.IsMobile && (Document == null || Document.Released != true);

			Details.Cache
				.AdjustUI()
				.For<INTran.inventoryID>(ui => ui.Enabled = false)
				.SameFor<INTran.tranDesc>()
				.SameFor<INTran.qty>()
				.SameFor<INTran.uOM>()
				.For<INTran.lotSerialNbr>(ui => ui.Enabled = isMobileAndNotReleased)
				.SameFor<INTran.expireDate>()
				.SameFor<INTran.reasonCode>()
				.SameFor<INTran.locationID>();
		}

		protected virtual void _(Events.RowUpdated<INScanUserSetup> e) => e.Row.IsOverridden = !e.Row.SameAs(Setup.Current);
		protected virtual void _(Events.RowInserted<INScanUserSetup> e) => e.Row.IsOverridden = !e.Row.SameAs(Setup.Current);
		#endregion

		#region DAC overrides
		[PXMergeAttributes]
		[PXUnboundDefault(typeof(INRegister.refNbr))]
		[PXSelector(typeof(SearchFor<INRegister.refNbr>.Where<INRegister.docType.IsEqual<RegisterScanHeader.docType.FromCurrent>>))]
		protected virtual void _(Events.CacheAttached<WMSScanHeader.refNbr> e) { }

		[PXMergeAttributes]
		[PXUnboundDefault(typeof(
				 InventoryMultiplicator.decrease	.When<RegisterScanHeader.docType.FromCurrent.IsIn<INDocType.issue, INDocType.transfer>>.
			Else<InventoryMultiplicator.increase>	.When<RegisterScanHeader.docType.FromCurrent.IsEqual<INDocType.receipt>>.
			ElseNull))]
		protected virtual void _(Events.CacheAttached<WMSScanHeader.inventoryMultiplicator> e) { }
		#endregion

		#region Overrides
		protected override bool ProcessSingleBarcode(string barcode)
		{
			// just clears the selected document after it got released on the next scan
			if (Header.ProcessingSucceeded == true && Document?.Released == true)
			{
				RefNbr = null;
				NoteID = null;
			}

			return base.ProcessSingleBarcode(barcode);
		}

		protected override ScanCommand<TSelf> DecorateScanCommand(ScanCommand<TSelf> original)
		{
			var command = base.DecorateScanCommand(original);

			if (command is RemoveCommand remove)
				remove.Intercept.IsEnabled.ByConjoin(basis => basis.NotReleasedAndHasLines);

			if (command is QtySupport.SetQtyCommand setQty)
				setQty.Intercept.IsEnabled.ByConjoin(basis => basis.UseQtyCorrectection.Implies(basis.DocumentIsEditable && basis.NotReleasedAndHasLines));

			return command;
		}

		/// <summary>
		/// Overrides <see cref="PXGraph.Persist()"/>
		/// </summary>
		[PXOverride]
		public virtual void Persist(Action base_Persist)
		{
			base_Persist();

			RefNbr = Document?.RefNbr;
			NoteID = Document?.NoteID;

			Details.Cache.Clear();
			Details.Cache.ClearQueryCacheObsolete();
		}
		#endregion

		#region States
		public new sealed class WarehouseState : WarehouseManagementSystem<TSelf, TGraph>.WarehouseState
		{
			protected override bool UseDefaultWarehouse => Basis.UseDefaultWarehouse;
		}

		public sealed class ReasonCodeState : EntityState<ReasonCode>
		{
			public const string Value = "RSNC";
			public class value : BqlString.Constant<value> { public value() : base(ReasonCodeState.Value) { } }

			public override string Code => Value;
			protected override string StatePrompt => Msg.Prompt;

			protected override bool IsStateActive() => Basis.UseDefaultReasonCode == false;

			protected override ReasonCode GetByBarcode(string barcode) => ReasonCode.PK.Find(Basis, barcode);
			protected override void ReportMissing(string barcode) => Basis.Reporter.Error(Msg.Missing, barcode);
			protected override Validation Validate(ReasonCode reasonCode) => Basis.IsValid<RegisterScanHeader.reasonCodeID>(reasonCode.ReasonCodeID, out string error) ? Validation.Ok : Validation.Fail(error);
			protected override void Apply(ReasonCode reasonCode) => Basis.ReasonCodeID = reasonCode.ReasonCodeID;
			protected override void ReportSuccess(ReasonCode reasonCode) => Basis.Reporter.Info(Msg.Ready, reasonCode.Descr ?? reasonCode.ReasonCodeID);
			protected override void ClearState() => Basis.ReasonCodeID = null;

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string Prompt = "Scan the barcode of the reason code.";
				public const string Ready = "The {0} reason code is selected.";
				public const string Missing = "The {0} reason code is not found.";
				public const string NotSet = "The reason code is not selected.";
			}
			#endregion
		}
		#endregion

		#region Commands
		public abstract class ReleaseCommand : ScanCommand
		{
			public override string Code => "RELEASE";
			public override string ButtonName => "scanRelease";
			public override string DisplayName => Msg.DisplayName;
			protected override bool IsEnabled => Basis.DocumentIsEditable && Basis.NotReleasedAndHasLines;

			protected override bool Process()
			{
				if (Basis.Document != null)
				{
					if (Basis.Document.Released == true)
					{
						Basis.ReportError(Messages.Document_Status_Invalid);
						return true;
					}

					if (Basis.Document.Hold != false)
						Basis.DocumentView.SetValueExt<INRegister.hold>(Basis.Document, false);
					Basis.Save.Press();

					var msg = (DocumentIsReleased, DocumentReleaseFailed);

					Basis
					.WaitFor<INRegister>((basis, doc) =>
					{
						INDocumentRelease.ReleaseDoc(new List<INRegister>() { doc }, false);
						basis.CurrentMode.Commands.OfType<ReleaseCommand>().FirstOrDefault()?.OnAfterRelease(doc);
					})
					.WithDescription(DocumentReleasing, Basis.Document.RefNbr)
					.ActualizeDataBy((basis, doc) => INRegister.PK.Find(basis, doc))
					.OnSuccess(x => x.Say(msg.DocumentIsReleased))
					.OnFail(x => x.Say(msg.DocumentReleaseFailed))
					.BeginAwait(Basis.Document);

					return true;
				}
				return false;
			}

			protected virtual void OnAfterRelease(INRegister doc) { }

			protected abstract string DocumentReleasing { get; }
			protected abstract string DocumentIsReleased { get; }
			protected abstract string DocumentReleaseFailed { get; }

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string DisplayName = "Release";
			}
			#endregion
		}
		#endregion

		#region Redirect
		public new abstract class RedirectFrom<TForeignBasis> : WarehouseManagementSystem<TSelf, TGraph>.RedirectFrom<TForeignBasis>
			where TForeignBasis : PXGraphExtension, IBarcodeDrivenStateMachine
		{
			public override bool IsPossible => PXAccess.FeatureInstalled<FeaturesSet.wMSInventory>();
		}
		#endregion
	}

	public sealed class RegisterScanHeader : PXCacheExtension<WMSScanHeader, QtyScanHeader, ScanHeader>
	{
		#region DocType
		[PXUnboundDefault(typeof(INRegister.docType))]
		[PXString(1, IsFixed = true)]
		[INDocType.List]
		public string DocType { get; set; }
		public abstract class docType : BqlString.Field<docType> { }
		#endregion
		#region ReasonCodeID
		[PXString]
		[PXSelector(typeof(SearchFor<ReasonCode.reasonCodeID>))]
		[PXRestrictor(typeof(Where<ReasonCode.usage.IsEqual<docType.FromCurrent>>), Messages.ReasonCodeDoesNotMatch)]
		public string ReasonCodeID { get; set; }
		public abstract class reasonCodeID : BqlString.Field<reasonCodeID> { }
		#endregion
	}
}