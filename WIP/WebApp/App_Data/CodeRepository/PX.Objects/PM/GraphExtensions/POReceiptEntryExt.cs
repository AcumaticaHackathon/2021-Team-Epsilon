using PX.Data;
using PX.Objects.CS;
using PX.Objects.PO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM
{
    public class POReceiptEntryExt : CommitmentTracking<POReceiptEntry>
    {
        public static new bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.projectAccounting>();
        }

		public PXAction<POReceipt> createAPDocument;
		[PXUIField(DisplayName = PO.Messages.CreateAPInvoice, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = true)]
		[PXProcessButton]
		public virtual System.Collections.IEnumerable CreateAPDocument(PXAdapter adapter)
		{
			if (this.Base.Document.Current != null &&
				this.Base.Document.Current.Released == true)
			{
				POReceipt doc = this.Base.Document.Current;
				if (doc.UnbilledQty != Decimal.Zero)
				{
					ValidateLines();
				}
			}
			return Base.createAPDocument.Press(adapter);
		}

		public virtual void ValidateLines()
		{
			bool validationFailed = false;
			foreach (POReceiptLine line in this.Base.transactions.Select())
			{
				if (line.TaskID != null)
				{
					PMProject project = PXSelectorAttribute.Select<POLine.projectID>(this.Base.transactions.Cache, line) as PMProject;
					if (project.IsActive != true)
					{
						PXUIFieldAttribute.SetError<POLine.projectID>(this.Base.transactions.Cache, line, PO.Messages.ProjectIsNotActive, project.ContractCD);
						validationFailed = true;
					}
					else
					{
						PMTask task = PXSelectorAttribute.Select<POLine.taskID>(this.Base.transactions.Cache, line) as PMTask;
						if (task.IsActive != true)
						{
							PXUIFieldAttribute.SetError<POLine.taskID>(this.Base.transactions.Cache, line, PO.Messages.ProjectTaskIsNotActive, task.TaskCD);
							validationFailed = true;
						}
					}
				}
			}

			if (validationFailed)
			{
				throw new PXException(PO.Messages.LineIsInvalid);
			}
		}
	}
}
