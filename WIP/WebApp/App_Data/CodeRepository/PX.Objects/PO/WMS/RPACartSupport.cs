using System;
using System.Linq;
using System.Collections.Generic;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.BarcodeProcessing;
using PX.Objects.IN;
using PX.Objects.IN.WMS;

namespace PX.Objects.PO.WMS
{
	using CartBase = CartSupport<ReceivePutAway, ReceivePutAway.Host>;

	public class RPACartSupport : CartBase
	{
		public static bool IsActive() => IsActiveBase();

		public override bool IsCartRequired() => Basis.Setup.Current.UseCartsForPutAway == true && Basis.Header.Mode == ReceivePutAway.PutAwayMode.Value;

		#region Views
		public
			SelectFrom<POCartReceipt>.
			View CartsLinks;

		public
			SelectFrom<INCartSplit>.
			InnerJoin<INCart>.On<INCartSplit.FK.Cart>.
			InnerJoin<POCartReceipt>.On<POCartReceipt.FK.Cart>.
			Where<POCartReceipt.FK.Receipt.SameAsCurrent>.
			View AllCartSplits;

		public
			SelectFrom<POReceiptSplitToCartSplitLink>.
			InnerJoin<INCartSplit>.On<POReceiptSplitToCartSplitLink.FK.CartSplit>.
			Where<POReceiptSplitToCartSplitLink.FK.Cart.SameAsCurrent>.
			View CartSplitLinks;

		public
			SelectFrom<POReceiptSplitToCartSplitLink>.
			InnerJoin<INCartSplit>.On<POReceiptSplitToCartSplitLink.FK.CartSplit>.
			InnerJoin<INCart>.On<INCartSplit.FK.Cart>.
			InnerJoin<POCartReceipt>.On<POCartReceipt.FK.Cart>.
			Where<POCartReceipt.FK.Receipt.SameAsCurrent>.
			View AllCartSplitLinks;
		#endregion

		#region Overrides
		/// <summary>
		/// Overrides <see cref="ReceivePutAway.PutAwayMode.Logic"/>
		/// </summary>
		public class AlterPutAwayModeLogic : PXGraphExtension<ReceivePutAway.PutAwayMode.Logic, ReceivePutAway, ReceivePutAway.Host>
		{
			public static bool IsActive() => RPACartSupport.IsActive();

			public ReceivePutAway Basis => Base1;
			public ReceivePutAway.PutAwayMode.Logic Mode => Base2;
			public RPACartSupport CartBasis => Basis.Get<RPACartSupport>();

			/// <summary>
			/// Overrides <see cref="ReceivePutAway.PutAwayMode.Logic.CanPutAway"/>
			/// </summary>
			[PXOverride]
			public virtual bool get_CanPutAway(Func<bool> base_CanPutAway)
			{
				return CartBasis.IsCartRequired() == false || CartBasis.CartLoaded == false
					? base_CanPutAway()
					: Mode.PutAway.SelectMain().Any(s => CartBasis.GetCartQty(s) > 0);
			}

			/// <summary>
			/// Overrides <see cref="ReceivePutAway.PutAwayMode.Logic.GetTransfer"/>
			/// </summary>
			[PXOverride]
			public virtual INRegister GetTransfer(Func<INRegister> base_GetTransfer)
			{
				return CartBasis.IsCartRequired()
					? 	SelectFrom<INRegister>.
						InnerJoin<POCartReceipt>.On<
							INRegister.docType.IsEqual<INDocType.transfer>.
							And<POCartReceipt.transferNbr.IsEqual<INRegister.refNbr>>>.
						Where<
							INRegister.transferType.IsEqual<INTransferType.oneStep>.
							And<INRegister.released.IsEqual<False>>.
							And<POCartReceipt.receiptNbr.IsEqual<WMSScanHeader.refNbr.FromCurrent>>.
							And<POCartReceipt.siteID.IsEqual<WMSScanHeader.siteID.FromCurrent>>.
							And<POCartReceipt.cartID.IsEqual<CartScanHeader.cartID.FromCurrent>>>.
						View.ReadOnly.SelectSingleBound(Basis, new[] { Basis.Header })
					: base_GetTransfer();
			}

