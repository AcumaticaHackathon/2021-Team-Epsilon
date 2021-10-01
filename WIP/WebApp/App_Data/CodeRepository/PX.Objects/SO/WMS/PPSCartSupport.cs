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

namespace PX.Objects.SO.WMS
{
	using CartBase = CartSupport<PickPackShip, PickPackShip.Host>;

	public class PPSCartSupport : CartBase
	{
		public static bool IsActive() => IsActiveBase();

		public override bool IsCartRequired() => Basis.Setup.Current.UseCartsForPick == true && Basis.Header.Mode == PickPackShip.PickMode.Value;

		#region Views
		public
			SelectFrom<SOCartShipment>.
			View CartsLinks;

		public
			SelectFrom<INCartSplit>.
			InnerJoin<INCart>.On<INCartSplit.FK.Cart>.
			InnerJoin<SOCartShipment>.On<SOCartShipment.FK.Cart>.
			Where<SOCartShipment.FK.Shipment.SameAsCurrent>.
			View AllCartSplits;

		public
			SelectFrom<SOShipmentSplitToCartSplitLink>.
			InnerJoin<INCartSplit>.On<SOShipmentSplitToCartSplitLink.FK.CartSplit>.
			Where<SOShipmentSplitToCartSplitLink.FK.Cart.SameAsCurrent>.
			View CartSplitLinks;

		public
			SelectFrom<SOShipmentSplitToCartSplitLink>.
			InnerJoin<INCartSplit>.On<SOShipmentSplitToCartSplitLink.FK.CartSplit>.
			InnerJoin<INCart>.On<INCartSplit.FK.Cart>.
			InnerJoin<SOCartShipment>.On<SOCartShipment.FK.Cart>.
			Where<SOCartShipment.FK.Shipment.SameAsCurrent>.
			View AllCartSplitLinks;
		#endregion

		#region Overrides
		/// <summary>
		/// Overrides <see cref="PickPackShip.PickMode.Logic"/>
		/// </summary>
		public class AlterPickModeLogic : PXGraphExtension<PickPackShip.PickMode.Logic, PickPackShip, PickPackShip.Host>
		{
			public static bool IsActive() => PPSCartSupport.IsActive();

			public PickPackShip.Host Graph => Base;
			public PickPackShip Basis => Base1;
			public PickPackShip.PickMode.Logic Mode => Base2;
			public PPSCartSupport CartBasis => Basis.Get<PPSCartSupport>();

			/// <summary>
			/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanMode(ScanMode{TSelf})"/>
			/// </summary>
			[PXOverride]
			public virtual ScanMode<PickPackShip> DecorateScanMode(ScanMode<PickPackShip> original, Func<ScanMode<PickPackShip>, ScanMode<PickPackShip>> base_DecorateScanMode)
			{
				var ppsMode = base_DecorateScanMode(original);

				if (ppsMode is PickPackShip.PickMode pick)
				{
					CartBasis.InjectCartState(pick);
					CartBasis.InjectCartCommands(pick);

					pick.Intercept.CreateTransitions.ByOverride(
						(basis, base_GetTransitions) =>
						{
							var cartSupport = basis.Get<PPSCartSupport>();
							if (cartSupport.IsCartRequired())
							{
								if (cartSupport.CartLoaded == false)
								{
									return basis.StateFlow(flow => flow
										.From<PickPackShip.PickMode.ShipmentState>()
										.NextTo<CartState>()
										.NextTo<PickPackShip.LocationState>()
										.NextTo<PickPackShip.InventoryItemState>()
										.NextTo<PickPackShip.LotSerialState>()
										.NextTo<PickPackShip.ExpireDateState>());
								}
								else
								{
									return basis.StateFlow(flow => flow
										.From<PickPackShip.PickMode.ShipmentState>()
										.NextTo<CartState>()
										.NextTo<PickPackShip.InventoryItemState>()
										.NextTo<PickPackShip.LotSerialState>());
								}
							}
							else
								return base_GetTransitions();
						});
				}

				return ppsMode;
			}

			/// <summary>
			/// Overrides <see cref="BarcodeDrivenStateMachine{TSelf, TGraph}.DecorateScanState(ScanState{TSelf})"/>
			/// </summary>
			[PXOverride]
			public virtual ScanState<PickPackShip> DecorateScanState(ScanState<PickPackShip> original, Func<ScanState<PickPackShip>, ScanState<PickPackShip>> base_DecorateScanState)
			{
				var state = base_DecorateScanState(original);

				if (state is PickPackShip.PickMode.ShipmentState shipmentState)
				{
					shipmentState.Intercept.Validate.ByAppend(
						(basis, shipment) =>
						{
							if (basis.Get<PPSCartSupport>().CartID != null && shipment.SiteID != basis.SiteID)
								return Validation.Fail(Msg.DocumentInvalidSite, shipment.ShipmentNbr);

							return Validation.Ok;
						});
				}

				return state;
			}

