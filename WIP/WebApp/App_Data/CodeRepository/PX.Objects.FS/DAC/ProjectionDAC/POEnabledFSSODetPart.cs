using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.CM.Extensions;
using PX.Objects.Common.Discount;
using PX.Objects.Common.Discount.Attributes;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.Objects.PO;
using PX.Objects.SO;
using PX.Objects.TX;
using System;
using CRLocation = PX.Objects.CR.Standalone.Location;

namespace PX.Objects.FS
{
    [Serializable]
    [PXPrimaryGraph(typeof(ServiceOrderEntry))]
    [PXProjection(typeof(
        Select2<FSSODet,
            InnerJoin<FSServiceOrder,
                On<FSServiceOrder.sOID, Equal<FSSODet.sOID>>,
            InnerJoin<InventoryItem,
                On<InventoryItem.inventoryID, Equal<FSSODet.inventoryID>>,
            LeftJoin<INItemClass,
                On<INItemClass.itemClassID, Equal<InventoryItem.itemClassID>>,
            LeftJoin<INSite, 
                On<INSite.siteID, Equal<FSSODet.siteID>>>>>>,
        Where<
            FSServiceOrder.canceled, Equal<False>,
            And<FSSODet.status, NotEqual<FSSODet.status.Canceled>,
            And<FSSODet.enablePO, Equal<True>,
            And<FSSODet.poNbr, IsNull,
            And<
                Where<
                    InventoryItem.stkItem, Equal<True>, 
                    Or<
                        Where<
                            InventoryItem.nonStockShip, Equal<True>,
                            Or<InventoryItem.nonStockReceipt, Equal<True>>>>>>>>>>>))]
    public class POEnabledFSSODet : FSSODet
    {
        #region Keys
        public new class PK : PrimaryKeyOf<POEnabledFSSODet>.By<lineNbr, refNbr, srvOrdType>
        {
            public static POEnabledFSSODet Find(PXGraph graph, int? lineNbr, string refNbr, string srvOrdType) => FindBy(graph, lineNbr, refNbr, srvOrdType);
        }

