using PX.Data;
using PX.Objects.AP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Objects.PO;
using PX.Objects.CM;
using PX.Objects.IN;

namespace PX.Objects.PM
{
	/// <summary>
	/// Extends AP Invoice Entry with Project related functionality. Requires Project Accounting feature.
	/// </summary>
	public class APInvoiceEntryExt : PXGraphExtension<APInvoiceEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<CS.FeaturesSet.projectAccounting>();
		}

		[PXOverride]
		public virtual void CopyCustomizationFieldsToAPTran(APTran apTranToFill, IAPTranSource poSourceLine, bool areCurrenciesSame)
		{
			if (CopyProjectFromLine(poSourceLine))
			{
				apTranToFill.ProjectID = poSourceLine.ProjectID;
				apTranToFill.TaskID = poSourceLine.TaskID;
			}
			else
			{
				apTranToFill.ProjectID = ProjectDefaultAttribute.NonProject();
			}

			apTranToFill.CostCodeID = poSourceLine.CostCodeID;
						
			if (IsPartiallyBilledCompleteByAmountSubcontractLine(apTranToFill, poSourceLine))
			{
				RedefaultInvoiceAmountForSubcontract(apTranToFill, poSourceLine, areCurrenciesSame);
			}
			else if(IsDebitAdjSubcontractLine(apTranToFill, poSourceLine))
			{
				RedefaultDebitAdjAmountForSubcontract(apTranToFill, poSourceLine, areCurrenciesSame);
			}
		}

		protected virtual bool CopyProjectFromLine(IAPTranSource poSourceLine)
		{
			return true;
		}

		private bool IsPartiallyBilledCompleteByAmountSubcontractLine(APTran tran, IAPTranSource line)
		{
			return line.OrderType == POOrderType.RegularSubcontract &&
					tran.TranType != APDocType.DebitAdj &&
					line.CompletePOLine == CompletePOLineTypes.Amount &&
					line.IsPartiallyBilled;
		}

		private void RedefaultInvoiceAmountForSubcontract(APTran tran, IAPTranSource line, bool areCurrenciesSame)
		{
			tran.CuryRetainageAmt = null;
			if (areCurrenciesSame)
			{
				tran.CuryLineAmt = line.UnbilledAmt;
			}
			else
			{
				decimal unbilledAmount;
				PXCurrencyAttribute.PXCurrencyHelper.CuryConvCury(Base.Document.Cache, Base.Document.Current, line.UnbilledAmt.GetValueOrDefault(), out unbilledAmount);
				tran.CuryLineAmt = unbilledAmount;
			}
		}

		private bool IsDebitAdjSubcontractLine(APTran tran, IAPTranSource line)
		{
			return line.OrderType == POOrderType.RegularSubcontract &&
					tran.TranType == APDocType.DebitAdj;
		}

		private void RedefaultDebitAdjAmountForSubcontract(APTran tran, IAPTranSource line, bool areCurrenciesSame)
		{
			tran.Qty = 0;
			tran.BaseQty = 0;
			tran.UnitCost = 0;
			tran.CuryUnitCost = 0;
			tran.RetainagePct = 0;
			tran.RetainageAmt = 0;
			tran.CuryRetainageAmt = 0;

			if (areCurrenciesSame)
			{
				tran.CuryLineAmt = line.BilledAmt;
			}
			else
			{
				decimal billedAmount;
				PXCurrencyAttribute.PXCurrencyHelper.CuryConvCury(Base.Document.Cache, Base.Document.Current, line.BilledAmt.GetValueOrDefault(), out billedAmount);
				tran.CuryLineAmt = billedAmount;
			}
		}

		[PXOverride]
		public virtual APTran InsertTranOnAddPOReceiptLine(IAPTranSource line, APTran tran, Func<IAPTranSource, APTran, APTran> baseMethod)
        {
			if (line.OrderType == POOrderType.RegularSubcontract)
			{
				if (tran.TranType == APDocType.DebitAdj)
				{
					tran.RetainagePct = 0;
					tran.CuryRetainageAmt = 0;
					tran.RetainageAmt = 0;
				}
				else
				{
					tran.CuryRetainageAmt = null;
				}
				tran.CuryDiscAmt = 0;
				tran.DiscAmt = 0;
				tran.DiscPct = 0;
			}

			return baseMethod(line, tran);
		}
	}
}
