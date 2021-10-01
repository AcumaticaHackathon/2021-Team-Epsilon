using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.Common;
using PX.Objects.IN.DAC;

using PX.BarcodeProcessing;

namespace PX.Objects.IN.WMS
{
	using CartSelf = INScanTransferCartSupport;
	using CartBase = CartSupport<INScanTransfer, INScanTransfer.Host>;

	public class INScanTransferCartSupport : CartBase
	{
		public static bool IsActive() => IsActiveBase();

		public override bool IsCartRequired() => Basis.Setup.Current.UseCartsForTransfers == true;

		public virtual bool IsCartEmpty => !CartSplitLinks.SelectMain().Any();

		public virtual INRegisterCart[] GetCartRegisters(INCart cart)
		{
			INRegisterCart[] carts =
				SelectFrom<INRegisterCart>.
				Where<INRegisterCart.FK.Cart.SameAsCurrent>.
				View.ReadOnly
				.SelectMultiBound(Basis, new object[] { cart })
				.RowCast<INRegisterCart>()
				.ToArray();

			return carts;
		}

		public decimal? GetCartQty(INTran line)
		{
			if (line == null) return null;

			INRegisterCartLine docCartLine = CartSplitLinks.Search<INRegisterCartLine.lineNbr>(line.LineNbr);
			return docCartLine?.Qty;
		}

		#region Views
		public
			SelectFrom<INRegisterCart>.
			Where<
				INRegisterCart.FK.Cart.SameAsCurrent.
				And<INRegisterCart.FK.Register.SameAsCurrent>>.
			View CartsLinks; // RegisterCart

		public
			SelectFrom<INRegisterCartLine>.
			InnerJoin<INCartSplit>.On<INRegisterCartLine.FK.CartSplit>.
			Where<INRegisterCartLine.FK.Cart.SameAsCurrent>.
			View CartSplitLinks; // RegisterCartLines

		/// <summary>
		/// Delegate for <see cref="INTransferEntry.transactions"/>
		/// </summary>
		public IEnumerable Transactions()
		{
			if (!IsCartRequired())
				return null;

			PXDelegateResult sorted = new PXDelegateResult
			{
				IsResultSorted = true,
				IsResultFiltered = true,
				IsResultTruncated = true
			};
			var view = new PXView(Base, false, Base.transactions.View.BqlSelect);
			int startRow = PXView.StartRow;
			int totalRow = 0;
			var lines = view.Select(
				PXView.Currents, PXView.Parameters, PXView.Searches, PXView.SortColumns, PXView.Descendings, PXView.Filters,
				ref startRow, PXView.MaximumRows, ref totalRow)
				.OfType<INTran>()
				.Select(x => new { Line = x, IsInCart = GetCartQty(x) > 0 })
				.OrderByDescending(x => x.IsInCart)
				.Select(x => x.Line)
				.ToList();
			sorted.AddRange(lines);
			return sorted;
		}
		#endregion