			/// <summary>
			/// Overrides <see cref="PickPackShip.PickMode.ConfirmState.Logic"/>
			/// </summary>
			public class AlterConfirmStateLogic : PXGraphExtension<PickPackShip.PickMode.ConfirmState.Logic, PickPackShip, PickPackShip.Host>
			{
				public static bool IsActive() => PPSCartSupport.IsActive();

				public PickPackShip Basis => Base1;
				public PickPackShip.PickMode.ConfirmState.Logic State => Base2;
				public PickPackShip.PickMode.Logic Mode => Basis.Get<PickPackShip.PickMode.Logic>();
				public PPSCartSupport CartBasis => Basis.Get<PPSCartSupport>();

				/// <summary>
				/// Overrides <see cref="PickPackShip.PickMode.ConfirmState.Logic.ConfirmPicked"/>
				/// </summary>
				[PXOverride]
				public virtual FlowStatus ConfirmPicked(Func<FlowStatus> base_ConfirmPicked)
				{
					if (CartBasis.IsCartRequired() == false)
						return base_ConfirmPicked();
					else if (CartBasis.CartLoaded == false)
						return ConfirmPickedInCart();
					else
						return ConfirmPickedOutCart();
				}

				protected virtual FlowStatus ConfirmPickedInCart()
				{
					bool remove = Basis.Remove == true;

					var pickedSplit = Mode.Picked
						.SelectMain()
						.Where(r => State.IsSelectedSplit(r))
						.OrderByDescending(split => split.IsUnassigned == false && remove ? split.PickedQty > 0 : split.Qty > split.PickedQty)
						.OrderByDescending(split => remove ? split.PickedQty > 0 : split.Qty > split.PickedQty)
						.ThenByDescending(split => split.LotSerialNbr == (Basis.LotSerialNbr ?? split.LotSerialNbr))
						.ThenByDescending(split => string.IsNullOrEmpty(split.LotSerialNbr))
						.ThenByDescending(split => (split.Qty > split.PickedQty || remove) && split.PickedQty > 0)
						.ThenByDescending(split => Sign.MinusIf(remove) * (split.Qty - split.PickedQty))
						.FirstOrDefault();
					if (pickedSplit == null)
						return FlowStatus.Fail(remove
							? PickPackShip.PickMode.ConfirmState.Msg.NothingToRemove
							: PickPackShip.PickMode.ConfirmState.Msg.NothingToPick
						).WithModeReset;

					decimal qty = Basis.BaseQty;
					decimal threshold = Base.GetQtyThreshold(pickedSplit);

					if (qty != 0)
					{
						if (!remove && pickedSplit.PickedQty + CartBasis.GetOverallCartQty(pickedSplit) + qty > pickedSplit.Qty * threshold)
							return FlowStatus.Fail(PickPackShip.PickMode.ConfirmState.Msg.Overpicking);
						if (remove && CartBasis.GetCartQty(pickedSplit) < qty)
							return FlowStatus.Fail(Msg.CartUnderpicking);

						try
						{
							FlowStatus cartStatus = SyncWithCart(pickedSplit, Sign.MinusIf(remove) * qty);
							if (cartStatus.IsError != false)
								return cartStatus;
						}
						finally
						{
							CartBasis.EnsureCartShipmentLink();
						}
					}

					Basis.EnsureShipmentUserLink();

					Basis.DispatchNext(
						remove
							? Msg.InventoryRemoved
							: Msg.InventoryAdded,
						Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM);

					return FlowStatus.Ok;
				}

