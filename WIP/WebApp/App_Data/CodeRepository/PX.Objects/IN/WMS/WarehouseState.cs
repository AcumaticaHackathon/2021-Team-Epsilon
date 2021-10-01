using System;
using System.Collections.Generic;
using System.Linq;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.BarcodeProcessing;

namespace PX.Objects.IN
{
	public abstract class WarehouseState<TScanBasis> : EntityState<TScanBasis, INSite>
		where TScanBasis : PXGraphExtension, IBarcodeDrivenStateMachine
	{
		public const string Value = "SITE";
		public class value : BqlString.Constant<value> { public value() : base(WarehouseState<TScanBasis>.Value) { } }

		public override string Code => Value;
		protected override string StatePrompt => Msg.Prompt;

		protected abstract int? SiteID { get; set; }
		protected abstract bool UseDefaultWarehouse { get; }
		protected virtual int? DefaultSiteID => UserPreferenceExt.GetDefaultSite(Basis.Graph);
		protected override bool IsStateSkippable() => SiteID != null;

		protected override void OnTakingOver()
		{
			if (IsActive && !IsSkippable && UseDefaultWarehouse && DefaultSiteID != null && INSite.PK.Find(Basis.Graph, DefaultSiteID) is INSite defaultSite)
				Process(defaultSite.SiteCD);
		}

		protected override INSite GetByBarcode(string barcode) => INSite.UK.Find(Basis.Graph, barcode);
		protected override void ReportMissing(string barcode) => Basis.Reporter.Error(Msg.Missing, barcode);
		protected override void Apply(INSite site) => SiteID = site.SiteID;
		protected override void ClearState() => SiteID = null;
		protected override void ReportSuccess(INSite site) => Basis.Reporter.Info(Msg.Ready, site.SiteCD);

		[PXLocalizable]
		public abstract class Msg
		{
			public const string Prompt = "Scan the barcode of the warehouse.";
			public const string Ready = "The {0} warehouse is selected.";
			public const string Missing = "The {0} warehouse is not found.";
		}
	}
}