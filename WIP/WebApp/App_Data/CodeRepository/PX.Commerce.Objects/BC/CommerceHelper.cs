using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CR;
using System;
using System.Collections.Generic;
using System.Linq;
using Location = PX.Objects.CR.Standalone.Location;

namespace PX.Commerce.Objects
{
	public class CommerceHelper : PXGraph<CommerceHelper>
	{
		protected IProcessor _processor;
		public PXSelectJoin<PX.Objects.AR.Customer, LeftJoin<PX.Objects.CR.Contact, On<PX.Objects.AR.Customer.defContactID, Equal<PX.Objects.CR.Contact.contactID>>>,
						Where<PX.Objects.CR.Contact.eMail, Equal<Required<PX.Objects.CR.Contact.eMail>>>> CustomerByEmail;
		public PXSelectJoin<PX.Objects.AR.Customer, LeftJoin<PX.Objects.CR.Contact, On<PX.Objects.AR.Customer.defContactID, Equal<PX.Objects.CR.Contact.contactID>>>,
						Where<PX.Objects.CR.Contact.phone1, Equal<Required<PX.Objects.CR.Contact.phone1>>, Or<PX.Objects.CR.Contact.phone2, Equal<Required<PX.Objects.CR.Contact.phone2>>>>> CustomerByPhone;
		public PXSelect<PX.Objects.AR.ARRegister, Where<PX.Objects.AR.ARRegister.externalRef, Equal<Required<PX.Objects.AR.ARRegister.externalRef>>>> PaymentByExternalRef;
		public PXSelect<PX.Objects.SO.SOOrder, Where<PX.Objects.SO.SOOrder.orderType, IsNotNull, And<PX.Objects.SO.SOOrder.orderType, In<Required<PX.Objects.SO.SOOrder.orderType>>, And<PX.Objects.SO.SOOrder.customerRefNbr, Equal<Required<PX.Objects.SO.SOOrder.customerRefNbr>>>>>> OrderByTypesAndCustomerRefNbr;
		public IProcessor Processor { get => _processor; }
		public string SOTypeImportMapping;

		protected List<BCPaymentMethods> _paymentMethods;
		public List<BCPaymentMethods> PaymentMethods
		{
			get
			{
				if (_paymentMethods == null)
					return PXSelectReadonly<BCPaymentMethods,
							Where<BCPaymentMethods.bindingID, Equal<Required<BCPaymentMethods.bindingID>>>>
							.Select(this, _processor.Operation.Binding).Select(x => x.GetItem<BCPaymentMethods>()).ToList();
				return _paymentMethods;
			}
		}

		public void Initialize(IProcessor processor)
		{
			_processor = processor;
		}

		#region Location helper methods

		public virtual void DeactivateLocation(CustomerMaint graph, int? previousDefault, CustomerLocationMaint locGraph, BCSyncStatus value)
		{
			locGraph.LocationCurrent.Current = PXSelect<PX.Objects.CR.Location, Where<PX.Objects.CR.Location.bAccountID, Equal<Required<PX.Objects.CR.Location.bAccountID>>, And<PX.Objects.CR.Location.noteID, Equal<Required<PX.Objects.CR.Location.noteID>>>>>.Select(locGraph, graph.BAccount.Current.BAccountID, value.LocalID);
			if (locGraph.LocationCurrent.Current != null)
			{
				if (previousDefault == locGraph.LocationCurrent.Current.LocationID)// only if default location is set to inactive 
					locGraph.GetExtension<BCLocationMaintExt>().ClearCache = true;
				PXAutomation.ApplyStep(locGraph, locGraph.LocationCurrent.Current, false);
				locGraph.Actions[nameof(LocationMaint.Deactivate)].Press();
				locGraph.GetExtension<BCLocationMaintExt>().ClearCache = false;
			}
		}