        public new static class FK
        {
            public class Branch : GL.Branch.PK.ForeignKeyOf<POEnabledFSSODet>.By<srvBranchID> { }
            public class BranchLocation : FSBranchLocation.PK.ForeignKeyOf<POEnabledFSSODet>.By<srvBranchLocationID> { }
            public class ServiceOrder : FSServiceOrder.PK.ForeignKeyOf<POEnabledFSSODet>.By<srvOrdType, refNbr> { }
            public class ServiceOrderType : FSSrvOrdType.PK.ForeignKeyOf<POEnabledFSSODet>.By<srvOrdType> { }
            public class InventoryItem : IN.InventoryItem.PK.ForeignKeyOf<POEnabledFSSODet>.By<inventoryID> { }
            public class SubItem : INSubItem.PK.ForeignKeyOf<POEnabledFSSODet>.By<subItemID> { }
            public class Site : INSite.PK.ForeignKeyOf<POEnabledFSSODet>.By<siteID> { }
            public class SiteStatus : IN.INSiteStatus.PK.ForeignKeyOf<POEnabledFSSODet>.By<inventoryID, subItemID, siteID> { }
            public class Location : INLocation.PK.ForeignKeyOf<POEnabledFSSODet>.By<locationID> { }
            public class LocationStatus : IN.INLocationStatus.PK.ForeignKeyOf<POEnabledFSSODet>.By<inventoryID, subItemID, siteID, locationID> { }
            public class LotSerialStatus : IN.INLotSerialStatus.PK.ForeignKeyOf<POEnabledFSSODet>.By<inventoryID, subItemID, siteID, locationID, lotSerialNbr> { }
            public class CurrencyInfo : CM.CurrencyInfo.PK.ForeignKeyOf<POEnabledFSSODet>.By<curyInfoID> { }
            public class TaxCategory : Objects.TX.TaxCategory.PK.ForeignKeyOf<POEnabledFSSODet>.By<taxCategoryID> { }
            public class Project : PMProject.PK.ForeignKeyOf<POEnabledFSSODet>.By<projectID> { }
            public class Task : PMTask.PK.ForeignKeyOf<POEnabledFSSODet>.By<projectTaskID> { }
            public class Account : GL.Account.PK.ForeignKeyOf<POEnabledFSSODet>.By<acctID> { }
            public class Subaccount : GL.Sub.PK.ForeignKeyOf<POEnabledFSSODet>.By<subID> { }
            public class CostCode : PMCostCode.PK.ForeignKeyOf<POEnabledFSSODet>.By<costCodeID> { }
            public class Discount : ARDiscount.PK.ForeignKeyOf<POEnabledFSSODet>.By<discountID> { }
            public class POVendor : AP.Vendor.PK.ForeignKeyOf<POEnabledFSSODet>.By<poVendorID> { }
            public class BillCustomer : AR.Customer.PK.ForeignKeyOf<POEnabledFSSODet>.By<billCustomerID> { }
            public class Equipment : FSEquipment.PK.ForeignKeyOf<POEnabledFSSODet>.By<SMequipmentID> { }
            public class Component : FSModelTemplateComponent.PK.ForeignKeyOf<POEnabledFSSODet>.By<componentID> { }
            public class EquipmentComponent : FSEquipmentComponent.PK.ForeignKeyOf<POEnabledFSSODet>.By<SMequipmentID, equipmentLineRef> { }
            public class PostInfo : FSPostInfo.PK.ForeignKeyOf<POEnabledFSSODet>.By<postID> { }
            public class PurchaseOrderType : SO.SOOrderType.PK.ForeignKeyOf<POEnabledFSSODet>.By<poType> { }
            public class PurchaseOrder : POOrder.PK.ForeignKeyOf<POEnabledFSSODet>.By<poType, poNbr> { }
            public class PurchaseSite : INSite.PK.ForeignKeyOf<POEnabledFSSODet>.By<pOSiteID> { }
            public class Schedule : FSSchedule.PK.ForeignKeyOf<POEnabledFSSODet>.By<scheduleID> { }
            public class ScheduleDetail : FSScheduleDet.UK.ForeignKeyOf<POEnabledFSSODet>.By<scheduleID, scheduleDetID> { }
            public class Staff : BAccount.PK.ForeignKeyOf<FSSOEmployee>.By<staffID> { }
            public class ItemClass : INItemClass.PK.ForeignKeyOf<POEnabledFSSODet>.By<inventoryItemClassID> { }
            public class Customer : AR.Customer.PK.ForeignKeyOf<FSServiceOrder>.By<srvCustomerID> { }
            public class CustomerLocation : CR.Location.PK.ForeignKeyOf<FSServiceOrder>.By<srvCustomerID, srvLocationID> { }
        }

        #endregion

        #region BranchID
        public abstract class srvBranchID : PX.Data.BQL.BqlInt.Field<srvBranchID> { }

        [PXDBInt(BqlField = typeof(FSServiceOrder.branchID))]
        [PXDefault(typeof(AccessInfo.branchID))]
        [PXUIField(DisplayName = "Branch")]
        [PXSelector(typeof(Search<Branch.branchID>), SubstituteKey = typeof(Branch.branchCD), DescriptionField = typeof(Branch.acctName))]
        public virtual int? SrvBranchID { get; set; }
        #endregion
        #region SrvBranchLocationID
        public abstract class srvBranchLocationID : PX.Data.BQL.BqlInt.Field<srvBranchLocationID> { }

        [PXDBInt(BqlField = typeof(FSServiceOrder.branchLocationID))]
        [PXUIField(DisplayName = "Branch Location ID")]
        [PXSelector(typeof(
            Search<FSBranchLocation.branchLocationID,
            Where<
                FSBranchLocation.branchID, Equal<Current<POEnabledFSSODet.srvBranchID>>>>),
            SubstituteKey = typeof(FSBranchLocation.branchLocationCD),
            DescriptionField = typeof(FSBranchLocation.descr))]
        public virtual int? SrvBranchLocationID { get; set; }
        #endregion
        #region InventoryItemClassID
        public abstract class inventoryItemClassID : PX.Data.BQL.BqlInt.Field<inventoryItemClassID> { }