		#region Overrides
		/// <summary>
		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanMode(ScanMode{TSelf})"/>
		/// </summary>
		[PXOverride]
		public virtual ScanMode<INScanTransfer> DecorateScanMode(ScanMode<INScanTransfer> original, Func<ScanMode<INScanTransfer>, ScanMode<INScanTransfer>> base_DecorateScanMode)
		{
			var mode = base_DecorateScanMode(original);

			if (!IsCartRequired())
				return mode;

			if (mode is INScanTransfer.TransferMode transfer)
			{
				InjectCartState(transfer);
				InjectCartCommands(transfer);

				transfer.Intercept.ResetMode.ByBaseSubstitute(
					(basis, fullReset, base_ResetMode) =>
					{
						var cartSupport = basis.Get<CartSelf>();
						basis.Clear<INScanTransfer.WarehouseState>(when: fullReset);
						basis.Clear<CartState>(when: fullReset && !basis.IsWithinReset);
						basis.Clear<INScanTransfer.TransferMode.SourceLocationState>(when: fullReset || basis.PromptLocationForEveryLine);
						basis.Clear<INScanTransfer.TransferMode.TargetLocationState>(when: fullReset || basis.PromptLocationForEveryLine);
						basis.Clear<INScanTransfer.ReasonCodeState>(when: fullReset || basis.PromptLocationForEveryLine);
						basis.Clear<INScanTransfer.InventoryItemState>();
						basis.Clear<INScanTransfer.LotSerialState>();
					});

				transfer.Intercept.CreateTransitions.ByReplace(
					basis =>
					{
						if (basis.PromptLocationForEveryLine)
						{
							return basis.StateFlow(flow => flow
								.From<INScanTransfer.WarehouseState>()
								.NextTo<CartState>()
								.NextTo<INScanTransfer.TransferMode.SourceLocationState>()
								.NextTo<INScanTransfer.InventoryItemState>()
								.NextTo<INScanTransfer.LotSerialState>()
								.NextTo<INScanTransfer.TransferMode.TargetLocationState>()
								.NextTo<INScanTransfer.ReasonCodeState>());
						}
						else
						{
							return basis.StateFlow(flow => flow
								.From<INScanTransfer.WarehouseState>()
								.NextTo<CartState>()
								.NextTo<INScanTransfer.TransferMode.SourceLocationState>()
								.NextTo<INScanTransfer.TransferMode.TargetLocationState>()
								.NextTo<INScanTransfer.ReasonCodeState>()
								.NextTo<INScanTransfer.InventoryItemState>()
								.NextTo<INScanTransfer.LotSerialState>());
						}
					});
			}

			return mode;
		}

		/// <summary>
		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanCommand(ScanCommand{TSelf})"/>
		/// </summary>
		[PXOverride]
		public virtual ScanCommand<INScanTransfer> DecorateScanCommand(ScanCommand<INScanTransfer> original, Func<ScanCommand<INScanTransfer>, ScanCommand<INScanTransfer>> base_DecorateScanCommand)
		{
			var command = base_DecorateScanCommand(original);

			if (!IsCartRequired())
				return command;

			if (command is INScanTransfer.ReleaseCommand release)
				release.Intercept.IsEnabled.ByConjoin(basis => basis.Get<CartSelf>().IsCartEmpty);

			void ResetLocationsOn(ScanCommand<INScanTransfer> cartCommand)
			{
				cartCommand.Intercept.Process.ByOverride(
					(basis, base_Process) =>
					{
						basis.LocationID = null;
						basis.ToLocationID = null;
						return base_Process();
					});
			}

			if (command is CartBase.CartIn cartIn)
				ResetLocationsOn(cartIn);

			if (command is CartBase.CartOut cartOut)
				ResetLocationsOn(cartOut);

			if (command is INScanTransfer.RemoveCommand remove)
				ResetLocationsOn(remove);

			return command;
		}

		/// <summary>
		/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanState(ScanState{TSelf})"/>
		/// </summary>
		[PXOverride]
		public virtual ScanState<INScanTransfer> DecorateScanState(ScanState<INScanTransfer> original, Func<ScanState<INScanTransfer>, ScanState<INScanTransfer>> base_DecorateScanState)
		{
			var state = base_DecorateScanState(original);

			if (!IsCartRequired())
				return state;

			if (state is CartState cartState)
			{
				cartState
					.Intercept.Validate.ByAppend(
						(basis, cart) =>
						{
							INRegisterCart[] carts = basis.Get<CartSelf>().GetCartRegisters(cart);

							if (carts.Length > 1 || (carts.Length == 1 && carts[0].DocType != INDocType.Transfer))
								return Validation.Fail(CartState.Msg.IsOccupied, cart.CartCD);

							return Validation.Ok;
						})
					.Intercept.Apply.ByAppend(
						(basis, cart) =>
						{
							var cartSupport = basis.Get<CartSelf>();
							INRegisterCart[] carts = cartSupport.GetCartRegisters(cart);
							if (carts.Length == 1)
							{
								cartSupport.CartsLinks.Current = carts[0];
								basis.RefNbr = carts[0].RefNbr;
								basis.ToSiteID = basis.Document?.ToSiteID;
							}
						});
			}

			if (state is INScanTransfer.InventoryItemState itemState)
			{
				itemState
					.Intercept.HandleAbsence.ByOverride(
						(basis, barcode, base_HandleAbsence) =>
						{
							var cartSupport = basis.Get<CartSelf>();
							if (cartSupport.CartLoaded == true)
							{
								if (basis.TryProcessBy<INScanTransfer.TransferMode.TargetLocationState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication))
									return AbsenceHandling.Done;

								return AbsenceHandling.Skipped;
							}
							else
							{
								return base_HandleAbsence(barcode);
							}
						});
			}

