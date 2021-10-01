using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.AP;
using PX.Objects.CM.Extensions;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.TM;
using System;

namespace PX.Objects.FS
{
    [Serializable]
    [PXProjection(typeof(Select2<FSAppointmentLog,
                         LeftJoin<Vendor,
                         On<
                             Vendor.bAccountID, Equal<FSAppointmentLog.bAccountID>>,
                         LeftJoin<EPEmployee,
                         On<
                             EPEmployee.bAccountID, Equal<FSAppointmentLog.bAccountID>>,
                         LeftJoin<FSAppointmentDet,
                         On<
                             FSAppointmentDet.appointmentID, Equal<FSAppointmentLog.docID>,
                             And<FSAppointmentDet.lineRef, Equal<FSAppointmentLog.detLineRef>>>>>>>))]
    [PXBreakInheritance]
    public class FSAppointmentLogExtItemLine : FSAppointmentLog
    {
        #region Keys
        public new class PK : PrimaryKeyOf<FSAppointmentLogExtItemLine>.By<docType, docRefNbr, lineNbr>
        {
            public static FSAppointmentLogExtItemLine Find(PXGraph graph, string docType, string docRefNbr, int? lineNbr) => FindBy(graph, docType, docRefNbr, lineNbr);
        }
        public new class UK : PrimaryKeyOf<FSAppointmentLogExtItemLine>.By<logID>
        {
            public static FSAppointmentLogExtItemLine Find(PXGraph graph, int? logID) => FindBy(graph, logID);
        }

        public new static class FK
        {
            public class ServiceOrderType : FSSrvOrdType.PK.ForeignKeyOf<FSAppointmentLogExtItemLine>.By<docType> { }
            public class Appointment : FSAppointment.PK.ForeignKeyOf<FSAppointmentLogExtItemLine>.By<docType, docRefNbr> { }
            public class Staff : BAccount.PK.ForeignKeyOf<FSAppointmentLogExtItemLine>.By<bAccountID> { }
            public class EarningType : EP.EPEarningType.PK.ForeignKeyOf<FSAppointmentLogExtItemLine>.By<earningType> { }
            public class CostCode : PMCostCode.PK.ForeignKeyOf<FSAppointmentLogExtItemLine>.By<costCodeID> { }
            public class Project : PMProject.PK.ForeignKeyOf<FSAppointmentLogExtItemLine>.By<projectID> { }
            public class Task : PMTask.PK.ForeignKeyOf<FSAppointmentLogExtItemLine>.By<projectTaskID> { }
            public class LaborItem : IN.InventoryItem.PK.ForeignKeyOf<FSAppointmentLogExtItemLine>.By<laborItemID> { }
            public class CurrencyInfo : CM.CurrencyInfo.PK.ForeignKeyOf<FSAppointmentLogExtItemLine>.By<curyInfoID> { }
            public class WorkGorupID : EPCompanyTree.PK.ForeignKeyOf<FSAppointmentLogExtItemLine>.By<workgroupID> { }
            public class TimeCard : EPTimeCard.PK.ForeignKeyOf<FSAppointmentLogExtItemLine>.By<timeCardCD> { }
            public class User : PX.SM.Users.PK.ForeignKeyOf<FSAppointmentLogExtItemLine>.By<userID> { }
        }
        #endregion

        #region DocID
        public new abstract class docID : PX.Data.BQL.BqlInt.Field<docID> { }

        [PXDBInt(IsKey = true, BqlField = typeof(FSAppointmentLog.docID))]
        [PXDBDefault(typeof(FSAppointment.appointmentID))]
        [PXUIField(DisplayName = "Appointment Ref. Nbr.", Visible = false, Enabled = false)]
        public override int? DocID { get; set; }
        #endregion
        #region LineRef
        public new abstract class lineRef : PX.Data.BQL.BqlString.Field<lineRef> { }

