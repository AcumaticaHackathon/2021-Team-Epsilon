using PX.Api.ContractBased.Models;
using PX.Commerce.BigCommerce.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Data;
using PX.Objects.CS;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Commerce.BigCommerce
{
	public class BCLocationEntityBucket : EntityBucketBase, IEntityBucket
	{
		public IMappedEntity Primary => Address;
		public IMappedEntity[] Entities => new IMappedEntity[] { Address, Customer };

		public override IMappedEntity[] PostProcessors => new IMappedEntity[] { Customer };

		public MappedLocation Address;
		public MappedCustomer Customer;
	}

	public class BCLocationRestrictor : BCBaseRestrictor, IRestrictor
	{
		public virtual FilterResult RestrictExport(IProcessor processor, IMappedEntity mapped)
		{
			return base.Restrict<MappedLocation>(mapped, delegate (MappedLocation obj)
			{
				BCBindingExt bindingExt = processor.GetBindingExt<BCBindingExt>();
				PX.Objects.AR.Customer guestCustomer = bindingExt.GuestCustomerID != null ? PX.Objects.AR.Customer.PK.Find((PXGraph)processor, bindingExt.GuestCustomerID) : null;

				if (guestCustomer != null && obj.Local != null && obj.Local.Customer?.Value != null)
				{
					if (guestCustomer.AcctCD.Trim() == obj.Local.Customer?.Value)
					{
						return new FilterResult(FilterStatus.Invalid,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogLocationSkippedGuest, obj.Local.LocationID?.Value ?? obj.Local.SyncID.ToString()));
					}
				}
				BCSyncStatus customeStatus = PXSelectJoin<BCSyncStatus,
					InnerJoin<PX.Objects.AR.Customer, On<BCSyncStatus.localID, Equal<PX.Objects.AR.Customer.noteID>>>,
					Where<PX.Objects.AR.Customer.acctCD, Equal<Required<PX.Objects.AR.Customer.acctCD>>,
						And<BCSyncStatus.connectorType, Equal<Required<BCSyncStatus.connectorType>>,
						And<BCSyncStatus.bindingID, Equal<Required<BCSyncStatus.bindingID>>,
						And<BCSyncStatus.entityType, Equal<Required<BCSyncStatus.entityType>>>>>>>
					.Select((PXGraph)processor, obj.Local?.Customer?.Value?.Trim(), processor.Operation.ConnectorType, processor.Operation.Binding, BCEntitiesAttribute.Customer);
				if (customeStatus?.ExternID == null)
				{
					return new FilterResult(FilterStatus.Invalid,
						PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogLocationSkippedCustomerNotSynced, obj.Local.LocationID?.Value ?? obj.Local.SyncID.ToString(), obj.Local.Customer.Value));
				}

				return null;
			});
		}

		public virtual FilterResult RestrictImport(IProcessor processor, IMappedEntity mapped)
		{
			#region Locations
			return base.Restrict<MappedLocation>(mapped, delegate (MappedLocation obj)
			{
				if (processor.SelectStatus(BCEntitiesAttribute.Customer, obj.Extern?.CustomerId?.ToString()) == null)
				{
					return new FilterResult(FilterStatus.Invalid,
						PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogLocationSkippedCustomerNotSynced, obj.Extern?.Id?.ToString(), obj.Extern?.CustomerId?.ToString()));
				}

				return null;
			});
			#endregion
		}
	}

	[BCProcessor(typeof(BCConnector), BCEntitiesAttribute.Address, BCCaptions.Address,
		IsInternal = false,
		Direction = SyncDirection.Bidirect,
		PrimaryDirection = SyncDirection.Bidirect,
		PrimarySystem = PrimarySystem.Extern,
		PrimaryGraph = typeof(PX.Objects.AR.CustomerLocationMaint),
		ExternTypes = new Type[] { typeof(CustomerAddressData) },
		LocalTypes = new Type[] { typeof(CustomerLocation) },
		AcumaticaPrimaryType = typeof(PX.Objects.CR.Location),
		URL = "customers/{0}/edit/{1}/edit-address",
		Requires = new string[] { BCEntitiesAttribute.Customer },
		ParentEntity = BCEntitiesAttribute.Customer
	)]
	[BCProcessorExternCustomField(BCConstants.FormFields, BigCommerceCaptions.FormFields, nameof(CustomerAddressData.FormFields), typeof(CustomerAddressData))]
	public class BCLocationProcessor : BCLocationBaseProcessor<BCLocationProcessor, BCLocationEntityBucket, MappedLocation>, IProcessor
	{

		#region Constructor
		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);
		}

		public void Initialise(IConnector iconnector, ConnectorOperation operation, Dictionary<string, List<States>> _countriesAndStates)
		{
			countriesAndStates = _countriesAndStates;
			Initialise(iconnector, operation);
		}
		#endregion

		#region Common
		public override object GetExternCustomFieldValue(BCLocationEntityBucket entity, ExternCustomFieldInfo customFieldInfo,
			object sourceData, string sourceObject, string sourceField, out string displayName)
		{
			displayName = null;
			//Get the Customer Location Form Fields
			return string.IsNullOrWhiteSpace(sourceField) ? null : GetCustomerFormFields(entity.Address.Extern.FormFields, sourceField);
		}

		public override void SetExternCustomFieldValue(BCLocationEntityBucket entity, ExternCustomFieldInfo customFieldInfo,
			object targetData, string targetObject, string targetField, string sourceObject, object value)
		{
			if (value != PXCache.NotSetValue)
			{
				dynamic resultValue = SetCustomerFormFields(targetData, targetObject, targetField, sourceObject, value);
				if (resultValue != null)
				{
					if (entity.Address.Extern.FormFields == null)
						entity.Address.Extern.FormFields = new List<CustomerFormFieldData>();
					entity.Address.Extern.FormFields.Add(new CustomerFormFieldData()
					{
						CustomerId = entity.Address.Extern.Id,
						Name = targetField,
						Value = resultValue
					});
				}
			}
		}
		#endregion


		#region Import
		public override void FetchBucketsForImport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
		}
		public override EntityStatus GetBucketForImport(BCLocationEntityBucket bucket, BCSyncStatus syncstatus)
		{
			FilterCustomers filter = new FilterCustomers { Include = "addresses,formfields", Id = syncstatus.ExternID.KeySplit(0) };
			CustomerData customerData = customerDataProviderV3.GetAll(filter).FirstOrDefault();
			if (customerData == null) return EntityStatus.None;

			int? addressKey = syncstatus.ExternID.KeySplit(1).ToInt();
			foreach (CustomerAddressData customerAddressData in customerData.Addresses)
			{
				if (customerAddressData.Id == addressKey)
				{
					if (customerData == null) return EntityStatus.None;

					MappedLocation addressObj = bucket.Address = bucket.Address.Set(customerAddressData, new object[] { customerData.Id, customerAddressData.Id }.KeyCombine(), customerAddressData.CalculateHash());
					EntityStatus status = EnsureStatus(addressObj, SyncDirection.Import);

					MappedCustomer customerObj = bucket.Customer = bucket.Customer.Set(customerData, customerData.Id?.ToString(), customerData.DateModifiedUT.ToDate());
					EntityStatus customerStatus = EnsureStatus(customerObj, SyncDirection.Import);
					addressObj.ParentID = customerObj.SyncID;

					return status;
				}
			}

			return EntityStatus.None;
		}

		public override void MapBucketImport(BCLocationEntityBucket bucket, IMappedEntity existing)
		{
			MappedLocation addressObj = bucket.Address;
			MappedCustomer customerObj = bucket.Customer;
			//Existing
			PX.Objects.AR.Customer customer = PXSelect<PX.Objects.AR.Customer,
				Where<PX.Objects.AR.Customer.noteID, Equal<Required<PX.Objects.AR.Customer.noteID>>>>.Select(this, customerObj.LocalID);
			if (customer == null) throw new PXException(BCMessages.NoCustomerForAddress, addressObj.ExternID);
			addressObj.Local = MapLocationImport(addressObj.Extern, customerObj);
			addressObj.Local.Customer = customer.AcctCD?.Trim().ValueField();
			addressObj.Local.Active = true.ValueField();

		}

		public override void SaveBucketImport(BCLocationEntityBucket bucket, IMappedEntity existing, String operation)
		{
			MappedLocation obj = bucket.Address;

			if (existing?.Local != null) obj.Local.Customer = ((CustomerLocation)existing.Local).Customer.Value.SearchField();
			if (existing?.Local != null) obj.Local.LocationID = ((CustomerLocation)existing.Local).LocationID.Value.SearchField();
			var alladdresses = SelectStatusChildren(obj.ParentID).Where(s => s.EntityType == BCEntitiesAttribute.Address)?.ToList();
			PX.Objects.CR.Location location = PXSelectJoin<PX.Objects.CR.Location,
		InnerJoin<PX.Objects.CR.BAccount, On<PX.Objects.CR.Location.locationID, Equal<PX.Objects.CR.BAccount.defLocationID>>>,
		Where<PX.Objects.CR.BAccount.noteID, Equal<Required<PX.Objects.CR.BAccount.noteID>>>>.Select(this, bucket.Customer.LocalID);

			if (alladdresses.Count == 0)// we copy into main if its first location
			{
				if (location != null && obj.LocalID == null)
				{
					obj.LocalID = location.NoteID; //if location already created
				}
			}
			if (obj.LocalID == null)
			{
				obj.Local.ShipVia = location.CCarrierID.ValueField();
			}
			CustomerLocation addressImpl = cbapi.Put<CustomerLocation>(obj.Local, obj.LocalID);

			obj.AddLocal(addressImpl, addressImpl.SyncID, addressImpl.SyncTime);
			UpdateStatus(obj, operation);
		}

		#endregion

		#region Export
		public override void FetchBucketsForExport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
		}
		public override EntityStatus GetBucketForExport(BCLocationEntityBucket bucket, BCSyncStatus syncstatus)
		{
			CustomerLocation addressImpl = cbapi.GetByID<CustomerLocation>(syncstatus.LocalID, GetCustomFieldsForExport());
			if (addressImpl == null) return EntityStatus.None;

			//Address
			MappedLocation addressObj = bucket.Address = bucket.Address.Set(addressImpl, addressImpl.SyncID, addressImpl.SyncTime);
			EntityStatus status = EnsureStatus(bucket.Address, SyncDirection.Export);

			//Customer
			BCSyncStatus customerStatus = PXSelectJoin<BCSyncStatus,
				InnerJoin<PX.Objects.AR.Customer, On<BCSyncStatus.localID, Equal<PX.Objects.AR.Customer.noteID>>>,
				Where<PX.Objects.AR.Customer.acctCD, Equal<Required<PX.Objects.AR.Customer.acctCD>>,
					And<BCSyncStatus.connectorType, Equal<Required<BCSyncStatus.connectorType>>,
					And<BCSyncStatus.bindingID, Equal<Required<BCSyncStatus.bindingID>>,
					And<BCSyncStatus.entityType, Equal<Required<BCSyncStatus.entityType>>>>>>>
				.Select(this, addressObj.Local.Customer?.Value?.Trim(), syncstatus.ConnectorType, syncstatus.BindingID, BCEntitiesAttribute.Customer);
			if (customerStatus == null) throw new PXException(BCMessages.CustomerNotSyncronized, addressImpl.Customer?.Value);

			MappedCustomer customerObj = bucket.Customer = bucket.Customer.Set((Customer)null, customerStatus.LocalID, customerStatus.LocalTS);
			customerObj.AddExtern(null, customerStatus.ExternID, customerStatus.ExternTS);
			addressObj.ParentID = customerStatus.SyncID;

			return status;
		}

		public override void MapBucketExport(BCLocationEntityBucket bucket, IMappedEntity existing)
		{
			MappedLocation addressObj = bucket.Address;
			MappedCustomer customerObj = bucket.Customer;
			if (customerObj == null || customerObj.ExternID == null) throw new PXException(BCMessages.CustomerNotSyncronized, addressObj.Local.Customer.Value);
			
			addressObj.Extern = MapLocationExport(addressObj);
		}

		public override void SaveBucketExport(BCLocationEntityBucket bucket, IMappedEntity existing, String operation)
		{
			MappedLocation obj = bucket.Address;

			CustomerAddressData addressData = null;
			IList<CustomerFormFieldData> formFields = obj.Extern.FormFields;
			//If customer form fields are not empty, it could cause an error		
			obj.Extern.FormFields = null;
			obj.Extern.CustomerId = bucket.Customer.ExternID.ToInt();
			int? addressId = null;
			List<CustomerFormFieldData> updatedFormFields = null;
			if (obj.ExternID == null || existing == null)
			{
				addressData = customerAddressDataProviderV3.Create(obj.Extern);
				addressId = addressData.Id;
			}
			else
			{
				addressId = obj.Extern.Id = obj.ExternID.KeySplit(1).ToInt();
				addressData = customerAddressDataProviderV3.Update(obj.Extern);
			}
			if (addressData == null)// To remove after BC fixes issue(of not returning response) Support case 04607326
			{
				if (obj.Extern.Id != null)
				{
					FilterAddresses filter = new FilterAddresses { Include = "formfields", CustomerId = obj.Extern.CustomerId.ToString(), Id = obj.Extern.Id.ToString() };
					addressData = customerAddressDataProviderV3.GetAll(filter).FirstOrDefault();
				}
				else
					throw new PXException(BCMessages.AddressAlreadyExistsMSg, obj.Extern.CustomerId.ToString());
			}

			if (formFields?.Count > 0 && addressId != null)
			{
				formFields.All(x => { x.AddressId = addressId; x.CustomerId = null; return true; });
				updatedFormFields = customerFormFieldRestDataProvider.UpdateAll((List<CustomerFormFieldData>)formFields);
			}

			var alladdresses = SelectStatusChildren(obj.ParentID).Where(s => s.EntityType == BCEntitiesAttribute.Address)?.ToList();
			if (alladdresses.All(x => x.Deleted == true) && obj.Local.Active?.Value == false)
			{
				bucket.Customer.ExternTimeStamp = DateTime.MaxValue;
				EnsureStatus(bucket.Customer);

			}
			if (obj.Local.Active?.Value == false)
			{
				CustomerLocation addressImpl = cbapi.Put<CustomerLocation>(new CustomerLocation() { Active = true.ValueField() }, obj.LocalID);
			}
			
			if(addressData != null)
				addressData.FormFields = updatedFormFields ?? new List<CustomerFormFieldData>();
			obj.AddExtern(addressData, new object[] { obj.Extern.CustomerId, addressId }.KeyCombine(), (addressData??obj.Extern).CalculateHash());
			UpdateStatus(obj, operation);

		}

		public override MappedLocation PullEntity(string externID, string externalInfo)
		{
			throw new NotImplementedException();
		}

		public override MappedLocation PullEntity(Guid? localID, Dictionary<string, object> externalInfo)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