				protected virtual FlowStatus ConfirmPickedOutCart()
				{
					bool remove = Basis.Remove == true;

					var pickedSplits =
						Mode.Picked.SelectMain().Where(
							r => r.InventoryID == Basis.InventoryID
								&& r.SubItemID == Basis.SubItemID
								&& r.LotSerialNbr == (Basis.LotSerialNbr ?? r.LotSerialNbr)
								&& (remove ? r.PickedQty > 0 : CartBasis.GetCartQty(r) > 0));
					if (pickedSplits.Any() == false)
						return FlowStatus.Fail(remove
							? PickPackShip.PickMode.ConfirmState.Msg.NothingToRemove
							: PickPackShip.PickMode.ConfirmState.Msg.NothingToPick
						).WithModeReset;

					decimal qty = Sign.MinusIf(remove) * Basis.BaseQty;
					if (qty != 0)
					{
						if (remove)
						{
							if (pickedSplits.Sum(_ => _.PickedQty) < -qty)
								return FlowStatus.Fail(PickPackShip.PickMode.ConfirmState.Msg.Underpicking);

							if (pickedSplits.Sum(_ => _.PickedQty - _.PackedQty) < -qty)
								return FlowStatus.Fail(PickPackShip.PickMode.ConfirmState.Msg.UnderpickingByPack);
						}
						else
						{
							if (pickedSplits.Sum(_ => _.Qty * Base.GetQtyThreshold(_) - _.PickedQty) < qty)
								return FlowStatus.Fail(PickPackShip.PickMode.ConfirmState.Msg.Overpicking);

							if (pickedSplits.Sum(_ => CartBasis.GetCartQty(_)) < qty)
								return FlowStatus.Fail(Msg.CartUnderpicking);
						}

						try
						{
							decimal unassignedQty = qty;
							foreach (var pickedSplit in remove ? pickedSplits.Reverse() : pickedSplits)
							{
								Basis.EnsureAssignedSplitEditing(pickedSplit);

								decimal currentQty = remove
									? -Math.Min(pickedSplit.PickedQty.Value, -unassignedQty)
									: Math.Min(CartBasis.GetCartQty(pickedSplit), unassignedQty);

								if (currentQty == 0)
									continue;

								FlowStatus cartStatus = SyncWithCart(pickedSplit, -currentQty);
								if (cartStatus.IsError != false)
									return cartStatus;

								pickedSplit.PickedQty += currentQty;
								Mode.Picked.Update(pickedSplit);

								unassignedQty -= currentQty;
								if (unassignedQty == 0)
									break;
							}
						}
						finally
						{
							CartBasis.EnsureCartShipmentLink();
						}
					}

					Basis.EnsureShipmentUserLink();

					if (CartBasis.CartSplits.SelectMain().Any() == false)
					{
						CartBasis.CartLoaded = false;
						Basis.DispatchNext(Msg.CartIsEmpty, Basis.SightOf<CartScanHeader.cartID>());
					}
					else
					{
						Basis.DispatchNext(
							remove
								? PickPackShip.PickMode.ConfirmState.Msg.InventoryRemoved
								: PickPackShip.PickMode.ConfirmState.Msg.InventoryAdded,
							Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM);
					}

					return FlowStatus.Ok;
				}