			/// <summary>
			/// Overrides <see cref="ReceivePutAway.PutAwayMode.Logic.OnTransferEntryCreated(INTransferEntry)"/>
			/// </summary>
			[PXOverride]
			public virtual void OnTransferEntryCreated(INTransferEntry transferEntry, Action<INTransferEntry> base_OnTransferEntryCreated)
			{
				base_OnTransferEntryCreated(transferEntry);
				if (Basis.TransferRefNbr != null)
					CartBasis.EnsureCartReceiptLink();
			}
			
			/// <summary>
			/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanMode(ScanMode{TSelf})"/>
			/// </summary>
			[PXOverride]
			public virtual ScanMode<ReceivePutAway> DecorateScanMode(ScanMode<ReceivePutAway> original, Func<ScanMode<ReceivePutAway>, ScanMode<ReceivePutAway>> base_DecorateScanMode)
			{
				var rpaMode = base_DecorateScanMode(original);

				if (rpaMode is ReceivePutAway.PutAwayMode putAway)
				{
					CartBasis.InjectCartState(putAway, makeItDefault: true);
					CartBasis.InjectCartCommands(putAway);

					putAway.Intercept.ResetMode.ByBaseSubstitute(
						(basis, fullReset, base_ResetMode) =>
						{
							var cartSupport = basis.Get<RPACartSupport>();
							var mode = basis.Get<ReceivePutAway.PutAwayMode.Logic>();
							if (cartSupport.IsCartRequired())
							{
								basis.Clear<CartState>(when: fullReset && !basis.IsWithinReset);
								basis.Clear<ReceivePutAway.PutAwayMode.ReceiptState>(when: fullReset && !basis.IsWithinReset);
								basis.Clear<ReceivePutAway.PutAwayMode.SourceLocationState>(when: fullReset || cartSupport.CartLoaded == false && mode.PromptLocationForEveryLine && mode.IsSingleLocation == false);
								basis.Clear<ReceivePutAway.PutAwayMode.TargetLocationState>(when: fullReset || cartSupport.CartLoaded == true && mode.PromptLocationForEveryLine);
								basis.Clear<ReceivePutAway.PutAwayMode.InventoryItemState>();
								basis.Clear<ReceivePutAway.PutAwayMode.LotSerialState>();
							}
							else
								base_ResetMode(fullReset);
						});

					putAway.Intercept.CreateTransitions.ByOverride(
						(basis, base_CreateTransitions) =>
						{
							var cartSupport = basis.Get<RPACartSupport>();
							var mode = basis.Get<ReceivePutAway.PutAwayMode.Logic>();
							if (cartSupport.IsCartRequired())
							{
								if (cartSupport.CartLoaded == false)
								{
									return basis.StateFlow(flow => flow
										.From<CartState>()
										.NextTo<ReceivePutAway.PutAwayMode.ReceiptState>()
										.NextTo<ReceivePutAway.PutAwayMode.SourceLocationState>()
										.NextTo<ReceivePutAway.PutAwayMode.InventoryItemState>()
										.NextTo<ReceivePutAway.PutAwayMode.LotSerialState>());
								}
								else if (mode.PromptLocationForEveryLine)
								{
									return basis.StateFlow(flow => flow
										.From<CartState>()
										.NextTo<ReceivePutAway.PutAwayMode.ReceiptState>()
										.NextTo<ReceivePutAway.PutAwayMode.InventoryItemState>()
										.NextTo<ReceivePutAway.PutAwayMode.LotSerialState>()
										.NextTo<ReceivePutAway.PutAwayMode.TargetLocationState>());
								}
								else
								{
									return basis.StateFlow(flow => flow
										.From<CartState>()
										.NextTo<ReceivePutAway.PutAwayMode.ReceiptState>()
										.NextTo<ReceivePutAway.PutAwayMode.TargetLocationState>()
										.NextTo<ReceivePutAway.PutAwayMode.InventoryItemState>()
										.NextTo<ReceivePutAway.PutAwayMode.LotSerialState>());
								}
							}
							else
								return base_CreateTransitions();
						});
				}

				return rpaMode;
			}

			#region States
			/// <summary>
			/// Overrides <see cref="ReceivePutAway.PutAwayMode.ReceiptState.Logic"/>
			/// </summary>
			public class AlterRefNbrStateLogic : PXGraphExtension<ReceivePutAway.PutAwayMode.ReceiptState.Logic, ReceivePutAway, ReceivePutAway.Host>
			{
				public static bool IsActive() => RPACartSupport.IsActive();

				public ReceivePutAway Basis => Base1;
				public RPACartSupport CartBasis => Basis.Get<RPACartSupport>();