		public virtual int? SetDefaultLocation(Guid? defaultLocalId, List<Location> locations, CustomerMaint graph, ref bool updated)
		{
			var nextdefault = locations.FirstOrDefault(x => x.NoteID == defaultLocalId);
			var defLocationExt = graph.GetExtension<CustomerMaint.DefLocationExt>();
			var locationDetails = graph.GetExtension<CustomerMaint.LocationDetailsExt>();

			if (nextdefault == null)
			{
				locationDetails.Locations.Cache.Clear();
				locationDetails.Locations.Cache.ClearQueryCache();
				nextdefault = locationDetails.Locations.Select().RowCast<Location>()?.ToList()?.FirstOrDefault(x => x.NoteID == defaultLocalId);
			}
			var previousDefault = graph.BAccount.Current.DefLocationID;
			if (graph.BAccount.Current.DefLocationID != nextdefault?.LocationID)//if mapped default and deflocation are not in sync
			{
				defLocationExt.DefLocation.Current = nextdefault;
				if (defLocationExt.DefLocation.Current?.IsActive == false)
				{
					if (_processor.GetEntity(_processor.Operation.EntityType).PrimarySystem == BCSyncSystemAttribute.External)
					{
						var locGraph = PXGraph.CreateInstance<PX.Objects.AR.CustomerLocationMaint>();

						locGraph.LocationCurrent.Current = locGraph.Location.Select(graph.BAccount.Current.BAccountID).FirstOrDefault(x => x.GetItem<PX.Objects.CR.Location>().NoteID == defLocationExt.DefLocation.Current.NoteID);
						PXAutomation.ApplyStep(locGraph, locGraph.LocationCurrent.Current, false);
						locGraph.Actions[nameof(LocationMaint.Activate)].Press();
						defLocationExt.DefLocation.Cache.Clear();
						defLocationExt.DefLocation.Cache.ClearQueryCache();
						defLocationExt.DefLocation.Current  = locationDetails.Locations.Select().RowCast<Location>()?.ToList()?.FirstOrDefault(x => x.NoteID == defaultLocalId);
					}
				}
				updated = true;
				defLocationExt.SetDefaultLocation.Press();
				graph.Actions.PressSave();
			}
			return previousDefault;
		}

		public virtual bool CompareStrings(string value1, string value2)
		{
			return string.Equals(value1?.Trim() ?? string.Empty, value2?.Trim() ?? string.Empty, StringComparison.InvariantCultureIgnoreCase);
		}

		public virtual DateTime? GetUpdatedDate(string customerID, DateTime? date)
		{
			List<PXDataField> fields = new List<PXDataField>();
			fields.Add(new PXDataField(nameof(BAccount.lastModifiedDateTime)));
			fields.Add(new PXDataFieldValue(nameof(BAccount.acctCD), customerID));
			using (PXDataRecord rec = PXDatabase.SelectSingle(typeof(BAccount), fields.ToArray()))
			{
				if (rec != null)
				{
					date = rec.GetDateTime(0);
					if (date != null)
					{
						date = PXTimeZoneInfo.ConvertTimeFromUtc(date.Value, LocaleInfo.GetTimeZone());

					}
				}
			}

			return date;
		}
		#endregion