			if (state is INScanTransfer.TransferMode.SourceLocationState sourceLocationState)
				sourceLocationState.Intercept.IsStateActive.ByConjoin(basis => basis.Get<CartSelf>().CartLoaded == false);

			if (state is INScanTransfer.TransferMode.TargetLocationState targetLocationState)
				targetLocationState.Intercept.IsStateActive.ByConjoin(basis => basis.Get<CartSelf>().CartLoaded == true);

			if (state is INScanTransfer.ReasonCodeState reasonState)
				reasonState.Intercept.IsStateActive.ByConjoin(basis => basis.Get<CartSelf>().CartLoaded == true);

			return state;
		}

		/// <summary>
		/// Overrides <see cref="INScanTransfer.TransferMode.ConfirmState.Logic"/>
		/// </summary>
		//[PXProtectedAccess(typeof(INScanTransfer.TransferMode.ConfirmState.Logic))] // TODO: use protected access once it is fixed for the current case
		public class AlterConfirmStateLogic : PXGraphExtension<INScanTransfer.TransferMode.ConfirmState.Logic, INScanTransfer, INScanTransfer.Host>
		{
			public static bool IsActive() => CartSelf.IsActive();

			public INScanTransfer Basis => Base1;
			public INScanTransfer.TransferMode.ConfirmState.Logic State => Base2;
			public INScanTransferCartSupport CartBasis => Basis.Get<CartSelf>();

			#region Protected access members
			///// <summary>
			///// Uses <see cref="INScanTransfer.TransferMode.ConfirmState.Logic.EnsureDocument"/>
			///// </summary>
			//[PXProtectedAccess]
			//protected abstract INRegister EnsureDocument();
			protected virtual INRegister EnsureDocument()
			{
				if (Basis.Document == null)
					Basis.DocumentView.Insert();

				Basis.DocumentView.SetValueExt<INRegister.siteID>(Basis.Document, Basis.SiteID);
				Basis.DocumentView.SetValueExt<INRegister.toSiteID>(Basis.Document, Basis.ToSiteID);

				return Basis.DocumentView.Update(Basis.Document);
			}

			///// <summary>
			///// Uses <see cref="INScanTransfer.TransferMode.ConfirmState.Logic.HasErrors(INTran, out FlowStatus)"/>
			///// </summary>
			//[PXProtectedAccess]
			//protected abstract bool HasErrors(INTran line, out FlowStatus error);
			protected virtual bool HasErrors(INTran tran, out FlowStatus error)
			{
				if (HasLotSerialError(tran, out error))
					return true;
				if (HasLocationError(tran, out error))
					return true;
				if (HasAvailabilityError(tran, out error))
					return true;

				error = FlowStatus.Ok;
				return false;
			}

			private bool HasLotSerialError(INTran tran, out FlowStatus error)
			{
				if (Basis.HasActive<INScanTransfer.LotSerialState>() && !string.IsNullOrEmpty(Basis.LotSerialNbr) && Basis.Graph.splits.SelectMain().Any(s => s.LotSerialNbr != Basis.LotSerialNbr))
				{
					error = FlowStatus.Fail(
						INScanTransfer.TransferMode.ConfirmState.Msg.QtyExceedsOnLot,
						Basis.LotSerialNbr,
						Basis.SightOf<WMSScanHeader.inventoryID>());
					return true;
				}

				error = FlowStatus.Ok;
				return false;
			}

