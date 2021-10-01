using PX.Payroll.Proxy;
using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Payroll.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WSTaxType = PX.Payroll.Data.PRTaxType;
using PX.Payroll;
using PX.Data.BQL;
using CsvHelper;
using System.IO;
using System.Text;

namespace PX.Objects.PR
{
	public class PRTaxMaintenance : PXGraph<PRTaxMaintenance>
	{
		public PRTaxMaintenance()
		{
			Taxes.AllowInsert = false;
			Taxes.AllowDelete = false;
			TaxAttributes.AllowInsert = false;
			TaxAttributes.AllowDelete = false;
			CompanyAttributes.AllowInsert = false;
			CompanyAttributes.AllowDelete = false;

			Employees.SetProcessDelegate(list => AssignEmployeeTaxes(list));
		}

		public override bool IsDirty
		{
			get
			{
				PXLongRunStatus status = PXLongOperation.GetStatus(this.UID);
				if (status == PXLongRunStatus.Completed || status == PXLongRunStatus.Aborted)
				{
					foreach (KeyValuePair<Type, PXCache> pair in Caches)
					{
						if (Views.Caches.Contains(pair.Key) && pair.Value.IsDirty)
						{
							return true;
						}
					}
				}
				return base.IsDirty;
			}
		}

		#region Views
		// This dummy view needs to be declared above any view that contains PREmployee so that the Vendor cache, not the cache from derived
		// type PREmployee, is used in Vendor selectors.
		[PXHidden]
		[PXCopyPasteHiddenView]
		public PXSelect<Vendor> DummyVendor;

		public PXFilter<PRCompanyTaxAttributeFilter> Filter;

		public SelectFrom<PRTaxCode>.View Taxes;
		public SelectFrom<PRTaxCode>
			.Where<PRTaxCode.taxID.IsEqual<PRTaxCode.taxID.FromCurrent>>.View CurrentTax;

		public PRAttributeDefinitionSelect<
			PRTaxCodeAttribute,
			SelectFrom<PRTaxCodeAttribute>
				.Where<PRTaxCodeAttribute.taxID.IsEqual<PRTaxCode.taxID.FromCurrent>>
				.OrderBy<PRTaxCodeAttribute.sortOrder.Asc>,
			PRTaxCode,
			Payroll.Data.PRTax,
			Payroll.TaxTypeAttribute> TaxAttributes;

		public PREmployeeAttributeDefinitionSelect<
			PRCompanyTaxAttribute,
			SelectFrom<PRCompanyTaxAttribute>
				.Where<PRCompanyTaxAttributeFilter.filterStates.FromCurrent.IsNotEqual<True>
					.Or<PRCompanyTaxAttribute.state.IsEqual<LocationConstants.Federal>>
					.Or<PRCompanyTaxAttribute.taxesInState.IsNotNull>>
				.OrderBy<PRCompanyTaxAttribute.state.Asc, PRCompanyTaxAttribute.sortOrder.Asc>,
			PRCompanyTaxAttributeFilter,
			PRCompanyTaxAttributeFilter.filterStates,
			PRTaxCode,
			PRTaxCode.taxState> CompanyAttributes;
		public SelectFrom<PRCompanyTaxAttribute>
			.LeftJoin<PRTaxCode>.On<PRTaxCode.taxState.IsEqual<PRCompanyTaxAttribute.state>>
			.Where<PRCompanyTaxAttributeFilter.filterStates.FromCurrent.IsNotEqual<True>
				.Or<PRCompanyTaxAttribute.state.IsEqual<LocationConstants.Federal>>
				.Or<PRTaxCode.taxID.IsNotNull>>.View FilteredCompanyAttributes;

		public SelectFrom<Address>
			.LeftJoin<PRLocation>.On<PRLocation.addressID.IsEqual<Address.addressID>>
			.LeftJoin<PREmployee>.On<PREmployee.defAddressID.IsEqual<Address.addressID>>
			.Where<PRLocation.isActive.IsEqual<True>
				.Or<PREmployee.activeInPayroll.IsEqual<True>>>.View Addresses;

		public InvokablePXProcessing<PREmployee> Employees;
		#endregion Views

