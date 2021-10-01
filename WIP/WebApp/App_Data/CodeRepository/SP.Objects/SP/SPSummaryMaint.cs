using System;
using System.Collections;
using PX.Common;
using PX.Data;
using System.Collections.Generic;
using PX.Objects.AR;
using PX.Objects.CR;
using SP.Objects.CR;
using SP.Objects.SP;

namespace SP.Objects.SP
{
	public class SPSummaryMaint : PXGraph<SPSummaryMaint>
	{
		#region Dac
		[SerializableAttribute()]
		public partial class SPSummary : IBqlTable
		{
			#region Name
			public abstract class name : IBqlField
			{
			}
			protected String _Name;
			[PXString(IsKey = true)]
			[PXUIField(DisplayName = "Name")]
			public virtual String Name
			{
				get { return this._Name; }
				set { this._Name = value; }
			}
			#endregion

			#region Data
			public abstract class data : IBqlField
			{
			}
			protected String _Data;
			[PXString]
			[PXUIField(DisplayName = "Data")]
			public virtual String Data
			{
				get { return this._Data; }
				set { this._Data = value; }
			}
			#endregion

			#region Order
			public abstract class order : IBqlField
			{
			}
			protected Int32? _Order;
			[PXInt]
			[PXUIField(DisplayName = "Order")]
			public virtual Int32? Order
			{
				get { return this._Order; }
				set { this._Order = value; }
			}
			#endregion
		}
		#endregion

		#region Select
		public PXSelectOrderBy<SPSummary,
			OrderBy<Asc<SPSummary.order>>> CustomerSummary;

		public PXSelectOrderBy<CRAnnouncement,
			OrderBy<Asc<CRAnnouncement.order>>> Announcements;
		#endregion

		#region Delegates
		public virtual IEnumerable customerSummary()
		{
			int Order = 0;
			List<SPSummary> ret = new List<SPSummary>();

			SPSummary record1 = new SPSummary();
			var graph1 = CreateInstance<ARDocumentEnq>();
			graph1.Filter.Insert();
			graph1.Filter.Select();
			record1.Name = "Outstanding Balance";

			Actions["PrintAgedBalanceReport"].SetEnabled(!(graph1.Filter.Current.CustomerBalance == null));
			
			if (graph1.Filter.Current.CustomerBalance != null)
			{
				decimal rounded = decimal.Round((decimal)graph1.Filter.Current.CustomerBalance, 2);
				record1.Data = "USD " + rounded.ToString();
			}
			record1.Order = Order++;
			ret.Add(record1);
			SPSummary record2 = new SPSummary();
			record2.Name = "Overdue Balance";
			var graph2 = CreateInstance<SPStatementForCustomer>();
			graph2.Filter.Insert();
			graph2.Filter.Select();
			graph2.Filter.Current.FromDate = null;
			graph2.Filter.Current.TillDate = null;
			ARStatementForCustomer.DetailsResult detail = new ARStatementForCustomer.DetailsResult();
			foreach (var details in graph2.Details.Select())
			{
				detail = details;
			}
			Actions["PrintCustomerStatement"].SetEnabled(!(detail.OverdueBalance == null));
			if (detail.OverdueBalance != null)
			{
				decimal rounded1 = decimal.Round((decimal)detail.OverdueBalance, 2);
				record2.Data = "USD " + rounded1.ToString();
			}
			record2.Order = Order++;
			ret.Add(record2);

			SPSummary record3 = new SPSummary();
			record3.Name = "Open Support Cases";
			var graph3 = CreateInstance<SPCaseOpenInquiry>();
			int OpenCase = graph3.FilteredItems.Select().Count;
			record3.Data = OpenCase.ToString();
			record3.Order = Order++;
			ret.Add(record3);

			SPSummary record4 = new SPSummary();

			int pcxcasesummary = 0;
			foreach(CRCase cases in graph3.FilteredItems.Select())
			{
				if (cases.Status == "Pending Customer")
				{
					pcxcasesummary++;
				}
			}
			record4.Name = "Cases Pending Update";
			record4.Data = pcxcasesummary.ToString();
			record4.Order = Order++;
			ret.Add(record4);

			foreach (var summary in ret)
			{
				yield return summary;
			}
		}

		public virtual IEnumerable announcements()
		{
			List<CRAnnouncement> ret = new List<CRAnnouncement>();
			int number = 0;
			foreach (CRAnnouncement summaryAnnouncements in PXSelect<CRAnnouncement,
							Where<CRAnnouncement.isPortalVisible, Equal<True>>, 
							OrderBy<Desc<CRAnnouncement.publishedDateTime>>>.Select(this))
			{
				summaryAnnouncements.Order = number++;
				summaryAnnouncements.Smallbody = Tools.RemoveHeader(summaryAnnouncements.Body);
				if (summaryAnnouncements.Smallbody.Length > 255)
				{
					summaryAnnouncements.Smallbody = summaryAnnouncements.Smallbody.Remove(80);
					summaryAnnouncements.Smallbody = summaryAnnouncements.Smallbody + "...";
				}
				ret.Add(summaryAnnouncements);
			}

			foreach (var summaryAnnouncements in ret)
			{
				yield return summaryAnnouncements;
			}

		}
		#endregion

