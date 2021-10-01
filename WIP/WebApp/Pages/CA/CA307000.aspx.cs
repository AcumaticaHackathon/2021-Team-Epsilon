using System;
using System.Drawing;
using System.Web.UI.WebControls;

using PX.Data;
using PX.Objects.CA;

public partial class Page_CA307000 : PX.Web.UI.PXPage
{
	private static class TranCss
	{
		public const string Refund = "CssRefund";
	}

	protected void Page_Load(object sender, EventArgs e)
	{
		RegisterStyle(TranCss.Refund, Color.Salmon);
	}

	private void RegisterStyle(string name, Color backColor)
	{
		var style = new Style
		{
			BackColor = backColor
		};
		Page.Header.StyleSheet.CreateStyleRule(style, this, "." + name);
	}

	protected void Tran_RowDataBound(object sender, PX.Web.UI.PXGridRowEventArgs e)
	{
		var row = (CCBatchTransaction)(PXResult<CCBatchTransaction>)(e.Row.DataItem);
		if (row?.SettlementStatus == CCBatchTranSettlementStatusCode.RefundSettledSuccessfully)
		{
			e.Row.Style.CssClass = TranCss.Refund;
		}
	}
}
