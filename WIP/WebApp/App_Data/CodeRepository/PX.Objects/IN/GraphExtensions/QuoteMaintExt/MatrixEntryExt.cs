using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN.Matrix.Interfaces;
using PX.Objects.CR;
using SiteStatus = PX.Objects.IN.Overrides.INDocumentRelease.SiteStatus;

namespace PX.Objects.IN.GraphExtensions.QuoteMaintExt
{
	public class MatrixEntryExt : Matrix.GraphExtensions.SmartPanelExt<QuoteMaint, CRQuote>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.matrixItem>();
		}

		protected override IEnumerable<IMatrixItemLine> GetLines(int? siteID, int? inventoryID)
			=> Base.Products.SelectMain().Where(l => l.InventoryID == inventoryID && (l.SiteID == siteID || siteID == null));

		protected override IEnumerable<IMatrixItemLine> GetLines(int? siteID, int? inventoryID, string taxCategoryID)
		{ 
			return Base.Products.SelectMain().Where(l => l.InventoryID == inventoryID && (l.SiteID == siteID || siteID == null)
				&& (l.TaxCategoryID == taxCategoryID || taxCategoryID == null));
		}

		protected override void UpdateLine(IMatrixItemLine line)
			=> Base.Products.Update((CROpportunityProducts)line);

		protected override void CreateNewLine(int? siteID, int? inventoryID, decimal qty)
			=> CreateNewLine(siteID, inventoryID, null, qty);

		protected override void CreateNewLine(int? siteID, int? inventoryID, string taxCategoryID, decimal qty)
		{
			CROpportunityProducts newline = Base.Products.Insert();

			if (newline == null) // Temporary fix for AC-179084
			{
				int productCntr = CROpportunity.PK.Find(Base, Base.Quote.Current?.OpportunityID)?.ProductCntr ?? 1;
				for (int tryIndex = 0; tryIndex <= productCntr; tryIndex++)
				{
					newline = Base.Products.Insert();
					if (newline != null)
						break;
				}
			}

			newline = PXCache<CROpportunityProducts>.CreateCopy(newline);
			newline.SiteID = siteID;
			newline.InventoryID = inventoryID;
			newline = PXCache<CROpportunityProducts>.CreateCopy(Base.Products.Update(newline));
			newline.Qty = qty;
			newline = Base.Products.Update(newline);

			if (!string.IsNullOrEmpty(taxCategoryID))
			{
				Base.Products.Cache.SetValueExt<CROpportunityProducts.taxCategoryID>(newline, taxCategoryID);
				newline = Base.Products.Update(newline);
			}
		}

		protected override bool IsDocumentOpen()
			=> Base.Products.Cache.AllowInsert;

		protected override void DeductAllocated(SiteStatus allocated, IMatrixItemLine line)
		{
		}

		protected override string GetAvailabilityMessage(int? siteID, InventoryItem item, SiteStatus availability)
		{
			return null;
		}

		protected override int? GetQtyPrecision()
		{
			object returnValue = null;
			Base.Products.Cache.RaiseFieldSelecting<CROpportunityProducts.quantity>(null, ref returnValue, true);
			if (returnValue is PXDecimalState state)
				return state.Precision;
			return null;
		}

		protected override bool IsItemStatusDisabled(InventoryItem item)
		{
			return base.IsItemStatusDisabled(item) || item?.ItemStatus == InventoryItemStatus.NoSales;
		}
	}
}
