using CommonServiceLocator;
using PX.Data;
using PX.Objects.CA.Descriptor;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PO;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM
{
    public class CommitmentTracking<T> : PXGraphExtension<T> where T : PXGraph
    {
		public PXSelect<PMBudgetAccum> Budget;
		public PXSelect<PMCommitment> InternalCommitments;
		
		[InjectDependency]
		public IBudgetService BudgetService { get; set; }

		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.projectAccounting>();
		}

        protected virtual void _(Events.RowInserted<PMCommitment> e)
		{
			RollUpCommitmentBalance(e.Row, 1);
		}

		protected virtual void _(Events.RowUpdated<PMCommitment> e)
		{
			RollUpCommitmentBalance(e.OldRow, -1);
			RollUpCommitmentBalance(e.Row, 1);
		}

		protected virtual void _(Events.RowDeleted<PMCommitment> e)
		{
			RollUpCommitmentBalance(e.Row, -1);
			ClearEmptyRecords();
		}

		public virtual void RollUpCommitmentBalance(PMCommitment row, int sign)
		{
			if (row == null)
				throw new ArgumentNullException();

			if (row.ProjectID == null || row.ProjectTaskID == null || row.AccountGroupID == null)
				return;

			PMAccountGroup ag = PXSelectorAttribute.Select<PMCommitment.accountGroupID>(Base.Caches[typeof(PMCommitment)], row) as PMAccountGroup;
			PMProject project = PMProject.PK.Find(Base, row.ProjectID);

			ProjectBalance pb = CreateProjectBalance();
			
			bool isExisting;
			PM.Lite.PMBudget target = BudgetService.SelectProjectBalance(row, ag, project, out isExisting);

			var rollupOrigQty = pb.CalculateRollupQty(row, target, row.OrigQty);
			var rollupQty = pb.CalculateRollupQty(row, target, row.Qty);
			var rollupOpenQty = pb.CalculateRollupQty(row, target, row.OpenQty);
			var rollupReceivedQty = pb.CalculateRollupQty(row, target, row.ReceivedQty);
			var rollupInvoicedQty = pb.CalculateRollupQty(row, target, row.InvoicedQty);

			PMBudgetAccum ps = new PMBudgetAccum();
			ps.ProjectID = target.ProjectID;
			ps.ProjectTaskID = target.ProjectTaskID;
			ps.AccountGroupID = target.AccountGroupID;
			ps.Type = target.Type;
			ps.InventoryID = target.InventoryID;
			ps.CostCodeID = target.CostCodeID;
			ps.UOM = target.UOM;
			ps.IsProduction = target.IsProduction;
			ps.Description = target.Description;

			ps.CuryInfoID = project.CuryInfoID;


			ps = Budget.Insert(ps);
			ps.CommittedOrigQty += sign * rollupOrigQty;
			ps.CommittedQty += sign * rollupQty;
			ps.CommittedOpenQty += sign * rollupOpenQty;
			ps.CommittedReceivedQty += sign * rollupReceivedQty;
			ps.CommittedInvoicedQty += sign * rollupInvoicedQty;
			ps.CuryCommittedOrigAmount += sign * row.OrigAmount.GetValueOrDefault();
			ps.CuryCommittedAmount += sign * row.Amount.GetValueOrDefault();
			ps.CuryCommittedOpenAmount += sign * row.OpenAmount.GetValueOrDefault();
			ps.CuryCommittedInvoicedAmount += sign * row.InvoicedAmount.GetValueOrDefault();

			if (ConversionToBaseRequired(Base, project))
			{
				decimal amtInBase;

				PXCurrencyAttribute.CuryConvBase<PMProject.curyInfoID>(Base.Caches[typeof(PMProject)], project, row.OrigAmount.GetValueOrDefault(), out amtInBase);
				ps.CommittedOrigAmount += sign * amtInBase;

				PXCurrencyAttribute.CuryConvBase<PMProject.curyInfoID>(Base.Caches[typeof(PMProject)], project, row.Amount.GetValueOrDefault(), out amtInBase);
				ps.CommittedAmount += sign * amtInBase;

				PXCurrencyAttribute.CuryConvBase<PMProject.curyInfoID>(Base.Caches[typeof(PMProject)], project, row.OpenAmount.GetValueOrDefault(), out amtInBase);
				ps.CommittedOpenAmount += sign * amtInBase;

				PXCurrencyAttribute.CuryConvBase<PMProject.curyInfoID>(Base.Caches[typeof(PMProject)], project, row.InvoicedAmount.GetValueOrDefault(), out amtInBase);
				ps.CommittedInvoicedAmount += sign * amtInBase;
			}
			else
			{
				ps.CommittedOrigAmount += sign * row.OrigAmount.GetValueOrDefault();
				ps.CommittedAmount += sign * row.Amount.GetValueOrDefault();
				ps.CommittedOpenAmount += sign * row.OpenAmount.GetValueOrDefault();
				ps.CommittedInvoicedAmount += sign * row.InvoicedAmount.GetValueOrDefault();
			}

		}

		private bool ConversionToBaseRequired(PXGraph graph, PMProject project)
		{
			if (!PXAccess.FeatureInstalled<FeaturesSet.projectMultiCurrency>())
				return false;

			Company company = PXSelect<Company>.Select(graph);
			if (project != null && company != null && project.CuryID == company.BaseCuryID)
			{
				return false;
			}

			return true;
		}


		public virtual ProjectBalance CreateProjectBalance()
		{
			return new ProjectBalance(this.Base);
		}

		protected virtual void ClearEmptyRecords()
		{
			foreach (PMBudgetAccum record in Budget.Cache.Inserted)
			{
				if (IsEmptyCommitmentChange(record))
					Budget.Cache.Remove(record);
			}
		}

		private bool IsEmptyCommitmentChange(PMBudgetAccum item)
		{
			if (item.CommittedOrigQty.GetValueOrDefault() != 0) return false;
			if (item.CommittedQty.GetValueOrDefault() != 0) return false;
			if (item.CommittedOpenQty.GetValueOrDefault() != 0) return false;
			if (item.CommittedReceivedQty.GetValueOrDefault() != 0) return false;
			if (item.CommittedInvoicedQty.GetValueOrDefault() != 0) return false;
			if (item.CuryCommittedOrigAmount.GetValueOrDefault() != 0) return false;
			if (item.CuryCommittedAmount.GetValueOrDefault() != 0) return false;
			if (item.CuryCommittedOpenAmount.GetValueOrDefault() != 0) return false;
			if (item.CuryCommittedInvoicedAmount.GetValueOrDefault() != 0) return false;
			if (item.CommittedOrigAmount.GetValueOrDefault() != 0) return false;
			if (item.CommittedAmount.GetValueOrDefault() != 0) return false;
			if (item.CommittedOpenAmount.GetValueOrDefault() != 0) return false;
			if (item.CommittedInvoicedAmount.GetValueOrDefault() != 0) return false;

			return true;
		}
	}

}