		#region Data view delegates
		public virtual IEnumerable taxes()
		{
			UpdateTaxesCustomInfo customInfo = PXLongOperation.GetCustomInfo(this.UID) as UpdateTaxesCustomInfo;
			bool taxesCached = Taxes.Cache.Cached.Any_();
			List<object> taxList = new PXView(this, false, Taxes.View.BqlSelect).SelectMulti();
			if (!taxesCached || customInfo?.ValidateTaxesNeeded == true)
			{
				ValidateTaxAttributes(taxList.Select(x => (PRTaxCode)x).ToList());
				if (customInfo != null)
				{
					customInfo.ValidateTaxesNeeded = false;
				}
			}

			if (customInfo?.NewTaxes.Any() == true)
			{
				customInfo.NewTaxes.ForEach(x =>
				{
					AdjustTaxCDForDuplicate(x);
					x = Taxes.Insert(x);
					taxList.Add(x);
				});
				ValidateTaxAttributes(Taxes.Cache.Inserted.Cast<PRTaxCode>().ToList());
				customInfo.NewTaxes.Clear();
			}

			if (customInfo?.UpdatedTaxes.Any() == true)
			{
				customInfo.UpdatedTaxes.ForEach(x => Taxes.Update(x));
				ValidateTaxAttributes(Taxes.Cache.Updated.Cast<PRTaxCode>().ToList());
				customInfo.UpdatedTaxes.Clear();
			}

			return taxList;
		}
		#endregion

		#region Actions
		public PXSave<PRCompanyTaxAttributeFilter> Save;
		public PXCancel<PRCompanyTaxAttributeFilter> Cancel;

