using System;
using System.Linq;
using System.Collections.Generic;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.BarcodeProcessing;

namespace PX.Objects.IN.WMS
{
	public abstract class CartSupport<TScanBasis, TScanGraph> : PXGraphExtension<TScanBasis, TScanGraph>
		where TScanBasis : WarehouseManagementSystem<TScanBasis, TScanGraph>
		where TScanGraph : PXGraph, new()
	{
		public TScanBasis Basis => Base1;

		protected static bool IsActiveBase() => PXAccess.FeatureInstalled<CS.FeaturesSet.wMSCartTracking>();

		public static CartSupport<TScanBasis, TScanGraph> GetSelf(TScanBasis basis) => basis.Get<CartSupport<TScanBasis, TScanGraph>>();

		public abstract bool IsCartRequired();

		#region State
		public CartScanHeader CartHeader => Basis.Header.Get<CartScanHeader>() ?? new CartScanHeader();
		public ValueSetter<ScanHeader>.Ext<CartScanHeader> CartSetter => Basis.HeaderSetter.With<CartScanHeader>();

		#region CartID
		public int? CartID
		{
			get => CartHeader.CartID;
			set
			{
				if (CartID != value)
				{
					CartSetter.Set(h => h.CartID, value);
					Cart.Current = Cart.Select();
				}
			}
		}
		#endregion
		#region CartLoaded
		public bool? CartLoaded
		{
			get => CartHeader.CartLoaded;
			set => CartSetter.Set(h => h.CartLoaded, value);
		}
		#endregion
		#endregion

		#region Selected entities
		public INCart SelectedCart => INCart.PK.Find(Basis, Basis.SiteID, CartID);
		#endregion

		#region Views
		public
			SelectFrom<INCart>.
			Where<
				INCart.siteID.IsEqual<WMSScanHeader.siteID.FromCurrent>.
				And<INCart.cartID.IsEqual<CartScanHeader.cartID.FromCurrent>>>.
			View Cart;

		public
			SelectFrom<INCartSplit>.
			Where<INCartSplit.FK.Cart.SameAsCurrent>.
			View CartSplits;
		#endregion

		#region Event Handlers
		protected virtual void _(Events.RowSelected<ScanHeader> e)
		{
			if (e.Row == null)
				return;

			PXUIFieldAttribute.SetVisible<CartScanHeader.cartID>(e.Cache, e.Row, IsCartRequired());

			if (IsCartRequired())
				Cart.Current = Cart.Select();
		}
		#endregion

		#region Commands
		public class CartOut : ScanCommand<TScanBasis>
		{
			public const string Value = "CART*OUT";
			public class value : BqlString.Constant<value> { public value() : base(CartOut.Value) { } }

			public CartSupport<TScanBasis, TScanGraph> CartBasis => GetSelf(Basis);

			public override string Code => Value;
			public override string ButtonName => "scanCartOut";
			public override string DisplayName => Msg.DisplayName;
			protected override bool IsEnabled => Basis.RefNbr != null && CartBasis.CartID != null && CartBasis.CartLoaded == false;

			protected override bool Process()
			{
				Basis.CurrentMode.Reset(fullReset: false);
				CartBasis.CartLoaded = true;
				Basis.SetScanState(Basis.CurrentMode.DefaultState.Code);
				Basis.Reporter.Info(Msg.Done);
				return true;
			}

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string DisplayName = "Cart Out";
				public const string Done = "The cart unloading mode has been activated.";
			}
			#endregion
		}

		public class CartIn : ScanCommand<TScanBasis>
		{
			public const string Value = "CART*IN";
			public class value : BqlString.Constant<value> { public value() : base(CartIn.Value) { } }

			public CartSupport<TScanBasis, TScanGraph> CartBasis => GetSelf(Basis);

			public override string Code => Value;
			public override string ButtonName => "scanCartIn";
			public override string DisplayName => Msg.DisplayName;
			protected override bool IsEnabled => Basis.RefNbr != null && CartBasis.CartID != null && CartBasis.CartLoaded == true;