				protected virtual FlowStatus SyncWithCart(SOShipLineSplit pickedSplit, decimal qty)
				{
					INCartSplit[] linkedSplits =
						SelectFrom<SOShipmentSplitToCartSplitLink>.
						InnerJoin<INCartSplit>.On<SOShipmentSplitToCartSplitLink.FK.CartSplit>.
						Where<
							SOShipmentSplitToCartSplitLink.FK.ShipmentSplitLine.SameAsCurrent.
							And<SOShipmentSplitToCartSplitLink.siteID.IsEqual<@P.AsInt>>.
							And<SOShipmentSplitToCartSplitLink.cartID.IsEqual<@P.AsInt>>>.
						View.SelectMultiBound(Basis, new object[] { pickedSplit }, Basis.SiteID, CartBasis.CartID)
						.RowCast<INCartSplit>()
						.ToArray();

					INCartSplit[] appropriateSplits =
						SelectFrom<INCartSplit>.
						Where<
							INCartSplit.cartID.IsEqual<@P.AsInt>.
							And<INCartSplit.inventoryID.IsEqual<SOShipLineSplit.inventoryID.FromCurrent>>.
							And<INCartSplit.subItemID.IsEqual<SOShipLineSplit.subItemID.FromCurrent>>.
							And<INCartSplit.siteID.IsEqual<SOShipLineSplit.siteID.FromCurrent>>.
							And<INCartSplit.fromLocationID.IsEqual<SOShipLineSplit.locationID.FromCurrent>>.
							And<INCartSplit.lotSerialNbr.IsEqual<SOShipLineSplit.lotSerialNbr.FromCurrent>>>.
						View.SelectMultiBound(Basis, new object[] { pickedSplit }, CartBasis.CartID)
						.RowCast<INCartSplit>()
						.ToArray();

					INCartSplit[] existingINSplits = linkedSplits.Concat(appropriateSplits).ToArray();

					INCartSplit cartSplit = existingINSplits.FirstOrDefault(s => s.LotSerialNbr == (Basis.LotSerialNbr ?? s.LotSerialNbr));
					if (cartSplit == null)
					{
						cartSplit = CartBasis.CartSplits.Insert(new INCartSplit
						{
							CartID = CartBasis.CartID,
							InventoryID = pickedSplit.InventoryID,
							SubItemID = pickedSplit.SubItemID,
							LotSerialNbr = pickedSplit.LotSerialNbr,
							ExpireDate = pickedSplit.ExpireDate,
							UOM = pickedSplit.UOM,
							SiteID = pickedSplit.SiteID,
							FromLocationID = pickedSplit.LocationID,
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
						return EnsureShipmentCartSplitLink(pickedSplit, cartSplit, qty);
				}

				protected virtual FlowStatus EnsureShipmentCartSplitLink(SOShipLineSplit soSplit, INCartSplit cartSplit, decimal deltaQty)
				{
					var allLinks =
						SelectFrom<SOShipmentSplitToCartSplitLink>.
						Where<
							SOShipmentSplitToCartSplitLink.FK.CartSplit.SameAsCurrent.
							Or<SOShipmentSplitToCartSplitLink.FK.ShipmentSplitLine.SameAsCurrent>>.
						View.SelectMultiBound(Basis, new object[] { cartSplit, soSplit })
						.RowCast<SOShipmentSplitToCartSplitLink>()
						.ToArray();

					SOShipmentSplitToCartSplitLink currentLink = allLinks.FirstOrDefault(
						link => SOShipmentSplitToCartSplitLink.FK.CartSplit.Match(Basis, cartSplit, link)
							&& SOShipmentSplitToCartSplitLink.FK.ShipmentSplitLine.Match(Basis, soSplit, link));

					decimal cartQty = allLinks.Where(link => SOShipmentSplitToCartSplitLink.FK.CartSplit.Match(Basis, cartSplit, link)).Sum(_ => _.Qty ?? 0);

					if (cartQty + deltaQty > cartSplit.Qty)
					{
						return FlowStatus.Fail(Msg.LinkCartOverpicking);
					}
					if (currentLink == null ? deltaQty < 0 : currentLink.Qty + deltaQty < 0)
					{
						return FlowStatus.Fail(Msg.LinkUnderpicking);
					}

					if (currentLink == null)
					{
						currentLink = CartBasis.CartSplitLinks.Insert(new SOShipmentSplitToCartSplitLink
						{
							ShipmentNbr = soSplit.ShipmentNbr,
							ShipmentLineNbr = soSplit.LineNbr,
							ShipmentSplitLineNbr = soSplit.SplitLineNbr,
							SiteID = cartSplit.SiteID,
							CartID = cartSplit.CartID,
							CartSplitLineNbr = cartSplit.SplitLineNbr,
							Qty = deltaQty
						});
					}
					else
					{
						currentLink.Qty += deltaQty;
						currentLink = CartBasis.CartSplitLinks.Update(currentLink);
					}

					if (currentLink.Qty == 0)
						CartBasis.CartSplitLinks.Delete(currentLink);

					return FlowStatus.Ok;
				}
			}
		}
		#endregion

		public void EnsureCartShipmentLink()
		{
			if (CartID != null && Basis.SiteID != null && Basis.RefNbr != null)
			{
				var link = new SOCartShipment
				{
					SiteID = Basis.SiteID,
					CartID = CartID,
					ShipmentNbr = Basis.RefNbr,
				};

				if (CartSplits.SelectMain().Any())
					CartsLinks.Update(link); // also insert
				else
					CartsLinks.Delete(link);
			}
		}

		#region Cart Quantities
		protected decimal GetCartQty(SOShipLineSplit sosplit)
		{
			if (IsCartRequired())
				return CartSplitLinks
					.SelectMain()
					.Where(link => SOShipmentSplitToCartSplitLink.FK.ShipmentSplitLine.Match(Basis, sosplit, link))
					.Sum(_ => _.Qty ?? 0);
			else
				return 0;
		}

		protected decimal GetOverallCartQty(SOShipLineSplit sosplit)
		{
			if (IsCartRequired())
				return AllCartSplitLinks
					.SelectMain()
					.Where(link => SOShipmentSplitToCartSplitLink.FK.ShipmentSplitLine.Match(Basis, sosplit, link))
					.Sum(_ => _.Qty ?? 0);
			else
				return 0;
		}
		#endregion

		#region Attached Fields
		[PXUIField(DisplayName = Msg.CartQty)]
		public class CartQty : PickPackShip.FieldAttached.To<SOShipLineSplit>.AsDecimal.Named<CartQty>
		{
			public override decimal? GetValue(SOShipLineSplit row) => Base.WMS?.Get<PPSCartSupport>()?.GetCartQty(row);
			protected override bool? Visible => PPSCartSupport.IsActive() && Base.WMS?.Get<PPSCartSupport>()?.IsCartRequired() == true;
		}

		[PXUIField(DisplayName = Msg.CartOverallQty)]
		public class OverallCartQty : PickPackShip.FieldAttached.To<SOShipLineSplit>.AsDecimal.Named<OverallCartQty>
		{
			public override decimal? GetValue(SOShipLineSplit row) => Base.WMS?.Get<PPSCartSupport>()?.GetOverallCartQty(row);
			protected override bool? Visible => PPSCartSupport.IsActive() && Base.WMS?.Get<PPSCartSupport>()?.IsCartRequired() == true;
		}
		#endregion
	}
}