				/// <summary>
				/// Overrides <see cref="ReceivePutAway.PutAwayMode.ReceiptState.Logic.CanBePutAway(POReceipt, out Validation)"/>
				/// </summary>
				[PXOverride]
				public virtual bool CanBePutAway(POReceipt receipt, out Validation error, CanBePutAwayDelegate base_CanBePutAway)
				{
					if (CartBasis.CartID > 0 && Basis.SiteID != receipt.SiteID)
					{
						error = Validation.Fail(CartState.Msg.InvalidSite, CartBasis.SelectedCart?.CartCD);
						return false;
					}

					POReceiptLineSplit notPutSplit =
						SelectFrom<POReceiptLineSplit>.
						Where<
							POReceiptLineSplit.receiptNbr.IsEqual<@P.AsString>.
							And<POReceiptLineSplit.putAwayQty.IsLess<POReceiptLineSplit.qty>>>.
						View.Select(Basis, receipt.ReceiptNbr);

					if (notPutSplit == null)
					{
						INRegister notReleasedTransfer =
							SelectFrom<INRegister>.
							Where<
								INRegister.docType.IsEqual<INDocType.transfer>.
								And<INRegister.transferType.IsEqual<INTransferType.oneStep>>.
								And<INRegister.released.IsEqual<False>>.
								And<INRegister.pOReceiptType.IsEqual<POReceipt.receiptType.FromCurrent>>.
								And<INRegister.pOReceiptNbr.IsEqual<POReceipt.receiptNbr.FromCurrent>>>.
							View.ReadOnly.SelectSingleBound(Basis, new[] { receipt });

						Basis.Graph.Document.Current = receipt;
						decimal cartQty = CartBasis.IsCartRequired()
							? CartBasis.AllCartSplitLinks.SelectMain().Sum(_ => _.Qty ?? 0)
							: 0;
						Basis.Graph.Document.Current = null;

						if (notReleasedTransfer == null && cartQty == 0)
						{
							error = Validation.Fail(ReceivePutAway.PutAwayMode.ReceiptState.Msg.AlreadyPutAwayInFull, receipt.ReceiptNbr);
							return false;
						}
					}

					error = Validation.Ok;
					return true;
				}
				public delegate bool CanBePutAwayDelegate(POReceipt receipt, out Validation error);
			}

			/// <summary>
			/// Overrides <see cref="ReceivePutAway.PutAwayMode.ConfirmState.Logic"/>
			/// </summary>
			public class AlterConfirmStateLogic : PXGraphExtension<ReceivePutAway.PutAwayMode.ConfirmState.Logic, ReceivePutAway, ReceivePutAway.Host>
			{
				public static bool IsActive() => RPACartSupport.IsActive();

				public ReceivePutAway Basis => Base1;
				public ReceivePutAway.PutAwayMode.ConfirmState.Logic State => Base2;
				public ReceivePutAway.PutAwayMode.Logic Mode => Basis.Get<ReceivePutAway.PutAwayMode.Logic>();
				public RPACartSupport CartBasis => Basis.Get<RPACartSupport>();

				//protected override bool ExecuteInTransaction => CartExt.IsCartRequired() == false ? base.ExecuteInTransaction : CartExt.CartLoaded == true;

				/// <summary>
				/// Overrides <see cref="ReceivePutAway.PutAwayMode.ConfirmState.Logic.ProcessPutAway"/>
				/// </summary>
				[PXOverride]
				public virtual FlowStatus ProcessPutAway(Func<FlowStatus> base_ProcessPutAway)
				{
					if (CartBasis.IsCartRequired() == false)
						return base_ProcessPutAway();
					else if (CartBasis.CartLoaded == false)
						return ProcessPutAwayInCart();
					else
						return ProcessPutAwayOutCart();
				}

