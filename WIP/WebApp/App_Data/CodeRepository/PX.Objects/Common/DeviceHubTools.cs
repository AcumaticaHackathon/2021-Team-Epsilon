using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PX.Common;
using PX.Data;
using PX.SM;

namespace PX.Objects.Common
{
	public static class DeviceHubTools
	{
		public static void PrintReportViaDeviceHub<TBAccount>(PXGraph graph, string reportID, Dictionary<string, string> reportParameters, string notificationSource, TBAccount baccount)
			where TBAccount : CR.BAccount
		{
			CR.NotificationUtility notificationUtility = new CR.NotificationUtility(graph);
			var reportsToPrint = SMPrintJobMaint.AssignPrintJobToPrinter(
				new Dictionary<PrintSettings, PXReportRequiredException>(),
				reportParameters,
				new PrintSettings { PrintWithDeviceHub = true, DefinePrinterManually = false },
				notificationUtility.SearchPrinter,
				notificationSource,
				reportID,
				baccount == null ? reportID : notificationUtility.SearchReport(notificationSource, baccount, reportID, graph.Accessinfo.BranchID),
				graph.Accessinfo.BranchID);

			SMPrintJobMaint.CreatePrintJobGroups(reportsToPrint);
		}
	}
}