		#region Taxes
		public virtual void ValidateTaxes(int? syncID, SalesOrder impl, SalesOrder local)
		{
			if (impl != null && (_processor.GetBindingExt<BCBindingExt>().TaxSynchronization == true) && (local.IsTaxValid?.Value == true))
			{
				String receivedTaxes = String.Join("; ", impl.TaxDetails?.Select(x => String.Join("=", x.TaxID?.Value, x.TaxAmount?.Value)).ToArray() ?? new String[] { BCConstants.None });
				_processor.LogInfo(_processor.Operation.LogScope(syncID), BCMessages.LogTaxesOnOrderReceived,
					impl.OrderNbr?.Value,
					impl.FinancialSettings?.CustomerTaxZone?.Value ?? BCConstants.None,
					String.IsNullOrEmpty(receivedTaxes) ? BCConstants.None : receivedTaxes);

				List<TaxDetail> sentTaxesToValidate = local?.TaxDetails?.ToList() ?? new List<TaxDetail>();
				List<TaxDetail> receivedTaxesToValidate = impl.TaxDetails?.ToList() ?? new List<TaxDetail>();
				//Validate Tax Zone
				if (sentTaxesToValidate.Count > 0 && impl.FinancialSettings.CustomerTaxZone.Value == null)
				{
					throw new PXException(BCObjectsMessages.CannotFindTaxZone,
						String.Join(", ", sentTaxesToValidate.Select(x => x.TaxID?.Value).Where(x => x != null).ToArray() ?? new String[] { BCConstants.None }));
				}
				//Validate tax codes and amounts
				List<TaxDetail> invalidSentTaxes = new List<TaxDetail>();
				foreach (TaxDetail sent in sentTaxesToValidate)
				{
					TaxDetail received = receivedTaxesToValidate.FirstOrDefault(x => String.Equals(x.TaxID?.Value, sent.TaxID?.Value, StringComparison.InvariantCultureIgnoreCase));
					if (received == null)
						_processor.LogInfo(_processor.Operation.LogScope(syncID), BCMessages.LogTaxesNotApplied,
						impl.OrderNbr?.Value,
						sent.TaxID?.Value);
					// This is the line to filter out the incoming taxes that has 0 value, thus if settings in AC are correct they wont be created as lines on SO
					if ((received == null && sent.TaxAmount.Value != 0)
						|| (received != null && !EqualWithRounding(sent.TaxAmount?.Value, received.TaxAmount?.Value)))
					{
						invalidSentTaxes.Add(sent);
					}

					if (received != null) receivedTaxesToValidate.Remove(received);
				}
				if (invalidSentTaxes.Count > 0)
				{
					throw new PXException(BCObjectsMessages.CannotFindMatchingTaxExt,
						String.Join(",", invalidSentTaxes.Select(x => x.TaxID?.Value)),
						impl.FinancialSettings?.CustomerTaxZone?.Value ?? BCConstants.None);
				}
				List<TaxDetail> invalidReceivedTaxes = receivedTaxesToValidate.Where(x => (x.TaxAmount?.Value ?? 0m) == 0m && (x.TaxableAmount?.Value ?? 0m) == 0m).ToList();
				if (invalidReceivedTaxes.Count > 0)
				{
					throw new PXException(BCObjectsMessages.CannotFindMatchingTaxAcu,
						String.Join(",", invalidReceivedTaxes.Select(x => x.TaxID?.Value)),
						impl.FinancialSettings?.CustomerTaxZone?.Value ?? BCConstants.None);
				}
			}
		}

		public virtual void LogTaxDetails(int? syncID, SalesOrder order)
		{
			//Logging for taxes
			if ((_processor.GetBindingExt<BCBindingExt>().TaxSynchronization == true) && (order.IsTaxValid?.Value == true))
			{
				String sentTaxes = String.Join("; ", order.TaxDetails?.Select(x => String.Join("=", x.TaxID?.Value, x.TaxAmount?.Value)).ToArray() ?? new String[] { BCConstants.None });
				_processor.LogInfo(_processor.Operation.LogScope(syncID), BCMessages.LogTaxesOnOrderSent,
					order.OrderNbr?.Value ?? BCConstants.None,
					order.FinancialSettings?.CustomerTaxZone?.Value ?? BCConstants.None,
					String.IsNullOrEmpty(sentTaxes) ? BCConstants.None : sentTaxes);
			}
		}

		public virtual string TrimAutomaticTaxNameForAvalara(string mappedTaxName)
		{
			return mappedTaxName.Split(new string[] { " - " }, StringSplitOptions.None).FirstOrDefault() ?? mappedTaxName;
		}
		#endregion

		#region Utilities
		public virtual bool EqualWithRounding(decimal? sent, decimal? received)
		{
			if (sent.HasValue && received.HasValue)
			{
				int countSent = BitConverter.GetBytes(decimal.GetBits(sent.Value)[3])[2];
				int countReceived = BitConverter.GetBytes(decimal.GetBits(received.Value)[3])[2];
				int precision = countSent < countReceived ? countSent : countReceived;

				return PX.Objects.CM.PXCurrencyAttribute.PXCurrencyHelper.Round(sent.Value, precision) == PX.Objects.CM.PXCurrencyAttribute.PXCurrencyHelper.Round(received.Value, precision);
			}
			return false;
		}

