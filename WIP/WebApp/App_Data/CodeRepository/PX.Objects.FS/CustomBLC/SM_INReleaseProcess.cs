using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.FS
{
    public class SM_INReleaseProcess : PXGraphExtension<INReleaseProcess>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public override void Initialize()
        {
            base.Initialize();
            Base.onBeforeSalesOrderProcessPOReceipt = ProcessPOReceipt;
        }

        public bool updateCosts = false;

        #region Views
        public PXSelect<FSServiceOrder> serviceOrderView;
        public PXSelect<FSSODetSplit> soDetSplitView;
        public PXSelect<FSAppointmentDet> apptDetView;
        public PXSelect<FSApptLineSplit> apptSplitView;
        #endregion

        #region CacheAttached
        [PXDBString(IsKey = true, IsUnicode = true)]
        [PXParent(typeof(Select<FSServiceOrder, Where<FSServiceOrder.srvOrdType, Equal<Current<FSSODetSplit.srvOrdType>>, And<FSServiceOrder.refNbr, Equal<Current<FSSODetSplit.refNbr>>>>>))]
        [PXDefault()]
        protected virtual void FSSODetSplit_RefNbr_CacheAttached(PXCache sender)
        {
        }

        [PXDBDate()]
        [PXDefault()]
        protected virtual void FSSODetSplit_OrderDate_CacheAttached(PXCache sender)
        {
        }

        [PXDBLong()]
        [INItemPlanIDSimple()]
        protected virtual void FSSODetSplit_PlanID_CacheAttached(PXCache sender)
        {
        }

        [PXDBInt()]
        protected virtual void FSSODetSplit_SiteID_CacheAttached(PXCache sender)
        {
        }

        [PXDBInt()]
        protected virtual void FSSODetSplit_LocationID_CacheAttached(PXCache sender)
        {
        }

        // The selector is removed to avoid validation.
        #region Remove LotSerialNbr Selector
        [PXDBString]
        protected void _(Events.CacheAttached<FSSODetSplit.lotSerialNbr> e) { }

        [PXDBString]
        protected void _(Events.CacheAttached<FSAppointmentDet.lotSerialNbr> e) { }

        [PXDBString]
        protected void _(Events.CacheAttached<FSApptLineSplit.lotSerialNbr> e) { }
        #endregion

        // Attribute PXDBDefault is removed to prevent values ​​explicitly assigned by code from being changed in the Persist.
        #region Remove PXDBDefault
        [PXDBString(20, IsKey = true, IsUnicode = true, InputMask = "")]
        protected void _(Events.CacheAttached<FSApptLineSplit.apptNbr> e) { }

        [PXDBString(15, IsUnicode = true, InputMask = "")]
        protected void _(Events.CacheAttached<FSApptLineSplit.origSrvOrdNbr> e) { }

        [PXDBInt()]
        protected void _(Events.CacheAttached<FSApptLineSplit.origLineNbr> e) { }

        [PXDBDate()]
        protected void _(Events.CacheAttached<FSApptLineSplit.apptDate> e) { }
        #endregion

        #region Remove CheckUnique
        [PXDBInt(IsKey = true)]
        [PXLineNbr(typeof(FSAppointment.lineCntr))]
        [PXUIField(DisplayName = "Line Nbr.", Visible = false, Enabled = false)]
        [PXFormula(null, typeof(MaxCalc<FSAppointment.maxLineNbr>))]
        protected void _(Events.CacheAttached<FSAppointmentDet.lineNbr> e) { }
        #endregion
        #endregion

        public virtual List<PXResult<INItemPlan, INPlanType>> ProcessPOReceipt(PXGraph graph, IEnumerable<PXResult<INItemPlan, INPlanType>> list, string POReceiptType, string POReceiptNbr)
        {
            return FSPOReceiptProcess.ProcessPOReceipt(graph, list, POReceiptType, POReceiptNbr, stockItemProcessing: true);
        }

        [Obsolete(Common.Messages.MethodIsObsoleteRemoveInLaterAcumaticaVersions)]
        public virtual bool IsSMRelated()
        {
            if (Base.inregister.Current == null)
            {
                return false;
            }

            int count = PXSelect<FSPostDet,
                        Where2<
                            Where<FSPostDet.sOPosted, Equal<True>,
                                And<FSPostDet.sOOrderType, Equal<Required<FSPostDet.sOOrderType>>,
                                And<FSPostDet.sOOrderNbr, Equal<Required<FSPostDet.sOOrderNbr>>>>>,
                        Or<
                            Where<FSPostDet.sOInvPosted, Equal<True>,
                                And<FSPostDet.sOInvDocType, Equal<Required<FSPostDet.sOInvDocType>>,
                                And<FSPostDet.sOInvRefNbr, Equal<Required<FSPostDet.sOInvRefNbr>>>>>>>>
                       .Select(Base, Base.inregister.Current.SOOrderType, Base.inregister.Current.SOOrderNbr, Base.inregister.Current.SrcDocType, Base.inregister.Current.SrcRefNbr)
                       .Count();

            return count > 0;
        }

        #region Overrides
        public delegate void PersistDelegate();

        [PXOverride]
        public virtual void Persist(PersistDelegate baseMethod)
        {
            if (SharedFunctions.isFSSetupSet(Base) == false)
            {
                baseMethod();
                return;
            }
            
            baseMethod();
            UpdateCosts();
        }

        public delegate void ReleaseDocProcDelegate(JournalEntry je, INRegister doc);

        [PXOverride]
        public virtual void ReleaseDocProc(JournalEntry je, INRegister doc, ReleaseDocProcDelegate del)
        {
            ValidatePostBatchStatus(PXDBOperation.Update, ID.Batch_PostTo.IN, doc.DocType, doc.RefNbr);

            if (del != null)
            {
                del(je, doc);
            }
        }
        #endregion

        public virtual void UpdateCosts()
        {
            FSPostRegister fsPostRegisterRow = GetPostRegister();
            if (fsPostRegisterRow != null 
                && string.IsNullOrEmpty(fsPostRegisterRow.RefNbr) == false)
            {
                List<FSAppointment> apptList = new List<FSAppointment>();
                PXGraph graphToUpdate = new PXGraph();

                PXCache<FSAppointment> apptCache = new PXCache<FSAppointment>(Base);

                PXSelectBase<FSPostDet> cmd = new PXSelect<FSPostDet>(Base);

                string postDocNbr = null;
                string postDocType = null;

                if (fsPostRegisterRow.PostedTO == ID.Batch_PostTo.SO)
                {
                    cmd.Join<InnerJoin<INTran,
                                        On<INTran.sOOrderType, Equal<FSPostDet.sOOrderType>,
                                            And<INTran.sOOrderNbr, Equal<FSPostDet.sOOrderNbr>,
                                            And<INTran.sOOrderLineNbr, Equal<FSPostDet.sOLineNbr>>>>>>();

                    cmd.WhereAnd<Where<FSPostDet.sOOrderType, Equal<Required<FSPostDet.sOOrderType>>,
                                     And<FSPostDet.sOOrderNbr, Equal<Required<FSPostDet.sOOrderNbr>>>>>();

                    postDocNbr = Base.inregister.Current.SOOrderNbr;
                    postDocType = Base.inregister.Current.SOOrderType;

                } 
                else if (fsPostRegisterRow.PostedTO == ID.Batch_PostTo.SI) 
                {
                    cmd.Join<InnerJoin<INTran,
                                        On<INTran.aRDocType, Equal<FSPostDet.sOInvDocType>,
                                            And<INTran.aRRefNbr, Equal<FSPostDet.sOInvRefNbr>,
                                            And<INTran.aRLineNbr, Equal<FSPostDet.sOInvLineNbr>>>>>>();

                    cmd.WhereAnd<Where<FSPostDet.sOInvDocType, Equal<Required<FSPostDet.sOInvDocType>>,
                                    And<FSPostDet.sOInvRefNbr, Equal<Required<FSPostDet.sOInvRefNbr>>>>>();

                    postDocType = Base.inregister.Current.SrcDocType;
                    postDocNbr = Base.inregister.Current.SrcRefNbr;
                }
                
                if (fsPostRegisterRow.EntityType == ID.PostDoc_EntityType.SERVICE_ORDER)
                {
                    cmd.Join<InnerJoin<FSSODet,
                                        On<FSSODet.postID, Equal<FSPostDet.postID>>>>();

                    cmd.Join<InnerJoin<FSAppointmentDet,
                                        On<FSAppointmentDet.sODetID, Equal<FSSODet.sODetID>>>>();

                    cmd.Join<InnerJoin<FSAppointment,
                                        On<FSAppointment.appointmentID, Equal<FSAppointmentDet.appointmentID>>>>();

                    var result = cmd.Select(postDocType, postDocNbr);

                    using (PXTransactionScope ts = new PXTransactionScope())
                    {
                        foreach (PXResult<FSPostDet, INTran, FSSODet, FSAppointmentDet, FSAppointment> bqlResult in result)
                        {
                            FSAppointment fsAppointmentRow = (FSAppointment)bqlResult;
                            FSAppointmentDet fsAppointmentDetRow = (FSAppointmentDet)bqlResult;
                            INTran inTranRow = (INTran)bqlResult;

                            UpdateCostInAppointmentDetail(graphToUpdate, apptCache, fsAppointmentRow, fsAppointmentDetRow, inTranRow);

                            if (apptList.Contains(fsAppointmentRow) == false)
                            {
                                apptList.Add(fsAppointmentRow);
                            }
                        }

                        UpdateCostTotalInAppointmentDocuments(graphToUpdate, apptCache, apptList);
                        ts.Complete();
                    }
                }
                else if (fsPostRegisterRow.EntityType == ID.PostDoc_EntityType.APPOINTMENT)
                {
                    cmd.Join<InnerJoin<FSAppointmentDet,
                                        On<FSAppointmentDet.postID, Equal<FSPostDet.postID>>>>();

                    cmd.Join<InnerJoin<FSAppointment,
                                        On<FSAppointment.appointmentID, Equal<FSAppointmentDet.appointmentID>>>>();

                    var result = cmd.Select(postDocType, postDocNbr);

                    using (PXTransactionScope ts = new PXTransactionScope())
                    {
                        foreach (PXResult<FSPostDet, INTran, FSAppointmentDet, FSAppointment> bqlResult in result)
                        {
                            FSAppointment fsAppointmentRow = (FSAppointment)bqlResult;
                            FSAppointmentDet fsAppointmentDetRow = (FSAppointmentDet)bqlResult;
                            INTran inTranRow = (INTran)bqlResult;

                            UpdateCostInAppointmentDetail(graphToUpdate, apptCache, fsAppointmentRow, fsAppointmentDetRow, inTranRow);

                            if (apptList.Contains(fsAppointmentRow) == false)
                            {
                                apptList.Add(fsAppointmentRow);
                            }
                        }

                        UpdateCostTotalInAppointmentDocuments(graphToUpdate, apptCache, apptList);
                        ts.Complete();
                    }
                }
            }
        }

        public virtual FSPostRegister GetPostRegister()
        {
            if (Base.inregister.Current == null)
            {
                return null;
            }

            var row = PXSelect<FSPostRegister,
                        Where2<
                            Where<FSPostRegister.postedTO, Equal<ListField_PostTo.SO>,
                                And<FSPostRegister.postDocType, Equal<Required<FSPostRegister.postDocType>>,
                                And<FSPostRegister.postRefNbr, Equal<Required<FSPostRegister.postRefNbr>>>>>,
                        Or<
                            Where<FSPostRegister.postedTO, Equal<ListField_PostTo.SI>,
                                And<FSPostRegister.postDocType, Equal<Required<FSPostRegister.postDocType>>,
                                And<FSPostRegister.postRefNbr, Equal<Required<FSPostRegister.postRefNbr>>>>>>>>
                       .SelectWindowed(Base, 0, 1, Base.inregister.Current.SOOrderType, Base.inregister.Current.SOOrderNbr, Base.inregister.Current.SrcDocType, Base.inregister.Current.SrcRefNbr)
                       .FirstOrDefault();

            return row;
        }

        public void UpdateCostInAppointmentDetail(PXGraph graphToUpdate, PXCache apptCache, FSAppointment fsAppointmentRow, FSAppointmentDet fsAppointmentDetRow, INTran inTranRow) 
        {
            decimal curyUnitCost = 0.0m;
            decimal curyTranCost = 0.0m;

            CM.PXDBCurrencyAttribute.CuryConvCury(apptCache, fsAppointmentRow, inTranRow.UnitCost != null ? (decimal)inTranRow.UnitCost : 0m, out curyUnitCost, CommonSetupDecPl.PrcCst);
            CM.PXDBCurrencyAttribute.CuryConvCury(apptCache, fsAppointmentRow, inTranRow.TranCost != null ? (decimal)inTranRow.TranCost : 0m, out curyTranCost, CommonSetupDecPl.PrcCst);

            if (inTranRow.RefNbr != null)
            {
                PXUpdate<
                    Set<FSAppointmentDet.unitCost, Required<FSAppointmentDet.unitCost>,
                    Set<FSAppointmentDet.curyUnitCost, Required<FSAppointmentDet.curyUnitCost>,
                    Set<FSAppointmentDet.extCost, Required<FSAppointmentDet.extCost>,
                    Set<FSAppointmentDet.curyExtCost, Required<FSAppointmentDet.curyExtCost>>>>>,
                FSAppointmentDet,
                Where<
                    FSAppointmentDet.appDetID, Equal<Required<FSAppointmentDet.appDetID>>>>
                .Update(
                    graphToUpdate,
                    inTranRow.UnitCost,
                    curyUnitCost,
                    inTranRow.TranCost,
                    curyTranCost,
                    fsAppointmentDetRow.AppDetID);
            }
        }

        public void UpdateCostTotalInAppointmentDocuments(PXGraph graphToUpdate, PXCache apptCache, List<FSAppointment> apptList) 
        {
            foreach (FSAppointment appt in apptList)
            {
                decimal? costTotal = 0.0m;
                decimal curyCostTotal = 0.0m;

                foreach (FSAppointmentLog logLine in
                        PXSelectReadonly<FSAppointmentLog,
                        Where<FSAppointmentLog.docID, Equal<Required<FSAppointmentLog.docID>>>>
                        .Select(graphToUpdate, appt.AppointmentID))
                {
                    costTotal += logLine.ExtCost;
                }

                foreach (FSAppointmentDet appLine in
                        PXSelect<FSAppointmentDet,
                        Where<FSAppointmentDet.appointmentID, Equal<Required<FSAppointmentDet.appointmentID>>>>
                        .Select(graphToUpdate, appt.AppointmentID))
                {
                    costTotal += appLine.ExtCost;
                }

                CM.PXDBCurrencyAttribute.CuryConvCury(apptCache, appt, (decimal)costTotal, out curyCostTotal, CommonSetupDecPl.PrcCst);

                PXUpdate<
                    Set<FSAppointment.costTotal, Required<FSAppointment.costTotal>,
                    Set<FSAppointment.curyCostTotal, Required<FSAppointment.curyCostTotal>>>,
                FSAppointment,
                Where<
                    FSAppointment.appointmentID, Equal<Required<FSAppointment.appointmentID>>>>
                .Update(graphToUpdate,
                        costTotal,
                        curyCostTotal,
                        appt.AppointmentID);
            }
        }

        #region Validations
        public virtual void ValidatePostBatchStatus(PXDBOperation dbOperation, string postTo, string createdDocType, string createdRefNbr)
        {
            DocGenerationHelper.ValidatePostBatchStatus<INRegister>(Base, dbOperation, postTo, createdDocType, createdRefNbr);
        }
        #endregion
    }
}