				protected virtual FlowStatus ProcessPutAwayInCart()
				{
					bool remove = Basis.Remove == true;

					if (Mode.IsSingleLocation)
						Basis.LocationID = Mode.PutAway.SelectMain().FirstOrDefault()?.LocationID;

					var receivedSplits =
						Mode.PutAway.SelectMain().Where(
							r => r.LocationID == (Basis.LocationID ?? r.LocationID)
								&& r.InventoryID == Basis.InventoryID
								&& r.SubItemID == Basis.SubItemID
								&& r.LotSerialNbr == (Basis.LotSerialNbr ?? r.LotSerialNbr));
					if (receivedSplits.Any() == false)
						return FlowStatus.Fail(ReceivePutAway.PutAwayMode.ConfirmState.Msg.NothingToPutAway).WithModeReset;

					if (!Basis.EnsureLocationPrimaryItem(Basis.InventoryID, Basis.LocationID))
						return FlowStatus.Fail(IN.Messages.NotPrimaryLocation);

					decimal qty = Sign.MinusIf(remove) * Basis.BaseQty;

					if (qty != 0)
					{
						if (!remove && receivedSplits.Sum(s => s.Qty - s.PutAwayQty) < qty)
							return FlowStatus.Fail(ReceivePutAway.PutAwayMode.ConfirmState.Msg.Overputting);
						if (remove && receivedSplits.Sum(s => CartBasis.GetCartQty(s)) + qty < 0)
							return FlowStatus.Fail(RPACartSupport.Msg.CartUnderpicking);

						try
						{
							decimal unassignedQty = qty;
							foreach (var receivedSplit in remove ? receivedSplits.Reverse() : receivedSplits)
							{
								decimal currentQty = remove
									? -Math.Min(CartBasis.GetCartQty(receivedSplit), -unassignedQty)
									: +Math.Min(receivedSplit.Qty.Value - receivedSplit.PutAwayQty.Value, unassignedQty);

								if (currentQty == 0)
									continue;

								FlowStatus cartStatus = SyncWithCart(receivedSplit, currentQty);
								if (cartStatus.IsError != false)
									return cartStatus;

								receivedSplit.PutAwayQty += currentQty;
								Mode.PutAway.Update(receivedSplit);

								unassignedQty -= currentQty;
								if (unassignedQty == 0)
									break;
							}
						}
						finally
						{
							CartBasis.EnsureCartReceiptLink();
						}
					}

					Basis.DispatchNext(
						remove
							? RPACartSupport.Msg.InventoryRemoved
							: RPACartSupport.Msg.InventoryAdded,
						Basis.SelectedInventoryItem.InventoryCD, Basis.Qty, Basis.UOM);

					return FlowStatus.Ok;
				}

				protected virtual FlowStatus ProcessPutAwayOutCart()
				{
					bool remove = Basis.Remove == true;

					if (Basis.HasActive<ReceivePutAway.PutAwayMode.TargetLocationState>() && Basis.ToLocationID == null)
						return FlowStatus.Fail(ReceivePutAway.PutAwayMode.TargetLocationState.Msg.NotSet).WithModeReset;

					var receivedSplits =
						Mode.PutAway.SelectMain().Where(r =>
							r.InventoryID == Basis.InventoryID &&
							r.SubItemID == Basis.SubItemID &&
							r.LotSerialNbr == (Basis.LotSerialNbr ?? r.LotSerialNbr) &&
							(remove ? r.PutAwayQty > 0 : CartBasis.GetCartQty(r) > 0));
					if (receivedSplits.Any() == false)
						return FlowStatus.Fail(ReceivePutAway.PutAwayMode.ConfirmState.Msg.NothingToPutAway).WithModeReset;

					decimal qty = Sign.MinusIf(remove) * Basis.BaseQty;

					if (qty != 0)
					{
						if (remove && receivedSplits.Sum(s => s.PutAwayQty) + qty < 0)
							return FlowStatus.Fail(ReceivePutAway.PutAwayMode.ConfirmState.Msg.Underputting);
						if (!remove && receivedSplits.Sum(s => CartBasis.GetCartQty(s)) - qty < 0)
							return FlowStatus.Fail(RPACartSupport.Msg.CartUnderpicking);

						try
						{
							decimal unassignedQty = qty;
							foreach (var receivedSplit in remove ? receivedSplits.Reverse() : receivedSplits)
							{
								decimal currentQty = remove
									? -Math.Min(receivedSplit.PutAwayQty.Value, -unassignedQty)
									: +Math.Min(CartBasis.GetCartQty(receivedSplit), unassignedQty);

								if (currentQty == 0)
									continue;

								FlowStatus cartStatus = SyncWithCart(receivedSplit, -currentQty);
								if (cartStatus.IsError != false)
									return cartStatus;

								FlowStatus transferStatus = State.SyncWithTransfer(receivedSplit, currentQty);
								if (transferStatus.IsError != false)
									return transferStatus;

								unassignedQty -= currentQty;
								if (unassignedQty == 0)
									break;
							}
						}
						finally
						{
							CartBasis.EnsureCartReceiptLink();
						}
					}

					if (CartBasis.CartSplits.SelectMain().Any() == false)
					{
						CartBasis.CartLoaded = false;
						Basis.DispatchNext(Msg.CartIsEmpty, Basis.SightOf<CartScanHeader.cartID>());
					}
					else
					{
						Basis.DispatchNext(
							remove
								? ReceivePutAway.PutAwayMode.ConfirmState.Msg.InventoryRemoved
								: ReceivePutAway.PutAwayMode.ConfirmState.Msg.InventoryAdded,
							Basis.SelectedInventoryItem.InventoryCD, Basis.Qty, Basis.UOM);
					}

					return FlowStatus.Ok;
				}