			private bool HasLocationError(INTran tran, out FlowStatus error)
			{
				if (Basis.HasActive<INScanTransfer.TransferMode.SourceLocationState>() && Basis.Graph.splits.SelectMain().Any(s => s.LocationID != Basis.LocationID))
				{
					error = FlowStatus.Fail(
						INScanTransfer.TransferMode.ConfirmState.Msg.QtyExceedsOnLocation,
						Basis.SightOf<WMSScanHeader.locationID>(),
						Basis.SightOf<WMSScanHeader.inventoryID>());
					return true;
				}

				error = FlowStatus.Ok;
				return false;
			}

			private bool HasAvailabilityError(INTran tran, out FlowStatus error)
			{
				var errorInfo = Basis.Graph.lsselect.GetAvailabilityCheckErrors(Basis.Details.Cache, tran).FirstOrDefault();
				if (errorInfo != null)
				{
					PXCache lsCache = Basis.Graph.lsselect.Cache;
					error = FlowStatus.Fail(errorInfo.MessageFormat, new object[]
					{
						lsCache.GetStateExt<INTran.inventoryID>(tran),
						lsCache.GetStateExt<INTran.subItemID>(tran),
						lsCache.GetStateExt<INTran.siteID>(tran),
						lsCache.GetStateExt<INTran.locationID>(tran),
						lsCache.GetValue<INTran.lotSerialNbr>(tran)
					});
					return true;
				}

				error = FlowStatus.Ok;
				return false;
			}
			#endregion

			/// <summary>
			/// Overrides <see cref="INScanTransfer.TransferMode.ConfirmState.Logic.Confirm"/>
			/// </summary>
			[PXOverride]
			public virtual FlowStatus Confirm(Func<FlowStatus> base_Confirm)
			{
				if (CartBasis.IsCartRequired())
					return ConfirmCartProcess();
				else
					return base_Confirm();
			}


			protected virtual FlowStatus ConfirmCartProcess()
			{
				if (!CanConfirmCartProcess(out var error))
					return error;

				if (Basis.Remove == false)
				{
					if (CartBasis.CartLoaded == false)
						return MoveToCart();
					else
						return MoveFromCart();
				}
				else
				{
					if (CartBasis.CartLoaded == false)
						return RemoveFromCart();
					else
						return ReturnToCart();
				}
			}

			protected virtual bool CanConfirmCartProcess(out FlowStatus error)
			{
				if (Basis.HasActive<INScanTransfer.LotSerialState>() && Basis.LotSerialNbr == null)
				{
					error = FlowStatus.Fail(INScanTransfer.LotSerialState.Msg.NotSet);
					return false;
				}

				if (Basis.HasActive<INScanTransfer.TransferMode.TargetLocationState>()
					&& CartBasis.CartLoaded == true
					&& Basis.ToLocationID == null)
				{
					error = FlowStatus.Fail(INScanTransfer.TransferMode.TargetLocationState.Msg.NotSet);
					return false;
				}

				if (Basis.SelectedLotSerialClass.LotSerTrack == INLotSerTrack.SerialNumbered
					&& Basis.SelectedLotSerialClass.LotSerAssign.IsNotIn(null, INLotSerAssign.WhenUsed)
					&& Basis.Qty != 1)
				{
					error = FlowStatus.Fail(INScanTransfer.InventoryItemState.Msg.SerialItemNotComplexQty);
					return false;
				}

				error = FlowStatus.Ok;
				return true;
			}

			protected virtual FlowStatus MoveToCart()
			{
				// !CartLoaded && !Remove

				decimal? qty = +Basis.Qty;

				INRegister doc = EnsureDocument();
				INTran existLine = FindFixedLineFromCart();

				if (!SyncWithLines(qty, ref existLine, out FlowStatus error))
					return error;

				INCartSplit cartSplit = SyncWithCart(qty);
				SyncWithDocumentCart(existLine, cartSplit, qty);

				Basis.DispatchNext(CartSelf.Msg.InventoryAdded, Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM);

				return FlowStatus.Ok;
			}

