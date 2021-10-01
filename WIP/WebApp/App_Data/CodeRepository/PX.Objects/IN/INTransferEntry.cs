using System;
using System.Collections;
using System.Collections.Generic;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.GL.Descriptor;
using PX.Objects.Common;
using PX.Objects.Common.Bql;

namespace PX.Objects.IN
{
	public class INTransferEntry : INRegisterEntryBase
	{
		internal bool SuppressLocationDefaultingForWMS = false;

		#region Views
		public
			PXSelect<INRegister,
			Where<INRegister.docType, Equal<INDocType.transfer>>>
			transfer;

		public
			PXSelect<INRegister,
			Where<
				INRegister.docType, Equal<INDocType.transfer>,
				And<INRegister.refNbr, Equal<Current<INRegister.refNbr>>>>>
			CurrentDocument;

		[PXImport(typeof(INRegister))]
		[PXCopyPasteHiddenFields(typeof(INTran.iNTransitQty), typeof(INTran.receiptedQty), typeof(INTran.iNTransitBaseQty), typeof(INTran.receiptedBaseQty))]
		public
			PXSelect<INTran,
			Where<
				INTran.docType, Equal<INDocType.transfer>,
				And<INTran.refNbr, Equal<Current<INRegister.refNbr>>,
				And<INTran.invtMult, Equal<shortMinus1>>>>>
			transactions;

		[PXCopyPasteHiddenView]
		public
			PXSelect<INTranSplit,
			Where<
				INTranSplit.docType, Equal<INDocType.transfer>,
				And<INTranSplit.refNbr, Equal<Current<INTran.refNbr>>,
				And<INTranSplit.lineNbr, Equal<Current<INTran.lineNbr>>>>>>
			splits;

		public
			PXSelect<INItemSite,
			Where<
				INItemSite.siteID, Equal<Required<INItemSite.siteID>>,
				And<INItemSite.inventoryID, Equal<Required<INItemSite.inventoryID>>>>>
			itemsite;

		public LSINTran lsselect;
		#endregion

		#region DAC overrides
		#region INRegister
		[Branch(typeof(Search<INSite.branchID, Where<INSite.siteID, Equal<Current<INRegister.siteID>>>>), IsDetail = false, Enabled = false)]
		protected virtual void _(Events.CacheAttached<INRegister.branchID> e) { }

		[Site(DisplayName = "Warehouse ID", DescriptionField = typeof(INSite.descr))]
		[PXDefault]
		[PXRestrictor(typeof(Where<INSite.active, Equal<True>>), IN.Messages.InactiveWarehouse, typeof(INSite.siteCD))]
		[InterBranchRestrictor(typeof(Where<SameOrganizationBranch<INSite.branchID, Current<AccessInfo.branchID>>>))]
		protected virtual void _(Events.CacheAttached<INRegister.siteID> e) { }