				protected virtual FlowStatus SyncWithCart(POReceiptLineSplit receivedSplit, decimal qty)
				{
					int? cartID = CartBasis.CartID;

					INCartSplit[] linkedSplits =
						SelectFrom<POReceiptSplitToCartSplitLink>.
						InnerJoin<INCartSplit>.On<POReceiptSplitToCartSplitLink.FK.CartSplit>.
						Where<
							POReceiptSplitToCartSplitLink.FK.ReceiptLineSplit.SameAsCurrent.
							And<POReceiptSplitToCartSplitLink.siteID.IsEqual<P.AsInt>>.
							And<POReceiptSplitToCartSplitLink.cartID.IsEqual<P.AsInt>>>.
						View.SelectMultiBound(Basis, new object[] { receivedSplit }, Basis.SiteID, cartID)
						.RowCast<INCartSplit>()
						.ToArray();

					INCartSplit[] appropriateSplits =
						SelectFrom<INCartSplit>.
						Where<
							INCartSplit.cartID.IsEqual<P.AsInt>.
							And<INCartSplit.inventoryID.IsEqual<POReceiptLineSplit.inventoryID.FromCurrent>>.
							And<INCartSplit.subItemID.IsEqual<POReceiptLineSplit.subItemID.FromCurrent>>.
							And<INCartSplit.siteID.IsEqual<POReceiptLineSplit.siteID.FromCurrent>>.
							And<INCartSplit.fromLocationID.IsEqual<POReceiptLineSplit.locationID.FromCurrent>>.
							And<INCartSplit.lotSerialNbr.IsEqual<POReceiptLineSplit.lotSerialNbr.FromCurrent>>>.
						View.SelectMultiBound(Basis, new object[] { receivedSplit }, cartID)
						.RowCast<INCartSplit>()
						.ToArray();

					INCartSplit[] existingINSplits = linkedSplits.Concat(appropriateSplits).ToArray();

					INCartSplit cartSplit = existingINSplits.FirstOrDefault(s => s.LotSerialNbr == (Basis.LotSerialNbr ?? s.LotSerialNbr));
					if (cartSplit == null)
					{
						cartSplit = CartBasis.CartSplits.Insert(new INCartSplit
						{
							CartID = cartID,
							InventoryID = receivedSplit.InventoryID,
							SubItemID = receivedSplit.SubItemID,
							LotSerialNbr = receivedSplit.LotSerialNbr,
							ExpireDate = receivedSplit.ExpireDate,
							UOM = receivedSplit.UOM,
							SiteID = receivedSplit.SiteID,
							FromLocationID = receivedSplit.LocationID,
							Qty = qty
						});
					}
					else
					{
						cartSplit.Qty += qty;
						cartSplit = CartBasis.CartSplits.Update(cartSplit);
					}

					if (cartSplit.Qty == 0)
					{
						CartBasis.CartSplits.Delete(cartSplit);
						return FlowStatus.Ok;
					}
					else
						return EnsureReceiptCartSplitLink(receivedSplit, cartSplit, qty);
				}