			protected virtual FlowStatus RemoveFromCart()
			{
				// !CartLoaded && Remove

				decimal? qty = -Basis.Qty;

				INRegister doc = EnsureDocument();
				INTran existLine = FindFixedLineFromCart();

				if (!SyncWithLines(qty, ref existLine, out FlowStatus error))
					return error;

				INCartSplit cartSplit = SyncWithCart(qty);
				SyncWithDocumentCart(existLine, cartSplit, qty);

				Basis.DispatchNext(CartSelf.Msg.InventoryRemoved, Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM);

				return FlowStatus.Ok;
			}

			protected virtual FlowStatus MoveFromCart()
			{
				// CartLoaded && !Remove

				decimal? qty = Basis.Qty;

				//get item from the cart
				INTran existLine = FindAnyLineFromCart();
				if (existLine == null)
					return LineMissingStatus();
				Basis.LocationID = existLine.LocationID;

				if (!SyncWithLines(-qty, ref existLine, out FlowStatus subError))
					return subError;

				INCartSplit cartSplit = SyncWithCart(-qty);
				SyncWithDocumentCart(existLine, cartSplit, -qty);

				Basis.SiteID = existLine.SiteID;
				Basis.LotSerialNbr = existLine.LotSerialNbr;
				Basis.ReasonCodeID = existLine.ReasonCode;
				Basis.ExpireDate = existLine.ExpireDate;
				Basis.SubItemID = existLine.SubItemID;

				//put item to the location
				existLine = FindLineFromDocument();
				if (!SyncWithLines(qty, ref existLine, out FlowStatus addError))
					return addError;

				if (CartBasis.IsCartEmpty)
				{
					CartBasis.CartLoaded = false;
					Basis.DispatchNext(Msg.CartIsEmpty, Basis.SightOf<CartScanHeader.cartID>());
				}
				else
				{
					Basis.DispatchNext(
						INScanTransfer.TransferMode.ConfirmState.Msg.InventoryAdded,
						Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM);
				}

				return FlowStatus.Ok;
			}

			protected virtual FlowStatus ReturnToCart()
			{
				// CartLoaded && Remove

				decimal? qty = Basis.Qty;

				INTran existLine = FindLineFromDocument();
				if (existLine == null)
					return LineMissingStatus();

				if (!SyncWithLines(-qty, ref existLine, out FlowStatus subError))
					return subError;

				Basis.LocationID = existLine.LocationID;
				Basis.ToLocationID = existLine.LocationID;
				Basis.SiteID = existLine.SiteID;
				Basis.LotSerialNbr = existLine.LotSerialNbr;
				Basis.ReasonCodeID = existLine.ReasonCode;
				Basis.ExpireDate = existLine.ExpireDate;
				Basis.SubItemID = existLine.SubItemID;

				//put item to the cart
				existLine = FindFixedLineFromCart();
				if (!SyncWithLines(qty, ref existLine, out FlowStatus addError))
					return addError;

				INCartSplit cartSplit = SyncWithCart(qty);
				SyncWithDocumentCart(existLine, cartSplit, qty);

				Basis.DispatchNext(
					INScanTransfer.TransferMode.ConfirmState.Msg.InventoryRemoved,
					Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM);

				return FlowStatus.Ok;
			}


			protected virtual INTran FindAnyLineFromCart() => FindLine(line => CartBasis.GetCartQty(line) != null);

			protected virtual INTran FindFixedLineFromCart()
			{
				return FindLine(line =>
					(CartBasis.CartLoaded == false
						? line.LocationID == Basis.LocationID
						: line.ToLocationID == Basis.ToLocationID) &&
					CartBasis.GetCartQty(line) != null);
			}

			protected virtual INTran FindLineFromDocument()
			{
				return FindLine(line =>
					(CartBasis.CartLoaded == false
						? line.LocationID == Basis.LocationID
						: line.ToLocationID == Basis.ToLocationID) &&
					CartBasis.GetCartQty(line) == null);
			}

