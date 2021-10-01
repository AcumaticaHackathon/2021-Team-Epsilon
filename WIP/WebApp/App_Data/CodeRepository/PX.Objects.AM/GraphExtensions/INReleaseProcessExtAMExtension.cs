using PX.Data;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Objects.PM.GraphExtensions;
using PX.Objects.GL;
using static PX.Objects.IN.INReleaseProcess;
using PX.Objects.PM;
using PX.Objects.AM.CacheExtensions;

namespace PX.Objects.AM.GraphExtensions
{
    public class INReleaseProcessExtAMExtension : PXGraphExtension<INReleaseProcessExt, INReleaseProcess>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<CS.FeaturesSet.manufacturing>();
        }

        [PXOverride]
        public virtual GLTran InsertGLCostsDebit(JournalEntry je, GLTran tran, GLTranInsertionContext context, Func<JournalEntry, GLTran, GLTranInsertionContext, GLTran> baseMethod)
        {
            int? locProjectID;
            int? locTaskID = null;
            if (context.Location != null && context.Location.ProjectID != null)//can be null if Adjustment
            {
                locProjectID = context.Location.ProjectID;
                locTaskID = context.Location.TaskID;

                if (locTaskID == null)//Location with ProjectTask WildCard
                {
                    if (context.Location.ProjectID == context.INTran.ProjectID)
                    {
                        locTaskID = context.INTran.TaskID;
                    }
                    else
                    {
                        //substitute with any task from the project.
                        PMTask task = PXSelect<PMTask, Where<PMTask.projectID, Equal<Required<PMTask.projectID>>,
                            And<PMTask.visibleInIN, Equal<True>, And<PMTask.isActive, Equal<True>>>>>.Select(Base, context.Location.ProjectID);
                        if (task != null)
                        {
                            locTaskID = task.TaskID;
                        }
                    }
                }

            }
            else
            {
                locProjectID = PM.ProjectDefaultAttribute.NonProject();
            }

            if (context.TranCost.TranType == INTranType.Adjustment || context.TranCost.TranType == INTranType.Transfer)
            {
                tran.ProjectID = locProjectID;
                tran.TaskID = locTaskID;
                var inTranExt = context.INTran.GetExtension<INTranExt>();
                if(context.TranCost.TranType == INTranType.Adjustment && inTranExt?.AMProdOrdID != null)
                {
                    AMProdItem amproditem = PXSelect<AMProdItem, Where<AMProdItem.orderType, Equal<Required<AMProdItem.prodOrdID>>,
                        And<AMProdItem.prodOrdID, Equal<Required<AMProdItem.prodOrdID>>>>>.Select(Base, inTranExt.AMOrderType, inTranExt.AMProdOrdID);
                    if(amproditem?.UpdateProject == true)
                    {
                        tran.ProjectID = context.INTran.ProjectID ?? locProjectID;
                        tran.TaskID = context.INTran.TaskID ?? locTaskID;
                    }
                }
            }
            else
            {
                tran.ProjectID = context.INTran.ProjectID ?? locProjectID;
                tran.TaskID = context.INTran.TaskID ?? locTaskID;
            }
            tran.CostCodeID = context.INTran.CostCodeID;
            return je.GLTranModuleBatNbr.Insert(tran);
        }
    }
}
