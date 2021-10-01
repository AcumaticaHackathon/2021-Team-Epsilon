using PX.Commerce.Core;
using PX.Data;

namespace PX.Commerce.BigCommerce
{
	public class BigCommerceEntityMaintExt : PXGraphExtension<BCEntityMaint>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.BigCommerceConnector; }

		protected virtual void _(Events.RowSelected<BCEntityImportFilter> e)
		{
			BCEntityImportFilter row = e.Row as BCEntityImportFilter;
			if (row != null && row?.ConnectorType == BCConnector.TYPE && (row?.FieldName == BCConstants.CreatedDateTime || row?.FieldName == BCConstants.ModifiedDateTime))
				e.Cache.RaiseExceptionHandling<BCEntityImportFilter.fieldName>(row, row.FieldName,
						new PXSetPropertyException(BCMessages.DateTimeDiscrepancyWarning, PXErrorLevel.RowWarning));
		}

	}
}