			protected virtual INTran FindLine(Func<INTran, bool> search)
			{
				var existLines = Base.transactions.SelectMain().Where(t =>
					t.InventoryID == Basis.InventoryID &&
					t.SiteID == Basis.SiteID &&
					t.ToSiteID == Basis.ToSiteID &&
					t.ReasonCode == (Basis.ReasonCodeID ?? t.ReasonCode) &&
					t.UOM == Basis.UOM &&
					(search == null || search(t)));

				INTran existLine = null;

				if (Basis.HasActive<INScanTransfer.LotSerialState>())
				{
					foreach (var line in existLines)
					{
						Base.transactions.Current = line;
						if (Base.splits.SelectMain().Any(t => (t.LotSerialNbr ?? "") == (Basis.LotSerialNbr ?? "")))
						{
							existLine = line;
							break;
						}
					}
				}
				else
				{
					existLine = existLines.FirstOrDefault();
				}
				return existLine;
			}

			protected virtual FlowStatus LineMissingStatus() => FlowStatus.Fail(INScanTransfer.TransferMode.ConfirmState.Msg.LineMissing, Basis.SightOf<WMSScanHeader.inventoryID>());

			protected virtual bool SyncWithLines(decimal? qty, ref INTran line, out FlowStatus flowStatus)
			{
				Action rollbackAction = null;
				INTran existLine = line;

				if (existLine != null)
				{
					decimal? newQty = existLine.Qty + qty;

					if (newQty == 0) // remove
					{
						var backup = PXCache<INTran>.CreateCopy(existLine);

						Basis.Details.Cache.SetValueExt<INTran.qty>(existLine, newQty);
						existLine = Basis.Details.Delete(existLine);

						rollbackAction = () => Basis.Details.Insert(backup);
					}
					else
					{
						if (Basis.SelectedLotSerialClass.LotSerTrack == INLotSerTrack.SerialNumbered && newQty != 1)
						{
							flowStatus = FlowStatus.Fail(INScanTransfer.InventoryItemState.Msg.SerialItemNotComplexQty);
							return false;
						}

						var backup = PXCache<INTran>.CreateCopy(existLine);

						Basis.Details.Cache.SetValueExt<INTran.qty>(existLine, newQty);
						Basis.Details.Cache.SetValueExt<INTran.lotSerialNbr>(existLine, null);
						existLine = Basis.Details.Update(existLine);

						Basis.Details.Cache.SetValueExt<INTran.lotSerialNbr>(existLine, Basis.LotSerialNbr);
						existLine = Basis.Details.Update(existLine);

						rollbackAction = () =>
						{
							Basis.Details.Cache.Delete(existLine);
							Basis.Details.Cache.Insert(backup);
						};
					}
				}
				else
				{
					if (qty < 0) // remove
					{
						flowStatus = LineMissingStatus();
						return false;
					}
					else
					{
						existLine = Basis.Details.Insert();
						var setter = Basis.Details.GetSetterForCurrent().WithEventFiring;

						setter.Set(tr => tr.InventoryID, Basis.InventoryID);
						setter.Set(tr => tr.SiteID, Basis.SiteID);
						setter.Set(tr => tr.ToSiteID, Basis.ToSiteID);
						setter.Set(tr => tr.LocationID, Basis.LocationID);

						if (CartBasis.CartLoaded == false)
							setter.Set(tr => tr.ToLocationID, Basis.LocationID);
						else
							setter.Set(tr => tr.ToLocationID, Basis.ToLocationID);

						setter.Set(tr => tr.UOM, Basis.UOM);
						setter.Set(tr => tr.ReasonCode, Basis.ReasonCodeID);
						existLine = Basis.Details.Update(existLine);

						Basis.Details.Cache.SetValueExt<INTran.qty>(existLine, qty);
						existLine = Basis.Details.Update(existLine);

						Basis.Details.Cache.SetValueExt<INTran.lotSerialNbr>(existLine, Basis.LotSerialNbr);
						existLine = Basis.Details.Update(existLine);

						rollbackAction = () => Basis.Details.Cache.Delete(existLine);
					}
				}

				if (Basis.Remove == false && HasErrors(existLine, out flowStatus))
				{
					rollbackAction?.Invoke();
					return false;
				}

				flowStatus = FlowStatus.Ok;
				line = existLine;
				return true;
			}