			protected override bool Process()
			{
				Basis.CurrentMode.Reset(fullReset: false);
				CartBasis.CartLoaded = false;
				Basis.SetScanState(Basis.CurrentMode.DefaultState.Code);
				Basis.Reporter.Info(Msg.Done);
				return true;
			}

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string DisplayName = "Cart In";
				public const string Done = "The cart loading mode has been activated.";
			}
			#endregion
		}
		#endregion

		#region States
		public class CartState : EntityState<TScanBasis, INCart>
		{
			public const string Value = "CART";
			public class value : BqlString.Constant<value> { public value() : base(CartState.Value) { } }

			public CartSupport<TScanBasis, TScanGraph> CartBasis => GetSelf(Basis);

			public override string Code => Value;
			protected override string StatePrompt => Msg.Prompt;
			protected override bool IsStateActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.wMSCartTracking>();
			protected override bool IsStateSkippable() => CartBasis.CartID != null;

			protected override INCart GetByBarcode(string barcode)
			{
				return
					SelectFrom<INCart>.
					InnerJoin<INSite>.On<INCart.FK.Site>.
					Where<
						INCart.cartCD.IsEqual<@P.AsString>.
						And<Match<INSite, AccessInfo.userName.FromCurrent>>>.
					View.ReadOnly.Select(Basis, barcode);
			}

			protected override Validation Validate(INCart cart)
			{
				if (Basis.RefNbr != null && cart.SiteID != Basis.SiteID)
					return Validation.Fail(Msg.InvalidSite, cart.CartCD);

				return Validation.Ok;
			}

			protected override void Apply(INCart entity)
			{
				CartBasis.CartID = entity.CartID;
				if (Basis.SiteID == null)
					Basis.SiteID = entity.SiteID;
			}
			protected override void ClearState()
			{
				CartBasis.CartLoaded = false;
				CartBasis.CartID = null;
				CartBasis.Cart.Current = null;
			}

			protected override void ReportMissing(string barcode) => Basis.Reporter.Error(Msg.Missing, barcode);
			protected override void ReportSuccess(INCart entity) => Basis.Reporter.Info(Msg.Ready, entity.CartCD);

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string Prompt = "Scan the barcode of the cart.";
				public const string Ready = "The cart {0} is selected.";
				public const string Missing = "The {0} cart is not found.";
				public const string IsOccupied = "The {0} cart is already in use.";
				public const string InvalidSite = "The warehouse of the {0} cart differs from the warehouse of the selected document.";
			}
			#endregion
		}
		#endregion

		#region Injections
		public virtual ScanMode<TScanBasis> InjectCartState(ScanMode<TScanBasis> mode, bool makeItDefault = false)
		{
			mode
				.Intercept.CreateStates.ByOverride(
					(basis, base_CreateStates) =>
					{
						var states = base_CreateStates().ToList();
						states.Add(new CartState());
						return states;
					})
				.Intercept.ResetMode.ByAppend(
					(basis, fullReset) =>
					{
						basis.Clear<CartState>(when: fullReset && !basis.IsWithinReset);
					});

			if (makeItDefault)
			{
				mode.Intercept.GetDefaultState.ByOverride(
					(basis, base_GetDefaultState) =>
					{
						if (GetSelf(basis).IsCartRequired())
							return basis.FindState<CartState>();
						else
							return base_GetDefaultState();
					});
			}

			return mode;
		}

		public virtual ScanMode<TScanBasis> InjectCartCommands(ScanMode<TScanBasis> mode)
		{
			return mode
				.Intercept.CreateCommands.ByOverride(
					(basis, base_CreateCommands) =>
					{
						if (GetSelf(basis).IsCartRequired())
						{
							var commands = base_CreateCommands().ToList();
							commands.Add(new CartIn());
							commands.Add(new CartOut());
							return commands;
						}
						else
							return base_CreateCommands();
					});
		}
		#endregion

		#region Messages
		[PXLocalizable]
		public abstract class Msg
		{
			public const string CartIsEmpty = "The {0} cart is empty.";

			public const string LinkCartOverpicking = "Link quantity cannot be greater than the quantity of a cart line split.";
			public const string LinkUnderpicking = "Link quantity cannot be negative.";

			public const string CartOverpicking = "The overall cart quantity cannot be greater than the difference between the expected quantity and already picked quantity.";
			public const string CartUnderpicking = "The cart quantity cannot become negative.";

			public const string InventoryAdded = "{0} x {1} {2} has been added to the cart.";
			public const string InventoryRemoved = "{0} x {1} {2} has been removed from the cart.";

			public const string CartQty = "Cart Qty.";
			public const string CartOverallQty = "Overall Cart Qty.";

			public const string DocumentInvalidSite = "The warehouse specified in the {0} document differs from the warehouse assigned to the selected cart.";
		}
		#endregion
	}

	public sealed class CartScanHeader : PXCacheExtension<WMSScanHeader, QtyScanHeader, ScanHeader>
	{
		public static bool IsActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.wMSCartTracking>();

		#region CartID
		[PXInt]
		[PXUIField(
			DisplayName = "Cart ID",
			Enabled = false,
			Visible = false // is made visible by IsCartRequired() method
		)]
		[PXSelector(typeof(
			SelectFrom<INCart>.
			InnerJoin<INSite>.On<INCart.FK.Site>.
			Where<
				INCart.active.IsEqual<True>.
				And<Match<INSite, AccessInfo.userName.FromCurrent>>>.
			SearchFor<INCart.cartID>),
			SubstituteKey = typeof(INCart.cartCD),
			DescriptionField = typeof(INCart.descr))]
		public int? CartID { get; set; }
		public abstract class cartID : BqlInt.Field<cartID> { }
		#endregion
		#region CartLoaded
		[PXBool, PXUnboundDefault(false)]
		[PXUIField(DisplayName = "Cart Unloading", Enabled = false)]
		[PXUIVisible(typeof(cartLoaded))]
		public bool? CartLoaded { get; set; }
		public abstract class cartLoaded : BqlBool.Field<cartLoaded> { }
		#endregion
	}
}