        [PXDBInt(BqlField = typeof(InventoryItem.itemClassID))]
        [PXUIField(DisplayName = "Item Class", Visibility = PXUIVisibility.SelectorVisible)]
        [PXSelector(typeof(Search<INItemClass.itemClassID,
                    Where<
                        INItemClass.itemType, Equal<INItemTypes.serviceItem>,
                        Or<FeatureInstalled<FeaturesSet.distributionModule>>>>),
                SubstituteKey = typeof(INItemClass.itemClassCD))]
        public virtual int? InventoryItemClassID { get; set; }
        #endregion
        #region OrderDate
        public abstract class orderDate : PX.Data.BQL.BqlDateTime.Field<orderDate> { }

        [PXDBDate(BqlField = typeof(FSServiceOrder.orderDate))]
        [PXUIField(DisplayName = "Requested on", Visibility = PXUIVisibility.SelectorVisible)]
        public override DateTime? OrderDate { get; set; }
        #endregion
        #region SrvCustomerID
        public abstract class srvCustomerID : PX.Data.BQL.BqlInt.Field<srvCustomerID> { }

        [PXDBInt(BqlField = typeof(FSServiceOrder.customerID))]
        [PXUIField(DisplayName = "Customer ID", Visibility = PXUIVisibility.SelectorVisible)]
        [PXRestrictor(typeof(Where<BAccountSelectorBase.status, IsNull,
                Or<BAccountSelectorBase.status, Equal<CustomerStatus.active>,
                Or<BAccountSelectorBase.status, Equal<CustomerStatus.oneTime>>>>),
                PX.Objects.AR.Messages.CustomerIsInStatus, typeof(BAccountSelectorBase.status))]
        [FSSelectorBusinessAccount_CU_PR_VC]
        public virtual int? SrvCustomerID { get; set; }
        #endregion
        #region SrvLocationID
        public abstract class srvLocationID : PX.Data.BQL.BqlInt.Field<srvLocationID> { }

        [LocationID(
            typeof(Where<Location.bAccountID, Equal<Current<POEnabledFSSODet.srvCustomerID>>>), 
            BqlField = typeof(FSServiceOrder.locationID), 
            DescriptionField = typeof(Location.descr), 
            DisplayName = "Location ID")]
        public virtual int? SrvLocationID { get; set; }
        #endregion
        #region RefNbr
        public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }

        [PXDBString(15, IsKey = true, IsUnicode = true, InputMask = "")]
        [PXUIField(DisplayName = "Service Order Nbr.", Enabled = false)]
        [PXDBDefault(typeof(FSServiceOrder.refNbr), DefaultForUpdate = false)]
        [PXParent(typeof(Select<FSServiceOrder,
                            Where<FSServiceOrder.srvOrdType, Equal<Current<FSSODet.srvOrdType>>,
                                And<FSServiceOrder.refNbr, Equal<Current<FSSODet.refNbr>>>>>))]
        public override string RefNbr { get; set; }
        #endregion
        #region SrvOrdType
        public new abstract class srvOrdType : PX.Data.BQL.BqlString.Field<srvOrdType> { }

        [PXDBString(4, IsKey = true, IsFixed = true)]
        [PXUIField(DisplayName = "Service Order Type", Enabled = false)]
        [PXDefault(typeof(FSServiceOrder.srvOrdType))]
        [PXSelector(typeof(Search<FSSrvOrdType.srvOrdType>), CacheGlobal = true)]
        public override string SrvOrdType { get; set; }
        #endregion
        #region PONbrCreated
        public abstract class poNbrCreated : PX.Data.BQL.BqlString.Field<poNbrCreated> { }