        [PXDBString(3, IsFixed = true, IsKey = true, BqlField = typeof(FSAppointmentLog.lineRef))]
        [PXUIField(DisplayName = "Log Ref. Nbr.", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
        public override string LineRef { get; set; }
        #endregion
        #region DetLineRef
        public new abstract class detLineRef : PX.Data.BQL.BqlString.Field<detLineRef> { }

        [PXDBString(4, IsFixed = true, BqlField = typeof(FSAppointmentLog.detLineRef))]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [FSSelectorAppointmentSODetID]
        [PXUIField(DisplayName = "Detail Ref. Nbr.")]
        public override string DetLineRef { get; set; }
        #endregion
        #region Descr
        public new abstract class descr : Data.BQL.BqlString.Field<descr> { }

        [PXDBString(Common.Constants.TranDescLength, IsUnicode = true, BqlField = typeof(FSAppointmentLog.descr))]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Description", Enabled = false)]
        [PXUIVisible(typeof(Where2<
                                Where<
                                    Current<FSLogActionFilter.action>, Equal<ListField_LogActions.Complete>,
                                Or<
                                    Current<FSLogActionFilter.action>, Equal<ListField_LogActions.Resume>,
                                Or<
                                    Current<FSLogActionFilter.action>, Equal<ListField_LogActions.Pause>>>>,
                            And<
                                Current<FSLogActionFilter.type>, Equal<FSLogTypeAction.Service>>>))]
        public override string Descr { get; set; }
        #endregion
        #region BAccountID
        public new abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }

        [PXDBInt(BqlField = typeof(FSAppointmentLog.bAccountID))]
        [PXUIField(DisplayName = "Staff Member")]
        [PXUIVisible(typeof(Where<Current<FSLogActionFilter.me>, Equal<False>>))]
        [FSSelector_StaffMember_ServiceOrderProjectID]
        public override int? BAccountID { get; set; }
        #endregion
        #region Status
        public new abstract class status : ListField_Status_Log { }

        [PXDBString(1, IsFixed = true, BqlField = typeof(FSAppointmentLog.status))]
        [PXUIField(DisplayName = "Log Line Status")]
        [status.ListAtrribute]
        [PXUIVisible(typeof(Where<Current<FSLogActionFilter.action>, Equal<FSLogActionFilter.action.Complete>>))]
        public override string Status { get; set; }
        #endregion
        #region ItemType
        public new abstract class itemType : PX.Data.BQL.BqlString.Field<FSAppointmentLog.itemType>
        {
            public abstract class Values : ListField_Log_ItemType { }
        }

        [PXDBString(2, IsFixed = true, BqlField = typeof(FSAppointmentLog.itemType))]
        [FSAppointmentLog.itemType.Values.ListAttribute]
        public override string ItemType { get; set; }
        #endregion
        #region DateTimeBegin
        public new abstract class dateTimeBegin : PX.Data.BQL.BqlDateTime.Field<dateTimeBegin> { }

        [PXDBDateAndTime(UseTimeZone = true, BqlField = typeof(FSAppointmentLog.dateTimeBegin),
                         PreserveTime = true, DisplayNameDate = "Start Date", DisplayNameTime = "Start Time")]
        [PXUIField(DisplayName = "Start Time", Enabled = false)]
        public override DateTime? DateTimeBegin { get; set; }
        #endregion
        #region DateTimeEnd
        public new abstract class dateTimeEnd : PX.Data.BQL.BqlDateTime.Field<dateTimeEnd> { }

        [PXDBDateAndTime(UseTimeZone = true, BqlField = typeof(FSAppointmentLog.dateTimeEnd),
                         PreserveTime = true, DisplayNameDate = "End Date", DisplayNameTime = "End Time")]
        [PXUIField(DisplayName = "End Time", Enabled = false)]
        [PXUIVisible(typeof(Where<Current<FSLogActionFilter.action>, Equal<ListField_LogActions.Resume>>))]
        public override DateTime? DateTimeEnd { get; set; }
        #endregion
        #region TimeDuration
        public new abstract class timeDuration : PX.Data.BQL.BqlInt.Field<timeDuration> { }

        [FSDBTimeSpanLong(BqlField = typeof(FSAppointmentLog.timeDuration))]
        [PXUIField(DisplayName = "Duration", Enabled = false)]
        [PXUIVisible(typeof(Where<Current<FSLogActionFilter.action>, Equal<ListField_LogActions.Resume>>))]
        public override int? TimeDuration { get; set; }
        #endregion
        #region Travel
        public new abstract class travel : PX.Data.BQL.BqlBool.Field<travel> { }

        [PXBool]
        [PXFormula(typeof(Where<FSAppointmentLog.itemType, Equal<ListField_Log_ItemType.travel>>))]
        [PXUIField(DisplayName = "Travel", Enabled = false)]
        [PXUIVisible(typeof(Where2<
                                Where<
                                    Current<FSLogActionFilter.action>, Equal<ListField_LogActions.Complete>,
                                Or<
                                    Current<FSLogActionFilter.action>, Equal<ListField_LogActions.Pause>,
                                Or<
                                    Current<FSLogActionFilter.action>, Equal<ListField_LogActions.Resume>>>>,
                            And<
                                Current<FSLogActionFilter.type>, Equal<FSLogTypeAction.Travel>>>))]
        public override bool? Travel { get; set; }
        #endregion
        #region InventoryID
        public abstract class inventoryID : Data.BQL.BqlInt.Field<inventoryID> { }

        [PXDBInt(BqlField = typeof(FSAppointmentDet.inventoryID))]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXSelector(typeof(Search<InventoryItem.inventoryID>), SubstituteKey = typeof(InventoryItem.inventoryCD))]
        [PXUIField(DisplayName = "Inventory ID", Enabled = false)]
        [PXUIVisible(typeof(Where2<
                                Where<
                                    Current<FSLogActionFilter.action>, Equal<ListField_LogActions.Complete>,
                                Or<
                                    Current<FSLogActionFilter.action>, Equal<ListField_LogActions.Resume>,
                                Or<
                                    Current<FSLogActionFilter.action>, Equal<ListField_LogActions.Pause>>>>,
                            And<
                                Current<FSLogActionFilter.type>, Equal<FSLogTypeAction.Service>>>))]
        public virtual int? InventoryID { get; set; }
        #endregion
        #region UserID
        public abstract class userID : Data.BQL.BqlGuid.Field<userID> { }
        [PXDBGuid(BqlField = typeof(EPEmployee.userID))]
        [PXUIField(Enabled = false, Visible = false)]
        public virtual Guid? UserID { get; set; }
        #endregion
        #region Selected
        public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }

        [PXBool]
        [PXFormula(typeof(Switch<Case<Where<userID, Equal<Current<AccessInfo.userID>>>, True>, False>))]
        [PXUIField(DisplayName = "Selected")]
        public virtual bool? Selected { get; set; }
        #endregion

        public new abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
        public new abstract class docRefNbr : PX.Data.BQL.BqlString.Field<docRefNbr> { }
        public new abstract class earningType : PX.Data.BQL.BqlString.Field<earningType> { }
        public new abstract class costCodeID : PX.Data.BQL.BqlInt.Field<costCodeID> { }
        public new abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
        public new abstract class projectTaskID : PX.Data.BQL.BqlInt.Field<projectTaskID> { }
        public new abstract class laborItemID : PX.Data.BQL.BqlInt.Field<laborItemID> { }
        public new abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
        public new abstract class workgroupID : PX.Data.BQL.BqlInt.Field<workgroupID> { }
        public new abstract class timeCardCD : PX.Data.BQL.BqlString.Field<timeCardCD> { }
    }
}