		public virtual void GetExistingRefundPayment(Payment refundPayment, string docType, string reference)
		{
			ARPayment existinCRPayment = null;
			switch (docType)
			{
				case ARPaymentType.VoidPayment:
					{
						existinCRPayment = PXSelect<PX.Objects.AR.ARPayment,
												   Where<ARPayment.docType, Equal<Required<ARPayment.docType>>,
												   And<ARPayment.refNbr, Equal<Required<ARPayment.refNbr>>>>>.Select(this, ARPaymentType.VoidPayment, reference);
						if (existinCRPayment != null)
						{
							refundPayment.NoteID = existinCRPayment.NoteID.ValueField();
						}
						break;
					}
				case ARPaymentType.Refund:
					{
						existinCRPayment = PXSelect<PX.Objects.AR.ARPayment,
						   Where<ARPayment.docType, Equal<Required<ARPayment.docType>>,
						   And<ARRegister.externalRef, Equal<Required<ARRegister.externalRef>>>>>.Select(this, docType, reference);
						if (existinCRPayment != null)
						{
							refundPayment.NoteID = existinCRPayment.NoteID.ValueField();
						}
						else// if cannot find wit external ref search with transaction number
						{
							foreach (var crPayment in PXSelectJoin<CCProcTran, InnerJoin<ARPayment, On<CCProcTran.docType, Equal<ARPayment.docType>, And<CCProcTran.refNbr, Equal<ARPayment.refNbr>>>>,
									Where<CCProcTran.docType, Equal<Required<CCProcTran.docType>>,
									And<CCProcTran.tranType, Equal<CCTranTypeCode.credit>,
									And<CCProcTran.pCTranNumber, Equal<Required<CCProcTran.pCTranNumber>>>>>>.Select(this, docType, reference))
							{
								refundPayment.NoteID = crPayment?.GetItem<ARPayment>()?.NoteID?.ValueField();
								break;
							}
						}
						break;
					}
			}
		}
        #endregion

        #region UserMappings
		public virtual void TryGetCustomOrderTypeMappings(ref List<string> orderTypes)
		{
			if (SOTypeImportMapping == null)
			{
				var allMappings = PXSelect<BCEntityImportMapping,
				Where<BCEntityImportMapping.connectorType, Equal<Required<BCEntity.connectorType>>,
				And<BCEntityImportMapping.bindingID, Equal<Required<BCEntity.bindingID>>,
				And<BCEntityImportMapping.entityType, Equal<BCEntitiesAttribute.order>>>>>.Select(this, _processor.Operation.ConnectorType, _processor.Operation.Binding).FirstTableItems;
				SOTypeImportMapping = allMappings?.FirstOrDefault(
					i => i.TargetObject == nameof(SalesOrder) && i.TargetField == nameof(SalesOrder.OrderType))?.SourceField ?? String.Empty;
			}
			Tuple<string, string>[] soTypes = MultipleOrderTypeAttribute.BCOrderTypeSlot.GetCachedOrderTypes().OrderTypes;

			if(!String.IsNullOrEmpty(SOTypeImportMapping))
				try
				{
					if (!SOTypeImportMapping.StartsWith("="))
						return;

					var constants = ECExpressionHelper.FormulaConstantValuesRetrieval(SOTypeImportMapping)?.Distinct();
					foreach (string constant in constants ?? new List<string>())
						if (constant.Length == 2 && !orderTypes.Contains(constant, StringComparer.InvariantCultureIgnoreCase) && soTypes.Any(i => String.Equals(i.Item1, constant, StringComparison.InvariantCultureIgnoreCase)))
							orderTypes.Add(constant.ToUpper());
				}
				catch (Exception ex) {
					_processor.LogError(_processor.Operation.LogScope(), BCMessages.OrderTypeMappingParseFailed, SOTypeImportMapping, ex);
				}
		}
        #endregion
    }
}