        [PXString]
        [PXUIField(DisplayName = "PO Nbr.")]
        [PO.PO.RefNbr(typeof(
            Search2<POOrder.orderNbr,
            LeftJoinSingleTable<Vendor,
                On<POOrder.vendorID, Equal<Vendor.bAccountID>,
                And<Match<Vendor, Current<AccessInfo.userName>>>>>,
            Where<
                POOrder.orderType, Equal<POOrderType.regularOrder>,
                And<Vendor.bAccountID, IsNotNull>>,
            OrderBy<Desc<POOrder.orderNbr>>>), Filterable = true)]
        public virtual string PONbrCreated { get; set; }
        #endregion
        #region POVendorID
        public new abstract class poVendorID : PX.Data.BQL.BqlInt.Field<poVendorID> { }

        [VendorNonEmployeeActive(DisplayName = "Vendor ID", DescriptionField = typeof(Vendor.acctName), CacheGlobal = true, Filterable = true)]
        public override int? POVendorID { get; set; }
        #endregion
        #region POVendorLocationID

        [PXFormula(typeof(Default<FSSODet.poVendorID>))]
        [PXDefault(typeof(Search<Vendor.defLocationID, Where<Vendor.bAccountID, Equal<Current<FSSODet.poVendorID>>>>),
                PersistingCheck = PXPersistingCheck.Nothing)]
        [LocationID(typeof(Where<Location.bAccountID, Equal<Current<FSSODet.poVendorID>>>),
                DescriptionField = typeof(Location.descr), Visibility = PXUIVisibility.SelectorVisible, DisplayName = "Vendor Location ID", Visible = true)]
        public override int? POVendorLocationID { get; set; }
        #endregion
        #region CuryUnitCost
        public abstract class srvCuryUnitCost : PX.Data.BQL.BqlDecimal.Field<srvCuryUnitCost> { }

        [PXDBDecimal(BqlField = typeof(FSSODet.curyUnitCost))]
        [PXUIField(DisplayName = "Unit Cost", Visibility = PXUIVisibility.SelectorVisible)]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual Decimal? SrvCuryUnitCost { get; set; }
        #endregion

        public new abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
        public new abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }
        public new abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
        public new abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
        public new abstract class lotSerialNbr : PX.Data.BQL.BqlString.Field<lotSerialNbr> { }
        public new abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
        public new abstract class taxCategoryID : PX.Data.BQL.BqlString.Field<taxCategoryID> { }
        public new abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
        public new abstract class projectTaskID : PX.Data.BQL.BqlInt.Field<projectTaskID> { }
        public new abstract class acctID : PX.Data.BQL.BqlInt.Field<acctID> { }
        public new abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }
        public new abstract class costCodeID : PX.Data.BQL.BqlInt.Field<costCodeID> { }
        public new abstract class discountID : PX.Data.BQL.BqlString.Field<discountID> { }
        public new abstract class billCustomerID : PX.Data.BQL.BqlInt.Field<billCustomerID> { }
        public new abstract class SMequipmentID : PX.Data.BQL.BqlInt.Field<SMequipmentID> { }
        public new abstract class componentID : PX.Data.BQL.BqlInt.Field<componentID> { }
        public new abstract class equipmentLineRef : PX.Data.BQL.BqlInt.Field<equipmentLineRef> { }
        public new abstract class postID : PX.Data.BQL.BqlInt.Field<postID> { }
        public new abstract class poType : PX.Data.BQL.BqlString.Field<poType> { }
        public new abstract class poNbr : PX.Data.BQL.BqlString.Field<poNbr> { }
        public new abstract class pOSiteID : PX.Data.BQL.BqlInt.Field<pOSiteID> { }
        public new abstract class scheduleID : PX.Data.BQL.BqlInt.Field<scheduleID> { }
        public new abstract class scheduleDetID : PX.Data.BQL.BqlInt.Field<scheduleDetID> { }
        public new abstract class staffID : PX.Data.BQL.BqlInt.Field<staffID> { }
    }
}
