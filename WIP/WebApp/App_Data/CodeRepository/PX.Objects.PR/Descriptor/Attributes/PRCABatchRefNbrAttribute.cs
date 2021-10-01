using PX.Data;
using PX.Objects.CA;
using System;

namespace PX.Objects.PR
{
	public class PRCABatchRefNbrAttribute : PXSelectorAttribute
	{
		public PRCABatchRefNbrAttribute(Type searchType)
			: base(searchType,
				typeof(CABatch.batchNbr),
				typeof(CABatch.tranDate),
				typeof(CABatch.cashAccountID),
				typeof(CABatch.paymentMethodID),
				typeof(PRCABatch.batchTotal),
				typeof(CABatch.extRefNbr))
		{ }
	}
}