		public PXAction<PRCompanyTaxAttributeFilter> UpdateTaxes;
		[PXUIField(DisplayName = "Update Taxes", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		public virtual IEnumerable updateTaxes(PXAdapter adapter)
		{
			PXLongOperation.StartOperation(this, delegate ()
			{
				PXPayrollAssemblyScope.UpdateTaxDefinition();
				CreateTaxesForAllLocations(out List<PRTaxCode> newTaxes, out List<PRTaxCode> updatedTaxes);

				BackgroundTaxDataUpdate backgroundUpdateGraph = CreateInstance<BackgroundTaxDataUpdate>();
				backgroundUpdateGraph.UpdateBackgroundData();
				backgroundUpdateGraph.Actions.PressSave();

				PXLongOperation.SetCustomInfo(new UpdateTaxesCustomInfo(newTaxes, updatedTaxes));
			});

			return adapter.Get();
		}

		public PXAction<PRCompanyTaxAttributeFilter> AssignTaxesToEmployees;
		[PXUIField(DisplayName = "Assign Taxes to Employees", MapEnableRights = PXCacheRights.Delete, MapViewRights = PXCacheRights.Delete)]
		[PXProcessButton]
		public virtual IEnumerable assignTaxesToEmployees(PXAdapter adapter)
		{
			PXLongOperation.ClearStatus(this.UID);
			return Employees.Invoke(adapter);
		}

		public PXAction<PRTaxCode> ViewTaxDetails;
		[PXUIField(DisplayName = "Tax Details", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable viewTaxDetails(PXAdapter adapter)
		{
			CurrentTax.AskExt();
			return adapter.Get();
		}
		#endregion Actions

		#region Events
		public virtual void _(Events.RowSelected<PRCompanyTaxAttributeFilter> e)
		{
			AssignTaxesToEmployees.SetEnabled(!Taxes.Cache.Inserted.Any_() && !TaxAttributes.Cache.IsDirty && !CompanyAttributes.Cache.IsDirty);
		}

		public virtual void _(Events.RowSelected<PRTaxCode> e)
		{
			if (e.Row == null)
			{
				return;
			}

			SetTaxCodeError(e.Cache, e.Row);
		}

		public virtual void _(Events.RowPersisting<PRTaxCodeAttribute> e)
		{
			if (e.Row.ErrorLevel == (int?)PXErrorLevel.RowError)
			{
				e.Cache.RaiseExceptionHandling<PRTaxCodeAttribute.value>(
					e.Row,
					e.Row.Value,
					new PXSetPropertyException(Messages.ValueBlankAndRequired, PXErrorLevel.RowError));
			}
		}

		public virtual void _(Events.RowPersisting<PRCompanyTaxAttribute> e)
		{
			if (e.Row.ErrorLevel == (int?)PXErrorLevel.RowError)
			{
				e.Cache.RaiseExceptionHandling<PRCompanyTaxAttribute.value>(
					e.Row,
					e.Row.Value,
					new PXSetPropertyException(Messages.ValueBlankAndRequiredAndNotOverridable, PXErrorLevel.RowError));
			}
		}
		#endregion Events

		#region Helpers
		protected virtual void CreateTaxesForAllLocations(out List<PRTaxCode> newTaxes, out List<PRTaxCode> updatedTaxes)
		{
			newTaxes = new List<PRTaxCode>();
			updatedTaxes = new List<PRTaxCode>();

			var payrollService = new PayrollTaxClient();
			List<Address> addresses = Addresses.Select().FirstTableItems.ToList();
			addresses = TaxLocationHelpers.GetUpdatedAddressLocationCodes(addresses, payrollService);

			string includeRailroadTaxesSettingName = GetIncludeRailroadTaxesSettingName();
			bool includeRailroadTaxes =
				SelectFrom<PRCompanyTaxAttribute>
					.Where<PRCompanyTaxAttribute.settingName.IsEqual<P.AsString>>.View
					.Select(this, includeRailroadTaxesSettingName).FirstTableItems
					.Any(x => bool.TryParse(x.Value, out bool boolValue) && boolValue)
				|| SelectFrom<PREmployeeAttribute>
					.InnerJoin<PREmployee>.On<PREmployee.bAccountID.IsEqual<PREmployeeAttribute.bAccountID>>
					.Where<PREmployeeAttribute.settingName.IsEqual<P.AsString>
						.And<PREmployee.activeInPayroll.IsEqual<True>>>.View
					.Select(this, includeRailroadTaxesSettingName).FirstTableItems
					.Any(x => bool.TryParse(x.Value, out bool boolValue) && boolValue);
			List<PRTaxCode> existingTaxes = Taxes.Select().FirstTableItems.ToList();
			List<WSTaxType> webServiceTaxes = payrollService.GetAllLocationTaxTypes(addresses, includeRailroadTaxes)
				.Distinct(new TaxTypeEqualityComparer())
				.ToList();
			foreach (WSTaxType taxType in webServiceTaxes)
			{
				PRTaxCode existingTax = existingTaxes.FirstOrDefault(y => y.TaxUniqueCode == taxType.UniqueTaxID);
				if (existingTax == null)
				{
					if (taxType.IsImplemented)
					{
						newTaxes.Add(CreateTax(taxType));
					}
					else
					{
						PXTrace.WriteWarning(Messages.TaxTypeIsNotImplemented, taxType.TaxID);
					} 
				}
				else
				{
					bool updated = false;
					if (existingTax.TypeName != taxType.TypeName)
				{
					existingTax.TypeName = taxType.TypeName;
						updated = true;
					}
					if (existingTax.JurisdictionLevel != TaxJurisdiction.GetTaxJurisdiction(taxType.TaxJurisdiction))
					{
						existingTax.JurisdictionLevel = TaxJurisdiction.GetTaxJurisdiction(taxType.TaxJurisdiction);
						updated = true;
					}
					if (existingTax.TaxCategory != TaxCategory.GetTaxCategory(taxType.TaxCategory))
					{
						existingTax.TaxCategory = TaxCategory.GetTaxCategory(taxType.TaxCategory);
						updated = true;
					}

					if (updated)
					{
					updatedTaxes.Add(existingTax);
				}
			}
		}
		}

		protected virtual PRTaxCode CreateTax(WSTaxType taxType)
		{
			string taxCD = taxType.TaxID.Replace('_', ' ');
			string taxJurisdiction = TaxJurisdiction.GetTaxJurisdiction(taxType.TaxJurisdiction);
			string stateAbbr = taxJurisdiction == TaxJurisdiction.Federal ? LocationConstants.FederalStateCode : PRState.FromLocationCode(int.Parse(taxType.LocationCode.Split('-')[0])).Abbr;
			if (taxJurisdiction != TaxJurisdiction.Federal)
			{
				taxCD = string.Join(" ", stateAbbr, taxCD);
				if (taxJurisdiction == TaxJurisdiction.Local)
				{
					string localTaxId = taxType.LocationCode.Split('-')[1];
					if (localTaxId == "000")
					{
						localTaxId = taxType.SchoolDistrictCode;
					}
					taxCD = string.Join(" ", taxCD, localTaxId);
				}
				else if (taxJurisdiction == TaxJurisdiction.Municipal)
				{
					taxCD = string.Join(" ", taxCD, taxType.LocationCode.Split('-')[2]);
				}
				else if (taxJurisdiction == TaxJurisdiction.SchoolDistrict)
				{
					taxCD = string.Join(" ", taxCD, taxType.SchoolDistrictCode);
				}
			}

			PRTaxCode newTaxCode = new PRTaxCode();
			int taxCDFieldLength = Taxes.Cache.GetAttributesOfType<PXDBStringAttribute>(newTaxCode, nameof(PRTaxCode.taxCD)).First().Length;
			newTaxCode.TaxCD = taxCD.Length > taxCDFieldLength ? taxCD.Substring(0, taxCDFieldLength) : taxCD;

			newTaxCode.TaxCategory = TaxCategory.GetTaxCategory(taxType.TaxCategory);
			newTaxCode.TypeName = taxType.TypeName;
			newTaxCode.TaxUniqueCode = taxType.UniqueTaxID;
			if (taxJurisdiction != TaxJurisdiction.Federal)
			{
				newTaxCode.TaxState = stateAbbr;
			}

			int descriptionLength = Taxes.Cache.GetAttributesOfType<PXDBStringAttribute>(newTaxCode, nameof(PRTaxCode.description)).First().Length;
			newTaxCode.Description = taxType.Description.Length > descriptionLength ? taxType.Description.Substring(0, descriptionLength) : taxType.Description;
			return newTaxCode;
		}

		protected virtual void ValidateTaxAttributes(List<PRTaxCode> taxes)
		{
			foreach (PRTaxCode taxCodeWithError in GetTaxAttributeErrors(taxes).Where(x => x.ErrorLevel != null && x.ErrorLevel != (int?)PXErrorLevel.Undefined))
			{
				SetTaxCodeError(Taxes.Cache, taxCodeWithError);
			}
		}

		protected virtual void SetTaxCodeError(PXCache cache, PRTaxCode taxCode)
		{
			(string previousErrorMsg, PXErrorLevel previousErrorLevel) = PXUIFieldAttribute.GetErrorWithLevel<PRTaxCode.taxCD>(cache, taxCode);
			bool previousErrorIsRelated = previousErrorMsg == Messages.ValueBlankAndRequired || previousErrorMsg == Messages.NewTaxSetting;

			if (taxCode.ErrorLevel == (int?)PXErrorLevel.RowError)
			{
				PXUIFieldAttribute.SetError(cache, taxCode, nameof(taxCode.TaxCD), Messages.ValueBlankAndRequired, taxCode.TaxCD, false, PXErrorLevel.RowError);
			}
			else if ((taxCode.ErrorLevel == (int?)PXErrorLevel.RowWarning || cache.GetStatus(taxCode) == PXEntryStatus.Inserted) &&
				(previousErrorLevel != PXErrorLevel.RowError || previousErrorIsRelated))
			{
				PXUIFieldAttribute.SetError(cache, taxCode, nameof(taxCode.TaxCD), Messages.NewTaxSetting, taxCode.TaxCD, false, PXErrorLevel.RowWarning);
			}
			else if (taxCode.ErrorLevel == (int?)PXErrorLevel.Undefined && previousErrorIsRelated)
			{
				PXUIFieldAttribute.SetError(cache, taxCode, nameof(taxCode.TaxCD), "", taxCode.TaxCD, false, PXErrorLevel.Undefined);
			}
		}

		protected virtual IEnumerable<PRTaxCode> GetTaxAttributeErrors(List<PRTaxCode> taxes)
		{
			PRTaxCode restoreCurrent = Taxes.Current;
			try
			{
				foreach (PRTaxCode taxCode in taxes)
				{
					Taxes.Current = taxCode;
					foreach (PRTaxCodeAttribute taxAttribute in TaxAttributes.Select().FirstTableItems)
					{
						// Raising FieldSelecting on PRTaxCodeAttribute will set error on the attribute and propagate
						// the error/warning to the tax code
						object value = taxAttribute.Value;
						TaxAttributes.Cache.RaiseFieldSelecting<PRTaxCodeAttribute.value>(taxAttribute, ref value, false);
					}

					yield return taxCode;
				}
			}
			finally
			{
				Taxes.Current = restoreCurrent;
			}
		}

		protected static void AssignEmployeeTaxes(List<PREmployee> list)
		{
			PREmployeePayrollSettingsMaint employeeGraph = PXGraph.CreateInstance<PREmployeePayrollSettingsMaint>();
			foreach (PREmployee employee in list)
			{
				try
				{
					PXProcessing.SetCurrentItem(employee);
					employeeGraph.Clear();
					employeeGraph.CurrentPayrollEmployee.Current = employee;
					employeeGraph.ImportTaxesProc(true);
					employeeGraph.Persist();
					PXProcessing.SetProcessed();
				}
				catch
				{
					PXProcessing.SetError(list.IndexOf(employee), Messages.CantAssignTaxesToEmployee);
				}
			}
		}

		protected virtual void AdjustTaxCDForDuplicate(PRTaxCode row)
		{
			int similarTaxCodes = SelectFrom<PRTaxCode>.View.Select(this).FirstTableItems.Count(x => x.TaxCD.StartsWith(row.TaxCD));
			if (similarTaxCodes > 0)
			{
				int taxCDFieldLength = Taxes.Cache.GetAttributesOfType<PXDBStringAttribute>(row, nameof(PRTaxCode.taxCD)).First().Length;
				row.TaxCD = row.TaxCD.Length >= taxCDFieldLength ? row.TaxCD.Substring(0, taxCDFieldLength - 1) : row.TaxCD;
				row.TaxCD = $"{row.TaxCD}{(char)('a' + similarTaxCodes)}";
			}
		}

		public static string GetIncludeRailroadTaxesSettingName()
		{
			var metaAttr = new EmployeeLocationSettingsAttribute(LocationConstants.FederalStateCode, string.Empty);
			return MetaDynamicSetting<EmployeeLocationSettingsAttribute>.GetUniqueSettingName(metaAttr, WebserviceContants.IncludeRailroadTaxesSetting);
		}
		#endregion Helpers

		#region Helper classes
		private class TaxTypeEqualityComparer : IEqualityComparer<WSTaxType>
		{
			public bool Equals(WSTaxType x, WSTaxType y)
			{
				return x.UniqueTaxID == y.UniqueTaxID;
			}

			public int GetHashCode(WSTaxType obj)
			{
				return obj.UniqueTaxID.GetHashCode();
			}
		}

		private class UpdateTaxesCustomInfo
		{
			public List<PRTaxCode> NewTaxes;
			public List<PRTaxCode> UpdatedTaxes;
			public bool ValidateTaxesNeeded = true;

			public UpdateTaxesCustomInfo(List<PRTaxCode> newTaxes, List<PRTaxCode> updatedTaxes)
			{
				NewTaxes = newTaxes;
				UpdatedTaxes = updatedTaxes;
			}
		}
		
		public class InvokablePXProcessing<TTable> : PXProcessing<TTable>
			where TTable : class, IBqlTable, new()
		{
			public InvokablePXProcessing(PXGraph graph) : base(graph) { }

			public IEnumerable Invoke(PXAdapter adapter)
			{
				return ProcessAll(adapter);
			}
		}

		[PXHidden]
		protected class BackgroundTaxDataUpdate : PXGraph<BackgroundTaxDataUpdate>
		{

			public SelectFrom<PRTaxUpdateHistory>.View TaxUpdateHistory;
			public SelectFrom<PRTaxSettingAdditionalInformation>.View TaxSettingAdditionalInformation;

			public void UpdateBackgroundData()
			{
				PayrollUpdateClient updateClient = new PayrollUpdateClient();
				PRTaxUpdateHistory updateHistory = TaxUpdateHistory.SelectSingle() ?? new PRTaxUpdateHistory();
				updateHistory.LastCheckTime = DateTime.UtcNow;
				updateHistory.ServerTaxDefinitionTimestamp = updateClient.GetTaxDefinitionTimestamp();
				updateHistory.LastUpdateTime = DateTime.UtcNow;
				TaxUpdateHistory.Update(updateHistory);
				
				UpdateCompanyTaxAttributeDescriptions(updateClient);
			}

			protected virtual void UpdateCompanyTaxAttributeDescriptions(PayrollUpdateClient updateClient)
			{
				Dictionary<TaxSettingAdditionalInformationKey, TaxSettingDescription> additionalDescriptions;
				using (CsvReader reader = new CsvReader(new StreamReader(new MemoryStream(updateClient.GetTaxSettingsAdditionalDescription()), Encoding.UTF8)))
				{
					additionalDescriptions = reader.GetRecords<TaxSettingDescription>().ToDictionary(k => new TaxSettingAdditionalInformationKey(k), v => v);
				}

				List<PRTaxSettingAdditionalInformation> settingAdditionalInformation = TaxSettingAdditionalInformation.Select().FirstTableItems.ToList();
				foreach (PRTaxSettingAdditionalInformation setting in settingAdditionalInformation)
				{
					if (additionalDescriptions.TryGetValue(new TaxSettingAdditionalInformationKey(setting), out TaxSettingDescription definition))
					{
						setting.AdditionalInformation = definition.AdditionalInformation;
						setting.UsedForSymmetry = definition.UsedForSymmetry;
						setting.FormBox = definition.FormBox;
						TaxSettingAdditionalInformation.Update(setting);
					}
				}

				HashSet<TaxSettingAdditionalInformationKey> settingsDefinedInDB = settingAdditionalInformation.Select(x => new TaxSettingAdditionalInformationKey(x)).ToHashSet();
				foreach (TaxSettingDescription newDefinition in additionalDescriptions.Values.Where(x => !settingsDefinedInDB.Contains(new TaxSettingAdditionalInformationKey(x))))
				{
					TaxSettingAdditionalInformation.Insert(new PRTaxSettingAdditionalInformation()
					{
						TypeName = newDefinition.TypeName,
						SettingName = newDefinition.SettingName,
						AdditionalInformation = newDefinition.AdditionalInformation,
						UsedForSymmetry = newDefinition.UsedForSymmetry,
						FormBox = newDefinition.FormBox,
						State = string.IsNullOrEmpty(newDefinition.State) ? null : newDefinition.State,
						CountryID = newDefinition.CountryID
					});
				}

				TaxSettingAdditionalInformation.Cache.Persist(PXDBOperation.Insert);
				TaxSettingAdditionalInformation.Cache.Persist(PXDBOperation.Update);
			}
		}
		#endregion Helper classes

		#region Obsolete
		[Obsolete]
		[PXHidden]
		[PXCopyPasteHiddenView]
		public PXFilter<FakeDac> FakeView;
		[Obsolete]
		[PXHidden]
		[PXCopyPasteHiddenView]
		public PXFilter<PRCompanyTaxAttributeFilter> CompanyAttributeFilter;
		[PXHidden]
		[Obsolete]
		public class FakeDac : IBqlTable { }
		[Obsolete]
		public virtual void _(Events.RowSelected<FakeDac> e) { }
		#endregion Obsolete
	}

	[PXHidden]
	[Serializable]
	public class PRCompanyTaxAttributeFilter : IBqlTable
	{
		#region FilterStates
		[PXBool]
		[PXUnboundDefault(false)]
		[PXUIField(DisplayName = "Show Attributes Only for States That Have Tax Codes Set Up")]
		public bool? FilterStates { get; set; }
		public abstract class filterStates : PX.Data.BQL.BqlBool.Field<filterStates> { }
		#endregion
	}
}
