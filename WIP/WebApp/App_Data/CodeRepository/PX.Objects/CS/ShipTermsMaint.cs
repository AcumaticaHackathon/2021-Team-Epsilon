using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.CS
{
	public class ShipTermsMaint : PXGraph<CarrierMaint, ShipTerms>
	{
		public PXSelect<ShipTerms> ShipTermsCurrent;
		public PXSelect<ShipTermsDetail, Where<ShipTermsDetail.shipTermsID, Equal<Current<ShipTerms.shipTermsID>>>> ShipTermsDetail;

		protected virtual void ShipTermsDetail_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			ShipTermsDetail doc = (ShipTermsDetail)e.Row;

			if (doc.BreakAmount < 0)
			{
				if (sender.RaiseExceptionHandling<ShipTermsDetail.breakAmount>(e.Row, null, new PXSetPropertyException(Messages.FieldShouldBePositive, $"[{nameof(CS.ShipTermsDetail.breakAmount)}]")))
				{
					throw new PXRowPersistingException(typeof(ShipTermsDetail.breakAmount).Name, null, Messages.FieldShouldBePositive, nameof(CS.ShipTermsDetail.breakAmount));
				}
				e.Cancel = true;
			}
			if (doc.FreightCostPercent < 0)
			{
				if (sender.RaiseExceptionHandling<ShipTermsDetail.freightCostPercent>(e.Row, null, new PXSetPropertyException(Messages.FieldShouldNotBeNegative, $"[{nameof(CS.ShipTermsDetail.freightCostPercent)}]")))
				{
					throw new PXRowPersistingException(typeof(ShipTermsDetail.freightCostPercent).Name, null, Messages.FieldShouldNotBeNegative, nameof(CS.ShipTermsDetail.freightCostPercent));
				}
				e.Cancel = true;
			}
			if (doc.InvoiceAmountPercent < 0)
			{
				if (sender.RaiseExceptionHandling<ShipTermsDetail.invoiceAmountPercent>(e.Row, null, new PXSetPropertyException(Messages.FieldShouldNotBeNegative, $"[{nameof(CS.ShipTermsDetail.invoiceAmountPercent)}]")))
				{
					throw new PXRowPersistingException(typeof(ShipTermsDetail.invoiceAmountPercent).Name, null, Messages.FieldShouldNotBeNegative, nameof(CS.ShipTermsDetail.invoiceAmountPercent));
				}
				e.Cancel = true;
			}
		}
	}
}