				protected virtual FlowStatus EnsureReceiptCartSplitLink(POReceiptLineSplit poSplit, INCartSplit cartSplit, decimal deltaQty)
				{
					PXSelectBase<POReceiptSplitToCartSplitLink> cartSplitLinks = CartBasis.CartSplitLinks;

					var allLinks =
						SelectFrom<POReceiptSplitToCartSplitLink>.
						Where<
							POReceiptSplitToCartSplitLink.FK.CartSplit.SameAsCurrent.
							Or<POReceiptSplitToCartSplitLink.FK.ReceiptLineSplit.SameAsCurrent>>.
						View.SelectMultiBound(Basis, new object[] { cartSplit, poSplit })
						.RowCast<POReceiptSplitToCartSplitLink>()
						.ToArray();

					POReceiptSplitToCartSplitLink currentLink = allLinks.FirstOrDefault(link =>
						POReceiptSplitToCartSplitLink.FK.CartSplit.Match(Basis, cartSplit, link) &&
						POReceiptSplitToCartSplitLink.FK.ReceiptLineSplit.Match(Basis, poSplit, link));

					decimal cartQty = allLinks.Where(link => POReceiptSplitToCartSplitLink.FK.CartSplit.Match(Basis, cartSplit, link)).Sum(link => link.Qty ?? 0);

					if (cartQty + deltaQty > cartSplit.Qty)
					{
						return FlowStatus.Fail(RPACartSupport.Msg.LinkCartOverpicking);
					}
					if (currentLink == null ? deltaQty < 0 : currentLink.Qty + deltaQty < 0)
					{
						return FlowStatus.Fail(ReceivePutAway.PutAwayMode.ConfirmState.Msg.LinkUnderpicking);
					}

					if (currentLink == null)
					{
						currentLink = cartSplitLinks.Insert(new POReceiptSplitToCartSplitLink
						{
							ReceiptNbr = poSplit.ReceiptNbr,
							ReceiptLineNbr = poSplit.LineNbr,
							ReceiptSplitLineNbr = poSplit.SplitLineNbr,
							SiteID = cartSplit.SiteID,
							CartID = cartSplit.CartID,
							CartSplitLineNbr = cartSplit.SplitLineNbr,
							Qty = deltaQty
						});
					}
					else
					{
						currentLink.Qty += deltaQty;
						currentLink = cartSplitLinks.Update(currentLink);
					}

					if (currentLink.Qty == 0)
						cartSplitLinks.Delete(currentLink);

					return FlowStatus.Ok;
				}
			}
			#endregion
		}
		#endregion

		public void EnsureCartReceiptLink()
		{
			if (CartID != null && Basis.SiteID != null && Basis.RefNbr != null)
			{
				var link = new POCartReceipt
				{
					SiteID = Basis.SiteID,
					CartID = CartID,
					ReceiptNbr = Basis.RefNbr,
					TransferNbr = Basis.TransferRefNbr,
				};

				if (CartSplits.SelectMain().Any() || Basis.Get<ReceivePutAway.PutAwayMode.Logic>().GetTransfer()?.Released != true)
					CartsLinks.Update(link); // also insert
				else
					CartsLinks.Delete(link);
			}
		}

		#region Cart Quantities
		public decimal GetCartQty(POReceiptLineSplit posplit)
		{
			if (IsCartRequired())
				return CartSplitLinks
					.SelectMain()
					.Where(link => POReceiptSplitToCartSplitLink.FK.ReceiptLineSplit.Match(Base, posplit, link))
					.Sum(_ => _.Qty ?? 0);
			else
				return 0;
		}

		public decimal GetOverallCartQty(POReceiptLineSplit posplit)
		{
			if (IsCartRequired())
				return AllCartSplitLinks
					.SelectMain()
					.Where(link => POReceiptSplitToCartSplitLink.FK.ReceiptLineSplit.Match(Base, posplit, link))
					.Sum(_ => _.Qty ?? 0);
			else
				return 0;
		}
		#endregion

		#region Attached Fields
		[PXUIField(DisplayName = Msg.CartQty)]
		public class CartQty : ReceivePutAway.FieldAttached.To<POReceiptLineSplit>.AsDecimal.Named<CartQty>
		{
			public override decimal? GetValue(POReceiptLineSplit row) => Base.WMS?.Get<RPACartSupport>()?.GetCartQty(row);
			protected override bool? Visible => RPACartSupport.IsActive() && Base.WMS?.Get<RPACartSupport>()?.IsCartRequired() == true;
		}

		[PXUIField(DisplayName = Msg.CartOverallQty)]
		public class OverallCartQty : ReceivePutAway.FieldAttached.To<POReceiptLineSplit>.AsDecimal.Named<OverallCartQty>
		{
			public override decimal? GetValue(POReceiptLineSplit row) => Base.WMS?.Get<RPACartSupport>()?.GetOverallCartQty(row);
			protected override bool? Visible => RPACartSupport.IsActive() && Base.WMS?.Get<RPACartSupport>()?.IsCartRequired() == true;
		}
		#endregion
	}
}