		[ToSite(typeof(INRegister.transferType), DisplayName = "To Warehouse ID", DescriptionField = typeof(INSite.descr))]
		[PXDefault]
		protected virtual void _(Events.CacheAttached<INRegister.toSiteID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[INOpenPeriodTransfer(IsHeader = true)]
		protected virtual void _(Events.CacheAttached<INRegister.finPeriodID> e) { }

		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.Visible), true)]
		protected virtual void _(Events.CacheAttached<INRegister.pOReceiptNbr> e) { }
		#endregion

		#region INTran
		[PXDBString(3, IsFixed = true)]
		[PXDefault(INTranType.Transfer)]
		[PXUIField(Enabled = false, Visible = false)]
		protected virtual void _(Events.CacheAttached<INTran.tranType> e) { }

		[Branch(typeof(INRegister.branchID), Visible = false, Enabled = false, Visibility = PXUIVisibility.Invisible)]
		protected virtual void _(Events.CacheAttached<INTran.branchID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Line Number", Enabled = false, Visible = false)]
		protected virtual void _(Events.CacheAttached<INTran.lineNbr> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXFormula(typeof(Selector<INTran.toSiteID, INSite.branchID>))]
		protected virtual void _(Events.CacheAttached<INTran.destBranchID> e) { }

		[ToSite]
		[PXDefault(typeof(INRegister.toSiteID))]
		protected virtual void _(Events.CacheAttached<INTran.toSiteID> e) { }

		[LocationAvail(typeof(INTran.inventoryID), typeof(INTran.subItemID), typeof(INTran.toSiteID), false, false, true, DisplayName = "To Location ID", Visibility = PXUIVisibility.Service, Visible = false)]
		[PXDefault]
		protected virtual void _(Events.CacheAttached<INTran.toLocationID> e) { }

		[PXDefault(typeof(SelectFrom<InventoryItem>.Where<InventoryItem.inventoryID.IsEqual<INTran.inventoryID.FromCurrent>>), SourceField = typeof(InventoryItem.baseUnit), CacheGlobal = true)]
		[INUnit(typeof(INTran.inventoryID))]
		protected virtual void _(Events.CacheAttached<INTran.uOM> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXDBScalar(typeof(Search<INTransitLineStatus.qtyOnHand,
			Where<INTransitLineStatus.transferLineNbr, Equal<INTran.lineNbr>,
				And<INTransitLineStatus.transferNbr, Equal<INTran.refNbr>>>>))]
		protected virtual void _(Events.CacheAttached<INTran.iNTransitBaseQty> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXFormula(typeof(Sub<INTran.baseQty, INTran.iNTransitBaseQty>))]
		protected virtual void _(Events.CacheAttached<INTran.receiptedBaseQty> e) { }
		#endregion
		#endregion

		#region Initialization
		public INTransferEntry()
		{
			INSetup record = insetup.Current;

			PXUIFieldAttribute.SetVisible<INTran.tranType>(transactions.Cache, null, false);
			PXUIFieldAttribute.SetEnabled<INTran.tranType>(transactions.Cache, null, false);

			FieldDefaulting.AddHandler<Overrides.INDocumentRelease.SiteStatus.negAvailQty>((sender, e) =>
			{
				e.NewValue = true;
				e.Cancel = true;
			});
		}
		#endregion

		#region Event Handlers
		#region INRegister
		protected virtual void _(Events.FieldDefaulting<INRegister, INRegister.docType> e) => e.NewValue = INDocType.Transfer;

		protected virtual void Set1Step(INRegister row)
		{
			if (row.SiteID == row.ToSiteID)
				row.TransferType = INTransferType.OneStep;
		}

		protected virtual void _(Events.FieldUpdated<INRegister, INRegister.siteID> e)
		{
			Set1Step(e.Row);
			e.Cache.SetDefaultExt<INRegister.branchID>(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<INRegister, INRegister.toSiteID> e)
		{
			if (e.Row != null)
			{
				foreach (INTran item in transactions.Select())
				{
					INTran updated = (INTran)transactions.Cache.CreateCopy(item);
					updated.ToSiteID = e.Row.ToSiteID;
					transactions.Cache.Update(updated);
				}
			}
			Set1Step(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<INRegister, INRegister.transferType> e)
		{
			object toSiteID = e.Row.ToSiteID;
			try
			{
				e.Cache.RaiseFieldVerifying<INRegister.toSiteID>(e.Row, ref toSiteID);
				e.Cache.RaiseExceptionHandling<INRegister.toSiteID>(e.Row, toSiteID, null);
			}
			catch (PXSetPropertyException ex)
			{
				e.Cache.RaiseExceptionHandling<INRegister.toSiteID>(e.Row, toSiteID, new PXSetPropertyException(ex, PXErrorLevel.Error, Messages.WarehouseNotAllowed, Messages.OneStep));
			}
		}

		protected virtual void _(Events.RowPersisting<INRegister> e)
		{
			if (e.Operation.Command() == PXDBOperation.Delete) return;

			PXDefaultAttribute.SetPersistingCheck<INTran.toLocationID>(this.transactions.Cache, null, e.Row.TransferType == INTransferType.OneStep ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);

			object newValue = e.Cache.GetValue<INRegister.toSiteID>(e.Row);
			try
			{
				e.Cache.RaiseFieldVerifying<INRegister.toSiteID>(e.Row, ref newValue);
			}
			catch (PXSetPropertyException ex)
			{
				PXCache inRegisterCachce = e.Cache.Graph.Caches[typeof(INRegister)];
				if ((String)inRegisterCachce.GetValue<INRegister.transferType>(inRegisterCachce.Current) == INTransferType.OneStep)
					e.Cache.RaiseExceptionHandling<INRegister.toSiteID>(e.Row, newValue, new PXSetPropertyException(ex, PXErrorLevel.Error, Messages.WarehouseNotAllowed, Messages.OneStep));
			}

		}

		protected virtual void _(Events.RowUpdated<INRegister> e)
		{
			if (insetup.Current.RequireControlTotal == false)
			{
				FillControlValue<INRegister.controlQty, INRegister.totalQty>(e.Cache, e.Row);
			}
			else if (insetup.Current.RequireControlTotal == true && e.Row.Hold == false && e.Row.Released == false)
			{
				RaiseControlValueError<INRegister.controlQty, INRegister.totalQty>(e.Cache, e.Row);
			}
		}

		protected virtual void _(Events.RowSelected<INRegister> e)
		{
			if (e.Row == null)
				return;

			PXUIFieldAttribute.SetEnabled(e.Cache, e.Row, e.Row.Released == false && e.Row.OrigModule == BatchModule.IN);
			PXUIFieldAttribute.SetEnabled<INRegister.refNbr>(e.Cache, e.Row, true);
			PXUIFieldAttribute.SetEnabled<INRegister.totalQty>(e.Cache, e.Row, false);
			PXUIFieldAttribute.SetEnabled<INRegister.status>(e.Cache, e.Row, false);
			PXUIFieldAttribute.SetEnabled<INRegister.branchID>(e.Cache, e.Row, false);

			e.Cache.AllowInsert = true;
			e.Cache.AllowUpdate = e.Row.Released == false;
			e.Cache.AllowDelete = e.Row.Released == false && e.Row.OrigModule == BatchModule.IN;

			lsselect.AllowInsert = e.Row.Released == false && e.Row.OrigModule == BatchModule.IN && e.Row.SiteID != null;
			lsselect.AllowUpdate = e.Row.Released == false;
			lsselect.AllowDelete = e.Row.Released == false && e.Row.OrigModule == BatchModule.IN;

			PXUIFieldAttribute.SetVisible<INRegister.controlQty>(e.Cache, e.Row, (bool)insetup.Current.RequireControlTotal);
			PXUIFieldAttribute.SetEnabled<INRegister.siteID>(e.Cache, e.Row, e.Row.Released == false && (e.Row.SiteID == null || transactions.Select().Count == 0));

			PXUIFieldAttribute.SetEnabled<INRegister.transferType>(e.Cache, e.Row, e.Row.OrigModule == BatchModule.IN && e.Row.Released == false && (e.Row.SiteID == null || e.Row.SiteID != e.Row.ToSiteID));
			PXUIFieldAttribute.SetVisible<INTran.toLocationID>(transactions.Cache, null, e.Row.TransferType == INTransferType.OneStep);
			PXUIFieldAttribute.SetVisible<INTran.receiptedQty>(transactions.Cache, null, e.Row.TransferType != INTransferType.OneStep);
			PXUIFieldAttribute.SetVisible<INTran.iNTransitQty>(transactions.Cache, null, e.Row.TransferType != INTransferType.OneStep);
			PXDefaultAttribute.SetPersistingCheck<INTran.toLocationID>(transactions.Cache, null, e.Row.TransferType == INTransferType.OneStep ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
		}
		#endregion

		#region INTran
		protected virtual void _(Events.FieldDefaulting<INTran, INTran.docType> e) => e.NewValue = INDocType.Transfer;

		public virtual void _(Events.FieldSelecting<INTran, INTran.iNTransitQty> e)
		{
			if (e.Row != null)
			{
				// Acuminator disable once PX1075 RaiseExceptionHandlingInEventHandlers [Justification]
				e.ReturnValue = INUnitAttribute.ConvertFromBase<INTran.inventoryID, INTran.uOM>(e.Cache, e.Row, e.Row.INTransitBaseQty.GetValueOrDefault(), INPrecision.QUANTITY);
			}
		}

		protected virtual void _(Events.FieldSelecting<INTran, INTran.receiptedQty> e)
		{
			if (e.Row != null)
			{
				// Acuminator disable once PX1075 RaiseExceptionHandlingInEventHandlers [Justification]
				e.ReturnValue = INUnitAttribute.ConvertFromBase<INTran.inventoryID, INTran.uOM>(e.Cache, e.Row, e.Row.ReceiptedBaseQty.GetValueOrDefault(), INPrecision.QUANTITY);
			}
		}

		protected virtual void _(Events.FieldDefaulting<INTran, INTran.toLocationID> e)
		{
			if (e.Row == null) return;

			// Replace with override when AC-201589 will be fixed. See original fix in PR of AC-197741.
			if (SuppressLocationDefaultingForWMS) return;

			INItemSite itemSite = itemsite.SelectWindowed(0, 1, e.Row.ToSiteID, e.Row.InventoryID);
			if (itemSite != null)
			{
				e.NewValue = e.Row.SiteID == e.Row.ToSiteID ? itemSite.DfltShipLocationID : itemSite.DfltReceiptLocationID;
				e.Cancel = true;
			}
			else
			{
				INSite site = INSite.PK.Find(this, e.Row.ToSiteID);
				if (site != null)
				{
					e.NewValue = e.Row.SiteID == e.Row.ToSiteID ? site.ShipLocationID : site.ReceiptLocationID;
					e.Cancel = true;
				}
			}

		}

		protected virtual void _(Events.FieldDefaulting<INTran, INTran.locationID> e)
		{
			if (e.Row == null) return;

			// Replace with override when AC-201589 will be fixed. See original fix in PR of AC-197741.
			if (SuppressLocationDefaultingForWMS) return;

			INItemSite itemSite = itemsite.SelectWindowed(0, 1, e.Row.SiteID, e.Row.InventoryID);
			if (itemSite != null)
			{
				e.NewValue = itemSite.DfltReceiptLocationID;
				e.Cancel = true;
			}
			else
			{
				INSite site = INSite.PK.Find(this, e.Row.SiteID);
				if (site != null)
				{
					e.NewValue = site.ReceiptLocationID;
					e.Cancel = true;
				}
			}
		}

		protected override void _(Events.FieldUpdated<INTran, INTran.inventoryID> e)
		{
			base._(e);
			e.Cache.SetDefaultExt<INTran.toLocationID>(e.Row);
		}

		protected virtual void _(Events.FieldDefaulting<INTran, INTran.siteID> e)
		{
			if (transfer.Current != null && transfer.Current.SiteID != null)
			{
				e.NewValue = transfer.Current.SiteID;
				e.Cancel = true;
			}
		}

		protected virtual void _(Events.FieldDefaulting<INTran, INTran.toSiteID> e)
		{
			if (transfer.Current != null && transfer.Current.ToSiteID != null)
			{
				e.NewValue = transfer.Current.ToSiteID;
				e.Cancel = true;
			}
		}

		protected virtual void _(Events.FieldUpdated<INTran, INTran.toSiteID> e)
		{
			if (e.Row != null)
			{
				foreach (INTranSplit item in splits.View.SelectMultiBound(new[] { e.Row }))
				{
					INTranSplit updated = (INTranSplit)splits.Cache.CreateCopy(item);
					updated.ToSiteID = e.Row.ToSiteID;
					splits.Cache.Update(updated);
				}
			}
		}

		protected virtual void _(Events.RowInserted<INTran> e)
		{
			if (e.Row != null && e.Row.OrigModule.IsIn(BatchModule.SO, BatchModule.PO))
				OnForeignTranInsert(e.Row);
		}

		protected virtual void _(Events.RowPersisting<INTran> e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				INRegister header = CurrentDocument.Current;

				if (header != null)
				{
					PXDefaultAttribute.SetPersistingCheck<INTran.toLocationID>(e.Cache, e.Row, header.TransferType == INTransferType.OneStep ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);

					if (header.SiteID != e.Row.SiteID || header.ToSiteID != e.Row.ToSiteID) // AC-139602. May occur during excel import.
					{
						if (e.Cache.RaiseExceptionHandling<INTran.locationID>(e.Row, null, new PXSetPropertyException(Messages.TransferLineIsCorrupted, PXErrorLevel.RowError)))
							throw new PXRowPersistingException(nameof(INTran.locationID), null, Messages.TransferLineIsCorrupted);
					}
				}

				CheckSplitsForSameTask(e.Cache, e.Row);
			}
		}
		#endregion
		#endregion

		protected virtual bool CheckSplitsForSameTask(PXCache sender, INTran row)
		{
			if (row.HasMixedProjectTasks == true)
			{
				sender.RaiseExceptionHandling<INTran.locationID>(row, null, new PXSetPropertyException(Messages.MixedProjectsInSplits));
				return false;
			}

			return true;
		}

		#region INRegisterEntryBase members
		public override PXSelectBase<INRegister> INRegisterDataMember => transfer;
		public override PXSelectBase<INTran> INTranDataMember => transactions;
		public override LSINTran LSSelectDataMember => lsselect;
		public override PXSelectBase<INTranSplit> INTranSplitDataMember => splits;
		protected override string ScreenID => "IN304000";
		#endregion

		#region SiteStatusLookup
		public class SiteStatusLookup : SiteStatusLookupExt<INTransferEntry, SiteStatusLookup.INSiteStatusSelected>
		{
			protected override bool IsAddItemEnabled(INRegister doc) => Transactions.AllowInsert;
			protected override bool IsSelected(INSiteStatusSelected selected) => selected.Selected == true;
			protected override decimal GetSelectedQty(INSiteStatusSelected selected) => selected.QtySelected ?? 0;
			protected override INTran InitTran(INTran newTran, INSiteStatusSelected selected)
			{
				newTran.ToSiteID = Document.Current.ToSiteID;

				newTran.SiteID = selected.SiteID ?? newTran.SiteID;
				newTran.InventoryID = selected.InventoryID;
				newTran.SubItemID = selected.SubItemID;
				newTran.UOM = selected.BaseUnit;
				newTran = PXCache<INTran>.CreateCopy(Transactions.Update(newTran));
				if (selected.LocationID != null)
				{
					newTran.LocationID = selected.LocationID;
					newTran = PXCache<INTran>.CreateCopy(Transactions.Update(newTran));
				}
				return newTran;
			}

			#region DAC overrides
			[PXCustomizeBaseAttribute(typeof(PXDefaultAttribute), nameof(PXDefaultAttribute.PersistingCheck), PXPersistingCheck.Null)]
			protected virtual void _(Events.CacheAttached<INSiteStatusFilter.siteID> e) { }

			[PXMergeAttributes(Method = MergeMethod.Replace)]
			[Location(typeof(INSiteStatusFilter.siteID))]
			protected virtual void _(Events.CacheAttached<INSiteStatusFilter.locationID> e) { }
			#endregion

			#region DACs
			public sealed class INTransferStatusFilter : PXCacheExtension<INSiteStatusFilter>
			{
				#region ReceiptNbr
				[PXDBString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
				[PXUIField(DisplayName = "Receipt Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
				[PXSelector(typeof(
					Search2<PO.POReceipt.receiptNbr,
					InnerJoin<AP.Vendor, On<PO.POReceipt.vendorID, Equal<AP.Vendor.bAccountID>>>,
					Where<PO.POReceipt.receiptType, Equal<PO.POReceiptType.poreceipt>,
					And<Match<AP.Vendor, Current<AccessInfo.userName>>>>>), Filterable = true)]
				public String ReceiptNbr { get; set; }
				public abstract class receiptNbr : BqlString.Field<receiptNbr> { }
				#endregion
			}

			[PXHidden, PXProjection(typeof(
				Select2<InventoryItem,
				LeftJoin<INLocationStatus,
								On<INLocationStatus.FK.InventoryItem>,
				LeftJoin<INLocation,
								On<INLocationStatus.locationID, Equal<INLocation.locationID>>,
				LeftJoin<INSubItem,
								On<INLocationStatus.FK.SubItem>,
				LeftJoin<INSite,
								On<INLocationStatus.FK.Site>,
				LeftJoin<INItemXRef,
							On2<INItemXRef.FK.InventoryItem,
							And<INItemXRef.alternateType, Equal<INAlternateType.barcode>,
							And2<Where<INItemXRef.subItemID, Equal<INLocationStatus.subItemID>,
									Or<INLocationStatus.subItemID, IsNull>>,
							And<CurrentValue<INSiteStatusFilter.barCode>, IsNotNull>>>>,
				LeftJoin<INItemClass,
								On<InventoryItem.FK.ItemClass>,
				LeftJoin<INPriceClass,
								On<InventoryItem.FK.PriceClass>,
				LeftJoin<PO.POReceiptLine,
							On<PO.POReceiptLine.receiptType, Equal<PO.POReceiptType.poreceipt>,
							And<PO.POReceiptLine.receiptNbr, Equal<CurrentValue<INTransferStatusFilter.receiptNbr>>,
							And<PO.POReceiptLine.siteID, Equal<CurrentValue<INRegister.siteID>>,
							And<PO.POReceiptLine.inventoryID, Equal<InventoryItem.inventoryID>>>>>>>>>>>>>,

				Where2<CurrentMatch<InventoryItem, AccessInfo.userName>,
					And2<Where<INLocationStatus.siteID, IsNull, Or<INSite.branchID, IsNotNull, And2<CurrentMatch<INSite, AccessInfo.userName>,
						And<Where2<FeatureInstalled<FeaturesSet.interBranch>,
							Or<SameOrganizationBranch<INSite.branchID, Current<INRegister.branchID>>>>>>>>,
					And2<Where<INLocationStatus.subItemID, IsNull,
									Or<CurrentMatch<INSubItem, AccessInfo.userName>>>,
					And2<Where<CurrentValue<INSiteStatusFilter.onlyAvailable>, Equal<boolFalse>,
						Or<INLocationStatus.qtyOnHand, Greater<decimal0>>>,
					And2<Where<CurrentValue<INTransferStatusFilter.receiptNbr>, IsNull,
								 Or<PO.POReceiptLine.lineNbr, IsNotNull>>,
					And<InventoryItem.stkItem, Equal<boolTrue>,
					And<InventoryItem.isTemplate, Equal<False>,
					And<InventoryItem.itemStatus, NotIn3<InventoryItemStatus.unknown, InventoryItemStatus.inactive, InventoryItemStatus.markedForDeletion>>>>>>>>>>),
				Persistent = false)]
			public class INSiteStatusSelected : IBqlTable
			{
				#region Selected
				[PXBool]
				[PXUnboundDefault(false)]
				[PXUIField(DisplayName = "Selected")]
				public virtual bool? Selected { get; set; }
				public abstract class selected : BqlBool.Field<selected> { }
				#endregion
				#region InventoryID
				[Inventory(BqlField = typeof(InventoryItem.inventoryID), IsKey = true)]
				[PXDefault]
				public virtual Int32? InventoryID { get; set; }
				public abstract class inventoryID : BqlInt.Field<inventoryID> { }
				#endregion
				#region InventoryCD
				[PXDefault]
				[InventoryRaw(BqlField = typeof(InventoryItem.inventoryCD))]
				public virtual String InventoryCD { get; set; }
				public abstract class inventoryCD : BqlString.Field<inventoryCD> { }
				#endregion
				#region Descr
				[PXDBLocalizableString(60, IsUnicode = true, BqlField = typeof(InventoryItem.descr), IsProjection = true)]
				[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
				public virtual String Descr { get; set; }
				public abstract class descr : BqlString.Field<descr> { }
				#endregion
				#region ItemClassID
				[PXDBInt(BqlField = typeof(InventoryItem.itemClassID))]
				[PXUIField(DisplayName = "Item Class ID", Visible = false)]
				[PXDimensionSelector(INItemClass.Dimension, typeof(INItemClass.itemClassID), typeof(INItemClass.itemClassCD), ValidComboRequired = true)]
				public virtual int? ItemClassID { get; set; }
				public abstract class itemClassID : BqlInt.Field<itemClassID> { }
				#endregion
				#region ItemClassCD
				[PXDBString(30, IsUnicode = true, BqlField = typeof(INItemClass.itemClassCD))]
				public virtual string ItemClassCD { get; set; }
				public abstract class itemClassCD : BqlString.Field<itemClassCD> { }
				#endregion
				#region ItemClassDescription
				[PXDBLocalizableString(Constants.TranDescLength, IsUnicode = true, BqlField = typeof(INItemClass.descr), IsProjection = true)]
				[PXUIField(DisplayName = "Item Class Description", Visible = false)]
				public virtual String ItemClassDescription { get; set; }
				public abstract class itemClassDescription : BqlString.Field<itemClassDescription> { }
				#endregion
				#region PriceClassID
				[PXDBString(10, IsUnicode = true, BqlField = typeof(InventoryItem.priceClassID))]
				[PXUIField(DisplayName = "Price Class ID", Visible = false)]
				public virtual String PriceClassID { get; set; }
				public abstract class priceClassID : BqlString.Field<priceClassID> { }
				#endregion
				#region PriceClassDescription
				[PXDBString(Constants.TranDescLength, IsUnicode = true, BqlField = typeof(INPriceClass.description))]
				[PXUIField(DisplayName = "Price Class Description", Visible = false)]
				public virtual String PriceClassDescription { get; set; }
				public abstract class priceClassDescription : BqlString.Field<priceClassDescription> { }
				#endregion
				#region BarCode
				[PXDBString(255, BqlField = typeof(INItemXRef.alternateID), IsUnicode = true)]
				public virtual String BarCode { get; set; }
				public abstract class barCode : BqlString.Field<barCode> { }
				#endregion
				#region SiteID
				[PXUIField(DisplayName = "Warehouse")]
				[Site(BqlField = typeof(INLocationStatus.siteID), IsKey = true)]
				public virtual Int32? SiteID { get; set; }
				public abstract class siteID : BqlInt.Field<siteID> { }
				#endregion
				#region SiteCD
				[PXDBString(IsUnicode = true, BqlField = typeof(INSite.siteCD))]
				[PXDimension(SiteAttribute.DimensionName)]
				public virtual String SiteCD { get; set; }
				public abstract class siteCD : BqlString.Field<siteCD> { }
				#endregion
				#region LocationID
				[Location(typeof(siteID), BqlField = typeof(INLocationStatus.locationID), IsKey = true)]
				[PXDefault]
				public virtual Int32? LocationID { get; set; }
				public abstract class locationID : BqlInt.Field<locationID> { }
				#endregion
				#region LocationCD
				[PXDBString(BqlField = typeof(INLocation.locationCD), IsUnicode = true)]
				[PXDimension(LocationAttribute.DimensionName)]
				[PXDefault]
				public virtual String LocationCD { get; set; }
				public abstract class locationCD : BqlString.Field<locationCD> { }
				#endregion
				#region SubItemID
				[SubItem(typeof(inventoryID), BqlField = typeof(INSubItem.subItemID), IsKey = true)]
				public virtual Int32? SubItemID { get; set; }
				public abstract class subItemID : BqlInt.Field<subItemID> { }
				#endregion
				#region SubItemCD
				[PXDBString(IsUnicode = true, BqlField = typeof(INSubItem.subItemCD))]
				[PXDimension(SubItemAttribute.DimensionName)]
				public virtual String SubItemCD { get; set; }
				public abstract class subItemCD : BqlString.Field<subItemCD> { }
				#endregion
				#region BaseUnit
				[PXDefault(typeof(Search<INItemClass.baseUnit, Where<INItemClass.itemClassID, Equal<Current<InventoryItem.itemClassID>>>>))]
				[INUnit(DisplayName = "Base Unit", Visibility = PXUIVisibility.Visible, BqlField = typeof(InventoryItem.baseUnit))]
				public virtual String BaseUnit { get; set; }
				public abstract class baseUnit : BqlString.Field<baseUnit> { }
				#endregion
				#region QtySelected
				[PXQuantity]
				[PXUnboundDefault(TypeCode.Decimal, "0.0")]
				[PXUIField(DisplayName = "Qty. Selected")]
				public virtual Decimal? QtySelected
				{
					get => this._QtySelected ?? 0m;
					set
					{
						if (value != null && value != 0m)
							this.Selected = true;
						this._QtySelected = value;
					}
				}
				protected Decimal? _QtySelected;
				public abstract class qtySelected : BqlDecimal.Field<qtySelected> { }
				#endregion
				#region QtyOnHand
				[PXDBQuantity(BqlField = typeof(INLocationStatus.qtyOnHand))]
				[PXDefault(TypeCode.Decimal, "0.0")]
				[PXUIField(DisplayName = "Qty. On Hand")]
				public virtual Decimal? QtyOnHand { get; set; }
				public abstract class qtyOnHand : BqlDecimal.Field<qtyOnHand> { }
				#endregion
				#region QtyAvail
				[PXDBQuantity(BqlField = typeof(INLocationStatus.qtyAvail))]
				[PXDefault(TypeCode.Decimal, "0.0")]
				[PXUIField(DisplayName = "Qty. Available")]
				public virtual Decimal? QtyAvail { get; set; }
				public abstract class qtyAvail : BqlDecimal.Field<qtyAvail> { }
				#endregion
				#region NoteID
				[PXNote(BqlField = typeof(InventoryItem.noteID))]
				public virtual Guid? NoteID { get; set; }
				public abstract class noteID : BqlGuid.Field<noteID> { }
				#endregion
			}
			#endregion
		}
		#endregion

		#region INOpenPeriodTransfer
		public class INOpenPeriodTransferAttribute : INOpenPeriodAttribute
		{
			#region Types
			protected class InTransferCalendarOrganizationIDProvider : CalendarOrganizationIDProvider
			{
				protected SourceSpecificationItem SiteSpecification { get; set; }

				protected SourceSpecificationItem ToSiteSpecification { get; set; }

				public InTransferCalendarOrganizationIDProvider()
				{
					SiteSpecification = new SourceSpecificationItem()
					{
						BranchSourceType = typeof(INRegister.siteID),
						BranchSourceFormulaType = typeof(Selector<INRegister.siteID, INSite.branchID>),
						IsMain = true,
					}.Initialize();

					ToSiteSpecification = new SourceSpecificationItem()
					{
						BranchSourceType = typeof(INRegister.toSiteID),
						BranchSourceFormulaType = typeof(Selector<INRegister.toSiteID, INSite.branchID>),
					}.Initialize();
				}

				public override SourcesSpecificationCollection GetSourcesSpecification(PXCache cache, object row)
				{
					var sourceSpecifications = new SourcesSpecificationCollection();

					sourceSpecifications.SpecificationItems.Add(SiteSpecification);

					sourceSpecifications.MainSpecificationItem = SiteSpecification;

					var register = (INRegister)row;

					if (register == null || register.TransferType == INTransferType.OneStep)
					{
						sourceSpecifications.SpecificationItems.Add(ToSiteSpecification);
					}

					sourceSpecifications.DependsOnFields.Add(typeof(INRegister.transferType));

					return sourceSpecifications;
				}
			}
			#endregion

			public INOpenPeriodTransferAttribute() : this(typeof(AccessInfo.branchID), null, typeof(INRegister.tranPeriodID)) { }

			[Obsolete(Common.Messages.MethodIsObsoleteAndWillBeRemoved2020R1)]
			public INOpenPeriodTransferAttribute(Type branchSourceType = null, Type branchSourceFormulaType = null, Type masterFinPeriodIDType = null)
				: base(
					  sourceType: typeof(INRegister.tranDate),
					  branchSourceType: branchSourceType,
					  branchSourceFormulaType: branchSourceFormulaType,
					  masterFinPeriodIDType: masterFinPeriodIDType,
					  selectionModeWithRestrictions: SelectionModesWithRestrictions.All)
			{
				if (PXAccess.FeatureInstalled<FeaturesSet.multipleCalendarsSupport>() && PXAccess.FeatureInstalled<FeaturesSet.branch>())
				{
					PeriodKeyProvider =
					CalendarOrganizationIDProvider = new InTransferCalendarOrganizationIDProvider();
				}
			}
		}
		#endregion
	}
}