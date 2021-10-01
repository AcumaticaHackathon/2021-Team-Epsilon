using PX.Api.ContractBased.Models;
using PX.Commerce.BigCommerce.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Data;
using PX.Objects.CS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PX.Commerce.BigCommerce
{
	public abstract class BCLocationBaseProcessor<TGraph, TEntityBucket, TPrimaryMapped> : BCProcessorSingleBase<TGraph, TEntityBucket, TPrimaryMapped>
		where TGraph : PXGraph
		where TEntityBucket : class, IEntityBucket, new()
		where TPrimaryMapped : class, IMappedEntity, new()
	{
		protected BCLocationProcessor locationProcessor;

		protected CustomerRestDataProviderV3 customerDataProviderV3;
		protected CustomerAddressRestDataProviderV3 customerAddressDataProviderV3;
		protected CustomerFormFieldRestDataProvider customerFormFieldRestDataProvider;
		protected List<Tuple<String, String, String>> formFieldsList;
		protected Dictionary<string, List<States>> countriesAndStates;
		protected StoreStatesProvider statesProvider;
		protected StoreCountriesProvider countriesProvider;
		protected BCRestClient client;
		public BCHelper helper = PXGraph.CreateInstance<BCHelper>();

		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);
			client = BCConnector.GetRestClient(GetBindingExt<BCBindingBigCommerce>());
			customerDataProviderV3 = new CustomerRestDataProviderV3(client);
			customerAddressDataProviderV3 = new CustomerAddressRestDataProviderV3(client);
			customerFormFieldRestDataProvider = new CustomerFormFieldRestDataProvider(client);
			statesProvider = new StoreStatesProvider(client);
			countriesProvider = new StoreCountriesProvider(client);
			formFieldsList = ConnectorHelper.GetConnectorSchema(operation.ConnectorType, operation.Binding, operation.EntityType)?.FormFields ?? new List<Tuple<string, string, string>>();
			if(countriesAndStates == null)
			{
				var states = statesProvider.Get();
				countriesAndStates =
					countriesProvider.Get().ToDictionary(
						x => x.CountryCode,
						x => states.Where(i => i.CountryID == x.ID).ToList());
			}
		}

		protected virtual CustomerLocation MapLocationImport(CustomerAddressData addressObj, MappedCustomer customerObj)
		{
			CustomerLocation locationImpl = new CustomerLocation();
			locationImpl.Custom = locationProcessor == null ? GetCustomFieldsForImport() : locationProcessor.GetCustomFieldsForImport();

			//Location
			string firstLastName = CustomerNameResolver(addressObj.FirstName, addressObj.LastName, (int)customerObj.Extern.Id);
			locationImpl.LocationName = (String.IsNullOrEmpty(addressObj.Company) ? firstLastName : addressObj.Company).ValueField();
			locationImpl.ContactOverride = true.ValueField();
			locationImpl.AddressOverride = true.ValueField();
			//Contact
			Contact contactImpl = locationImpl.LocationContact = new Contact();
			contactImpl.FirstName = addressObj.FirstName.ValueField();
			contactImpl.LastName = addressObj.LastName.ValueField();
			contactImpl.Attention = firstLastName.ValueField();
			contactImpl.Phone1 = new StringValue { Value = addressObj.Phone };
			contactImpl.FullName = addressObj.Company?.ValueField();

			//Address
			Address addressImpl = contactImpl.Address = new Address();
			addressImpl.AddressLine1 = addressObj.Address1.ValueField();
			addressImpl.AddressLine2 = addressObj.Address2.ValueField();
			addressImpl.City = addressObj.City.ValueField();
			addressImpl.Country = addressObj.CountryCode.ValueField();
			if (!string.IsNullOrEmpty(addressObj.State))
			{
				States seekenState = countriesAndStates.FirstOrDefault(i => i.Key.Equals(addressObj.CountryCode)).Value.FirstOrDefault(i => string.Equals(i.StateID, addressObj.State, StringComparison.OrdinalIgnoreCase) || 
					string.Equals(i.State, addressObj.State, StringComparison.OrdinalIgnoreCase));
				var stateValue = seekenState?.StateID ?? addressObj.State;
				addressImpl.State = GetSubstituteLocalByExtern(BCSubstitute.GetValue(Operation.ConnectorType, BCSubstitute.State), stateValue, stateValue).ValueField();
			}
			else
				addressImpl.State = string.Empty.ValueField();
			addressImpl.PostalCode = addressObj.PostalCode?.ToUpperInvariant()?.ValueField();
			return locationImpl;
		}
	
		protected virtual CustomerAddressData MapLocationExport(MappedLocation addressObj)
		{
			CustomerLocation locationImpl = addressObj.Local;
			CustomerAddressData addressData = new CustomerAddressData();

			
			//Contact
			Contact contactImpl = locationImpl.LocationContact;
			addressData.Company = contactImpl.FullName?.Value ?? locationImpl.LocationName?.Value;
			string fullName = new String[] {
				contactImpl.Attention?.Value,
				locationImpl.LocationName?.Value,
			 contactImpl.FullName?.Value
			}.FirstNotEmpty();
			if (string.IsNullOrWhiteSpace(fullName))
			{
				throw new PXException(BCMessages.CustomerLocationNameIsEmpty);
			}
			addressData.FirstName = fullName.FieldsSplit(0, fullName);
			addressData.LastName = fullName.FieldsSplit(1, fullName);
			addressData.Phone = contactImpl.Phone1?.Value ?? contactImpl.Phone2?.Value;

			//Address
			Address addressImpl = contactImpl.Address;
			addressData.Address1 = addressImpl.AddressLine1?.Value;
			addressData.Address2 = addressImpl.AddressLine2?.Value;
			addressData.City = addressImpl.City?.Value;
			addressData.CountryCode = addressImpl.Country?.Value;
			if (!string.IsNullOrEmpty(addressImpl.State?.Value))
			{
				var stateValue = GetSubstituteExternByLocal(BCSubstitute.GetValue(Operation.ConnectorType, BCSubstitute.State), addressImpl.State.Value, addressImpl.State.Value);
				addressData.State = countriesAndStates.FirstOrDefault(i => i.Key.Equals(addressImpl.Country.Value)).Value.FirstOrDefault(
					i => string.Equals(i.StateID, stateValue, StringComparison.OrdinalIgnoreCase) ||
						 string.Equals(i.State, stateValue, StringComparison.OrdinalIgnoreCase))?.State;
			}
			if (String.IsNullOrEmpty(addressData.State) && 
				countriesAndStates.FirstOrDefault(i => i.Key.Equals(addressImpl.Country.Value)).Value != null &&
				countriesAndStates.FirstOrDefault(i => i.Key.Equals(addressImpl.Country.Value)).Value.Count > 0)
				throw new PXException(BCMessages.NoValidStateForAddress, addressImpl.Country.Value, addressImpl.State?.Value);

			addressData.PostalCode = addressImpl.PostalCode?.Value;
			return addressData;
		}

		public virtual string CustomerNameResolver(string firstName, string lastName, int id)
		{
			if (String.IsNullOrEmpty(firstName) || String.IsNullOrEmpty(lastName))
				throw new PXException(BCMessages.CustomerNameIsEmpty, id);
			if (firstName.Equals(lastName))
				return firstName;
			return String.Concat(firstName, " ", lastName);
		}

		public override List<Tuple<string, string>> GetExternCustomFieldList(BCEntity entity, EntityInfo entityInfo,
			ExternCustomFieldInfo customFieldInfo, PropertyInfo objectPropertyInfo = null)
		{
			List<Tuple<String, String>> fieldsList = new List<Tuple<String, String>>();
			SchemaInfo extEntitySchema = ConnectorHelper.GetConnectorSchema(entity.ConnectorType, entity.BindingID, entity.EntityType);
			foreach (var formField in extEntitySchema.FormFields?.Where(x => x.Item1 == entity.EntityType))
			{
				fieldsList.Add(Tuple.Create(formField.Item2, formField.Item2));
			}
			return fieldsList;
		}

		public object GetCustomerFormFields(IList<CustomerFormFieldData> customerFormFields, string fieldName)
		{
			if (customerFormFields?.Count > 0)
			{
				var formField = customerFormFields.Where(x => x.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
				if (formField.Value is Newtonsoft.Json.Linq.JArray)
				{
					var fieldValues = formField?.Value as Newtonsoft.Json.Linq.JArray;
					if (fieldValues == null || fieldValues.Count == 0) return null;
					return string.Join(",", fieldValues.ToObject<string[]>());
				}
				return formField?.Value;
			}
			return null;
		}

		public object SetCustomerFormFields(object targetData, string targetObject, string targetField, string sourceObject, object sourceValue)
		{
			dynamic value = null;
			if (sourceValue != null && !(sourceValue is string && string.IsNullOrWhiteSpace(sourceValue.ToString())))
			{
				var collectionType = sourceValue.GetType().IsGenericType &&
						(sourceValue.GetType().GetGenericTypeDefinition() == typeof(List<>) ||
						 sourceValue.GetType().GetGenericTypeDefinition() == typeof(IList<>));
				var formFieldType = formFieldsList.Where(x => x.Item1 == Operation.EntityType && string.Equals(x.Item2, targetField?.Trim(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault()?.Item3;
				if (collectionType && formFieldType == BCExternCustomFieldAttribute.JArray)
				{
					value = new List<string>();
					foreach (object item in (IList<object>)sourceValue)
					{
						((List<string>)value).Add(Convert.ToString(item));
					}
				}
				else if (!collectionType && formFieldType == BCExternCustomFieldAttribute.JArray)
				{
					var strValue = Convert.ToString(sourceValue);
					if (strValue.Length > 1 && strValue.Contains(","))
						value = strValue.Split(',');
					else if (strValue.Length > 1 && strValue.Contains(";"))
						value = strValue.Split(';');
					else
						value = strValue == null ? new List<string>() : new List<string> { strValue };
				}
				else if (collectionType && formFieldType == BCExternCustomFieldAttribute.Value)
				{
					value = string.Empty;
					foreach (object item in (IList<object>)sourceValue)
					{
						value = (string)value + (Convert.ToString(item));
					}
				}
				else
				{
					value = Convert.ToString(sourceValue);
				}
			}
			return value;
		}

		public override string ValidateExternCustomField(BCEntity entity, EntityInfo entityInfo,
			ExternCustomFieldInfo customFieldInfo, string sourceObject, string sourceField, string targetObject, string targetField, EntityOperationType direction)
		{
			if (!string.IsNullOrEmpty(sourceField) && sourceField.StartsWith("=") && direction == EntityOperationType.ImportMapping)
			{
				return BCMessages.InvalidSourceFieldFormFields;
			}
			return null;
		}
	}
}