		#region Actions
		public PXAction<SPSummary> AnnouncementViewDetails;
		[PXUIField(DisplayName = "View Announcement")]
		[PXButton]
		public virtual void announcementViewDetails()
		{
			var row = Announcements.Current;
			if (row != null)
			{
				var graph = CreateInstance<CRCommunicationAnnouncementPreview>();
				graph.AnnouncementsDetails.Current.Body = "<html><head></head><body>";
				graph.AnnouncementsDetails.Current.Body = graph.AnnouncementsDetails.Current.Body + "<font size=\"4\">";
				graph.AnnouncementsDetails.Current.Body = graph.AnnouncementsDetails.Current.Body + Announcements.Current.Subject;
				graph.AnnouncementsDetails.Current.Body = graph.AnnouncementsDetails.Current.Body + "</font>";
				graph.AnnouncementsDetails.Current.Body = graph.AnnouncementsDetails.Current.Body + "<br/><br/>" + Tools.RemoveHeader(Announcements.Current.Body);
				graph.AnnouncementsDetails.Current.Body = graph.AnnouncementsDetails.Current.Body + "</body></html>";
				PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.Same);
			}
		}

		public PXAction<SPSummary> SummaryViewDetails;
		[PXUIField(DisplayName = "View Details")]
		[PXButton]
		public virtual void summaryViewDetails()
		{
			switch (CustomerSummary.Current.Name)
			{
				case "Outstanding Balance":
					var graph = CreateInstance<ARDocumentEnq>();
					graph.Filter.Select();
					PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.Same);
					break;

				case "Overdue Balance":
					var graph2 = CreateInstance<ARDocumentEnq>();
					graph2.Actions["ARAgedPastDueReport"].Press();
					PXRedirectHelper.TryRedirect(graph2, PXRedirectHelper.WindowMode.Same);
					break;
				
				/*var graph2 = CreateInstance<SPStatementForCustomer>();
					graph2.Filter.Insert();
					graph2.Filter.Select();
					graph2.Filter.Current.FromDate = null;
					graph2.Filter.Current.TillDate = null;
					ARStatementForCustomer.DetailsResult detail = new ARStatementForCustomer.DetailsResult();
					foreach (var details in graph2.Details.Select())
					{
						detail = details;
					}
					graph2.Filter.Current.FromDate = detail.StatementDate.Value.AddMonths(-6);
					graph2.Filter.Current.TillDate = detail.StatementDate;
					graph2.Filter.Select();
					PXRedirectHelper.TryRedirect(graph2, PXRedirectHelper.WindowMode.Same);
					break;*/

				case "Open Support Cases":
					var graph3 = CreateInstance<SPCaseOpenInquiry>();
					PXRedirectHelper.TryRedirect(graph3, PXRedirectHelper.WindowMode.Same);
					break;

				case "Cases Pending Update":
					var graph4 = CreateInstance<SPCaseOpenInquiry>();
					PXRedirectHelper.TryRedirect(graph4, PXRedirectHelper.WindowMode.Same);
					break;

				default:
					var graph5 = CreateInstance<SPCaseOpenInquiry>();
					PXRedirectHelper.TryRedirect(graph5, PXRedirectHelper.WindowMode.Same);
					break;
			}
		}

		public PXAction<SPSummary> NewCase;
		[PXUIField(DisplayName = "Enter New Support Case")]
		[PXButton]
		public virtual void newCase()
		{
			throw new PXRedirectToUrlException("~/Pages/SP/SP203000.aspx?CaseCD=null", PXBaseRedirectException.WindowMode.Same, "");
		}

		public PXAction<SPSummary> PrintCustomerStatement;
		[PXUIField(DisplayName = "Print Customer Statement")]
		[PXButton]
		public virtual void printCustomerStatement()
		{
			var graph2 = CreateInstance<SPStatementForCustomer>();
			graph2.Filter.Insert();
			graph2.Filter.Select();
			graph2.Filter.Current.FromDate = null;
			graph2.Filter.Current.TillDate = null;
			ARStatementForCustomer.DetailsResult detail = new ARStatementForCustomer.DetailsResult();
			foreach (var details in graph2.Details.Select())
			{
				detail = details;
			}
			graph2.Filter.Current.FromDate = detail.StatementDate.Value.AddMonths(-6);
			graph2.Filter.Current.TillDate = detail.StatementDate;
			graph2.Filter.Select();
			graph2.Details.Current = detail;
			graph2.Actions["PrintReport"].Press();
		}

		public PXAction<SPSummary> AccountSettings;
		[PXUIField(DisplayName = "Company Profile")]
		[PXButton]
		public virtual void accountSettings()
		{
			var graph = CreateInstance<BusinessAccountMaint>();
			PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.Same);
		}

		public PXAction<SPSummary> PrintAgedBalanceReport;
		[PXUIField(DisplayName = "Print Aged Balance Report")]
		[PXButton]
		public virtual void printAgedBalanceReport()
		{
			var graph2 = CreateInstance<ARDocumentEnq>();
			graph2.Actions["ARAgedPastDueReport"].Press();
		}

		public PXAction<SPSummary> ManageUsers;
		[PXUIField(DisplayName = "Contacts")]
		[PXButton]
		public virtual void manageUsers()
		{
			var graph = CreateInstance<SPContactProductInquiry>();
			PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.Same);
		}

		public PXAction<SPSummary> BrowseDocumentsHistory;
		[PXUIField(DisplayName = "Browse Documents History")]
		[PXButton]
		public virtual void browseDocumentsHistory()
		{
			var graph = CreateInstance<ARDocumentEnq>();
			graph.Filter.Select();
			PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.Same);
		}
		#endregion
	}
}