			protected virtual INCartSplit SyncWithCart(decimal? qty)
			{
				INCartSplit cartSplit = CartBasis.CartSplits.Search<INCartSplit.inventoryID, INCartSplit.subItemID, INCartSplit.fromLocationID, INCartSplit.lotSerialNbr>(
					Basis.InventoryID, Basis.SubItemID, Basis.LocationID, string.IsNullOrEmpty(Basis.LotSerialNbr) ? null : Basis.LotSerialNbr);
				if (cartSplit == null)
				{
					cartSplit = CartBasis.CartSplits.Insert(new INCartSplit
					{
						CartID = CartBasis.CartID,
						InventoryID = Basis.InventoryID,
						SubItemID = Basis.SubItemID,
						LotSerialNbr = Basis.LotSerialNbr,
						ExpireDate = Basis.ExpireDate,
						UOM = Basis.UOM,
						SiteID = Basis.SiteID,
						FromLocationID = Basis.LocationID,
						Qty = qty
					});
				}
				else
				{
					var copy = (INCartSplit)CartBasis.CartSplits.Cache.CreateCopy(cartSplit);
					copy.Qty += qty;
					cartSplit = CartBasis.CartSplits.Update(copy);

					if (cartSplit.Qty == 0)
						cartSplit = CartBasis.CartSplits.Delete(cartSplit);
				}

				return cartSplit;
			}

			protected virtual void SyncWithDocumentCart(INTran line, INCartSplit cartSplit, decimal? qty)
			{
				if (CartBasis.CartsLinks.Current == null)
					CartBasis.CartsLinks.Insert();

				CartBasis.CartsLinks.Cache.SetValue<INRegisterCart.docType>(CartBasis.CartsLinks.Current, Basis.Document.DocType);
				CartBasis.CartsLinks.Cache.SetValue<INRegisterCart.refNbr>(CartBasis.CartsLinks.Current, Basis.Document.RefNbr);

				SyncWithDocumentCartLine(line, cartSplit, qty);

				if (CartBasis.IsCartEmpty)
					CartBasis.CartsLinks.DeleteCurrent();
			}

			protected virtual void SyncWithDocumentCartLine(INTran line, INCartSplit cartSplit, decimal? qty)
			{
				bool emptyLine = line.Qty.GetValueOrDefault() == 0;

				INRegisterCartLine docCartLine = CartBasis.CartSplitLinks.Search<INRegisterCartLine.lineNbr>(line.LineNbr);
				if (docCartLine == null)
				{
					if (qty <= 0)
						throw new PXArgumentException(nameof(qty));
					docCartLine = CartBasis.CartSplitLinks.Insert();
					CartBasis.CartSplitLinks.Cache.SetValue<INRegisterCartLine.cartSplitLineNbr>(docCartLine, cartSplit.SplitLineNbr);
				}
				docCartLine = (INRegisterCartLine)CartBasis.CartSplitLinks.Cache.CreateCopy(docCartLine);
				docCartLine.Qty += qty;
				CartBasis.CartSplitLinks.Cache.Update(docCartLine);

				if (docCartLine.Qty == 0)
					CartBasis.CartSplitLinks.Delete(docCartLine);
			}
		}
		#endregion

		#region Attached Fields
		[PXUIField(DisplayName = Msg.CartQty)]
		public class CartQty : PXFieldAttachedTo<INTran>.By<INScanTransfer.Host>.AsDecimal.Named<CartQty>
		{
			public override decimal? GetValue(INTran row) => Base.FindImplementation<CartSelf>()?.GetCartQty(row);
			protected override bool? Visible => CartSelf.IsActive() && Base.FindImplementation<CartSelf>()?.IsCartRequired() == true;
		}
		#endregion
	}
}