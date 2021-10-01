using PX.Api.ContractBased.Models;
using PX.Commerce.BigCommerce.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Location = PX.Objects.CR.Standalone.Location;

namespace PX.Commerce.BigCommerce
{
	public class BCCustomerEntityBucket : EntityBucketBase, IEntityBucket
	{
		public IMappedEntity Primary { get => Customer; }
		public override IMappedEntity[] PreProcessors { get => new IMappedEntity[] { CustomerPriceClass }; }
		public IMappedEntity[] Entities => new IMappedEntity[] { Customer };

		public CustomerLocation ConnectorGeneratedAddress;
		public MappedCustomer Customer;
		public MappedGroup CustomerPriceClass;
		public List<MappedLocation> CustomerAddresses;
	}

	public class BCCustomerRestrictor : BCBaseRestrictor, IRestrictor
	{
		public virtual FilterResult RestrictExport(IProcessor processor, IMappedEntity mapped)
		{
			return base.Restrict<MappedCustomer>(mapped, delegate (MappedCustomer obj)
			{
				BCBindingExt bindingExt = processor.GetBindingExt<BCBindingExt>();
				PX.Objects.AR.Customer guestCustomer = bindingExt.GuestCustomerID != null ? PX.Objects.AR.Customer.PK.Find((PXGraph)processor, bindingExt.GuestCustomerID) : null;

				if (guestCustomer != null && obj.Local != null && obj.Local.CustomerID?.Value != null)
				{
					if (guestCustomer.AcctCD.Trim() == obj.Local.CustomerID?.Value)
					{
						return new FilterResult(FilterStatus.Invalid,
							PXMessages.LocalizeFormatNoPrefixNLA(BCMessages.LogCustomerSkippedGuest, obj.Local.CustomerID?.Value ?? obj.Local.SyncID.ToString()));
					}
				}
				return null;
			});
		}

		public virtual FilterResult RestrictImport(IProcessor processor, IMappedEntity mapped)
		{
			return null;
		}
	}

	[BCProcessor(typeof(BCConnector), BCEntitiesAttribute.Customer, BCCaptions.Customer,
		IsInternal = false,
		Direction = SyncDirection.Bidirect,
		PrimaryDirection = SyncDirection.Bidirect,
		PrimarySystem = PrimarySystem.Extern,
		PrimaryGraph = typeof(PX.Objects.AR.CustomerMaint),
		ExternTypes = new Type[] { typeof(CustomerData) },
		LocalTypes = new Type[] { typeof(Core.API.Customer) },
		AcumaticaPrimaryType = typeof(PX.Objects.AR.Customer),
		AcumaticaPrimarySelect = typeof(PX.Objects.AR.Customer.acctCD),
		URL = "customers/{0}/edit"
	)]
	[BCProcessorRealtime(PushSupported = true, HookSupported = true,
		PushSources = new String[] { "BC-PUSH-Customers", "BC-PUSH-Locations" },
		WebHookType = typeof(WebHookCustomerAddress),
		WebHooks = new String[]
		{
			"store/customer/created",
			"store/customer/updated",
			"store/customer/deleted",
			"store/customer/address/created",
			"store/customer/address/updated",
			"store/customer/address/deleted"
		})]
	[BCProcessorExternCustomField(BCConstants.FormFields, BigCommerceCaptions.FormFields, nameof(CustomerData.FormFields), typeof(CustomerData))]
	public class BCCustomerProcessor : BCLocationBaseProcessor<BCCustomerProcessor, BCCustomerEntityBucket, MappedCustomer>, IProcessor
	{
		bool isLocationActive;
		protected CustomerPriceClassRestDataProvider customerPriceClassDataProvider;
		public BCHelper helper = PXGraph.CreateInstance<BCHelper>();

		#region Initialization
		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);
			customerPriceClassDataProvider = new CustomerPriceClassRestDataProvider(client);
			isLocationActive = ConnectorHelper.GetConnectorBinding(operation.ConnectorType, operation.Binding).ActiveEntities.Any(x => x == BCEntitiesAttribute.Address);
			if (isLocationActive)
			{
				locationProcessor = PXGraph.CreateInstance<BCLocationProcessor>();
				((BCLocationProcessor)locationProcessor).Initialise(iconnector, operation.Clone().With(_ => { _.EntityType = BCEntitiesAttribute.Address; return _; }), countriesAndStates);
			}

			helper.Initialize(this);
		}
		#endregion

		#region Common
		public override void WithTransaction(System.Action action)
		{
			action();
		}
		public override MappedCustomer PullEntity(Guid? localID, Dictionary<string, object> fields)
		{
			Core.API.Customer impl = cbapi.GetByID<Core.API.Customer>(localID);
			if (impl == null) return null;

			MappedCustomer obj = new MappedCustomer(impl, impl.SyncID, impl.SyncTime);

			return obj;
		}
		public override MappedCustomer PullEntity(string externID, string jsonObject)
		{
			FilterCustomers filter = new FilterCustomers { Include = "addresses,formfields" };
			filter.Id = externID.KeySplit(0);
			CustomerData data = customerDataProviderV3.GetAll(filter).FirstOrDefault();
			if (data == null) return null;

			MappedCustomer obj = new MappedCustomer(data, data.Id?.ToString(), data.DateModifiedUT.ToDate());
			if (isLocationActive) //location is active then check if anything is changed and  force set pending
			{

				BCSyncStatus customerStatus = PXSelect<BCSyncStatus,
						Where<BCSyncStatus.connectorType, Equal<Required<BCSyncStatus.connectorType>>,
							And<BCSyncStatus.bindingID, Equal<Required<BCSyncStatus.bindingID>>,
							And<BCSyncStatus.entityType, Equal<Required<BCSyncStatus.entityType>>,
							And<BCSyncStatus.externID, Equal<Required<BCSyncStatus.externID>>>>>>>
						.Select(this, Operation.ConnectorType, Operation.Binding, BCEntitiesAttribute.Customer, data.Id.ToString());
				if (customerStatus == null) return obj;
				bool changed = IsLocationModified(customerStatus.SyncID, data.Addresses);
				if (changed)
					obj.ExternTimeStamp = DateTime.MaxValue;
			}
			return obj;
		}

		public override IEnumerable<MappedCustomer> PullSimilar(IExternEntity entity, out string uniqueField)
		{
			uniqueField = ((CustomerData)entity)?.Email;
			if (uniqueField == null) return null;

			List<MappedCustomer> result = new List<MappedCustomer>();
			foreach (PX.Objects.AR.Customer item in helper.CustomerByEmail.Select(uniqueField))
			{
				Core.API.Customer data = new Core.API.Customer() { SyncID = item.NoteID, SyncTime = item.LastModifiedDateTime };
				result.Add(new MappedCustomer(data, data.SyncID, data.SyncTime));
			}

			if (result == null || result?.Count == 0) return null;

			return result;
		}
		public override IEnumerable<MappedCustomer> PullSimilar(ILocalEntity entity, out string uniqueField)
		{
			uniqueField = ((Core.API.Customer)entity)?.MainContact?.Email?.Value;
			if (uniqueField == null) return null;
			FilterCustomers filter = new FilterCustomers { Include = "addresses,formfields", Email = uniqueField };
			IEnumerable<CustomerData> datas = customerDataProviderV3.GetAll(filter);
			if (datas == null) return null;

			return datas.Select(data => new MappedCustomer(data, data.Id.ToString(), data.DateModifiedUT.ToDate()));
		}

		public override object GetExternCustomFieldValue(BCCustomerEntityBucket entity, ExternCustomFieldInfo customFieldInfo,
			object sourceData, string sourceObject, string sourceField, out string displayName)
		{
			displayName = null;
			//Get the Customer Form Fields
			return string.IsNullOrWhiteSpace(sourceField) ? null : GetCustomerFormFields(entity.Customer.Extern.FormFields, sourceField);
		}

		public override void SetExternCustomFieldValue(BCCustomerEntityBucket entity, ExternCustomFieldInfo customFieldInfo,
			object targetData, string targetObject, string targetField, string sourceObject, object value)
		{
			if (value != PXCache.NotSetValue)
			{
				dynamic resultValue = SetCustomerFormFields(targetData, targetObject, targetField, sourceObject, value);
				if (resultValue != null)
				{
					if (entity.Customer.Extern.FormFields == null)
						entity.Customer.Extern.FormFields = new List<CustomerFormFieldData>();
					entity.Customer.Extern.FormFields.Add(new CustomerFormFieldData()
					{
						CustomerId = entity.Customer.Extern.Id,
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
			FilterCustomers filter = new FilterCustomers { Include = "addresses,formfields", Sort = "date_created:asc" };
			if (minDateTime != null)
				filter.MinDateModified = minDateTime;
			if (maxDateTime != null)
				filter.MaxDateModified = maxDateTime;
			IEnumerable<CustomerData> datas = customerDataProviderV3.GetAll(filter);

			BCEntity entity = GetEntity(Operation.EntityType);
			int countNum = 0;
			List<IMappedEntity> mappedList = new List<IMappedEntity>();
			try
			{
				foreach (CustomerData data in datas)
				{
					IMappedEntity obj = new MappedCustomer(data, data.Id?.ToString(), data.DateModifiedUT.ToDate());
					if (entity.EntityType != obj.EntityType)
						entity = GetEntity(obj.EntityType);

					mappedList.Add(obj);
					countNum++;

					if (countNum % BatchFetchCount == 0)
					{
						ProcessMappedListForImport(ref mappedList, true);
					}
				}
			}
			finally
			{
				if (mappedList.Any())
				{
					ProcessMappedListForImport(ref mappedList, true);
				}
			}

		}

		public override EntityStatus GetBucketForImport(BCCustomerEntityBucket bucket, BCSyncStatus bcstatus)
		{
			FilterCustomers filter = new FilterCustomers { Include = "addresses,formfields", Id = bcstatus.ExternID };
			CustomerData data = customerDataProviderV3.GetAll(filter).FirstOrDefault();
			if (data == null) return EntityStatus.None;

			MappedCustomer obj = bucket.Customer = bucket.Customer.Set(data, data.Id?.ToString(), data.DateModifiedUT.ToDate());
			EntityStatus status = EnsureStatus(obj, SyncDirection.Import);
			if (isLocationActive && status == EntityStatus.Syncronized)
			{
				bool changed = IsLocationModified(obj.SyncID, data.Addresses);

				if (changed)
					status = EnsureStatus(obj, SyncDirection.Import, resync: changed);

			}

			if (data.CustomerGroupId != null && data.CustomerGroupId > 0 && GetEntity(BCEntitiesAttribute.CustomerPriceClass)?.IsActive == true)
			{
				var priceClass = customerPriceClassDataProvider.GetByID(data.CustomerGroupId.ToString());
				if (priceClass != null)
				{
					MappedGroup customerPriceClassObj = bucket.CustomerPriceClass = bucket.CustomerPriceClass.Set(priceClass, priceClass.Id?.ToString(), priceClass.CalculateHash());
					EntityStatus priceClassStatus = EnsureStatus(customerPriceClassObj, SyncDirection.Import);
				}
			}

			return status;
		}

		public override void MapBucketImport(BCCustomerEntityBucket bucket, IMappedEntity existing)
		{
			MappedCustomer customerObj = bucket.Customer;
			Core.API.Customer customerImpl = customerObj.Local = new Core.API.Customer();
			customerImpl.Custom = GetCustomFieldsForImport();

			//General Info
			string firstLastName = CustomerNameResolver(customerObj.Extern.FirstName, customerObj.Extern.LastName, (int)customerObj.Extern.Id);
			customerImpl.CustomerName = (string.IsNullOrEmpty(customerObj.Extern.Company) ? firstLastName : customerObj.Extern.Company).ValueField();
			customerImpl.AccountRef = APIHelper.ReferenceMake(customerObj.Extern.Id, GetBinding().BindingName).ValueField();
			customerImpl.CustomerClass = customerObj.LocalID == null || existing?.Local == null ? GetBindingExt<BCBindingExt>().CustomerClassID?.ValueField() : null;
			if (customerObj.Extern.CustomerGroupId > 0)
			{
				PX.Objects.AR.ARPriceClass priceClass = PXSelectJoin<PX.Objects.AR.ARPriceClass,
				LeftJoin<BCSyncStatus, On<PX.Objects.AR.ARPriceClass.noteID, Equal<BCSyncStatus.localID>>>,
				Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
					And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
					And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
					And<BCSyncStatus.externID, Equal<Required<BCSyncStatus.externID>>>>>>>.Select(this, BCEntitiesAttribute.CustomerPriceClass, customerObj.Extern.CustomerGroupId?.ToString());
				if (GetEntity(BCEntitiesAttribute.CustomerPriceClass)?.IsActive == true)
				{
					if (priceClass == null) throw new PXException(BCMessages.PriceClassNotSyncronizedForItem, customerObj.Extern.CustomerGroupId?.ToString());
				}
				customerImpl.PriceClassID = priceClass?.PriceClassID?.ValueField();
			}
			else
				customerImpl.PriceClassID = new StringValue() { Value = null };

			//Primary Contact
			customerImpl.PrimaryContact = new Core.API.Contact();
			customerImpl.PrimaryContact.FirstName = customerObj.Extern.FirstName.ValueField();
			customerImpl.PrimaryContact.LastName = customerObj.Extern.LastName.ValueField();

			//Main Contact
			Core.API.Contact contactImpl = customerImpl.MainContact = new Core.API.Contact();
			contactImpl.FullName = customerImpl.CustomerName; //FullName is mapped to the CompanyName
			contactImpl.Attention = firstLastName.ValueField();
			contactImpl.Email = customerObj.Extern.Email.ValueField();
			contactImpl.Phone2 = customerObj.Extern.Phone.ValueField();
			contactImpl.Active = true.ValueField();
			Core.API.Address addressImpl = contactImpl.Address = new Core.API.Address();
			bucket.CustomerAddresses = new List<MappedLocation>();
			StringValue shipVia = null;
			BCBindingExt bindingExt = GetBindingExt<BCBindingExt>();
			if (bindingExt.CustomerClassID != null)
			{
				PX.Objects.AR.CustomerClass customerClass = PXSelect<PX.Objects.AR.CustomerClass, Where<PX.Objects.AR.CustomerClass.customerClassID, Equal<Required<PX.Objects.AR.CustomerClass.customerClassID>>>>.Select(this, bindingExt.CustomerClassID);
				if (customerClass != null)
				{
					addressImpl.Country = customerClass.CountryID.ValueField(); // no address is present then set country from customer class
					shipVia = customerClass.ShipVia.ValueField();

				}
			}
			if (customerObj.Extern.Addresses?.Count() > 0)
			{
				var addressStatus = SelectStatusChildren(customerObj.SyncID).Where(s => s.EntityType == BCEntitiesAttribute.Address)?.ToList();
				// Get from all available address in BC  which is not marked as deleted
				CustomerAddressData addressObj = customerObj.Extern.Addresses.FirstOrDefault(x => ValidDefaultAddress(x, addressStatus));
				contactImpl.Phone1 = addressObj.Phone.ValueField();
				//Address
				addressImpl.AddressLine1 = addressObj.Address1.ValueField();
				addressImpl.AddressLine2 = addressObj.Address2.ValueField();
				addressImpl.City = addressObj.City.ValueField();
				addressImpl.Country = addressObj.CountryCode.ValueField();
				addressImpl.DefaultId = addressObj.Id.ValueField(); // to use this address to set as default
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
				if (isLocationActive)
				{
					List<PXResult<Location, PX.Objects.CR.Address, PX.Objects.CR.Contact, BAccount>> pXResult = null;
					if (existing != null && addressStatus?.Count() == 0)// merge locations for clearsync or if no sync status hystory exists for existing customer /Locations
					{
						pXResult = PXSelectJoin<Location, InnerJoin<PX.Objects.CR.Address, On<Location.defAddressID, Equal<PX.Objects.CR.Address.addressID>>,
						   InnerJoin<PX.Objects.CR.Contact, On<Location.defContactID, Equal<PX.Objects.CR.Contact.contactID>>,
						   InnerJoin<BAccount, On<Location.bAccountID, Equal<BAccount.bAccountID>>>>>
						   , Where<BAccount.noteID, Equal<Required<BAccount.noteID>>>>.Select(this, existing?.LocalID).
						   Cast<PXResult<Location, PX.Objects.CR.Address, PX.Objects.CR.Contact, BAccount>>().ToList();
					}
					foreach (CustomerAddressData externLocation in customerObj.Extern.Addresses)
					{
						externLocation.FormFields = externLocation.FormFields ?? new List<CustomerFormFieldData>();
						MappedLocation mappedLocation = new MappedLocation(externLocation, new object[] { customerObj.ExternID, externLocation.Id }.KeyCombine(), externLocation.CalculateHash(), customerObj.SyncID);

						mappedLocation.Local = MapLocationImport(externLocation, customerObj);
						if (addressObj.Id == externLocation.Id && !addressStatus.Any(y => y.ExternID.KeySplit(1) == externLocation.Id.ToString()))
							mappedLocation.Local.ShipVia = shipVia;
						locationProcessor.RemapBucketImport(new BCLocationEntityBucket() { Address = mappedLocation }, null);
						if (pXResult != null)
							mappedLocation.LocalID = CheckIfExists(mappedLocation.Extern, pXResult, bucket.CustomerAddresses);

						bucket.CustomerAddresses.Add(mappedLocation);
					}

				}
			}
			else if (existing == null)
			{
				bucket.ConnectorGeneratedAddress = new CustomerLocation();
				bucket.ConnectorGeneratedAddress.ShipVia = shipVia;
				bucket.ConnectorGeneratedAddress.ContactOverride = true.ValueField();
				bucket.ConnectorGeneratedAddress.AddressOverride = true.ValueField();
			}
		}

		public override void SaveBucketImport(BCCustomerEntityBucket bucket, IMappedEntity existing, string operation)
		{
			MappedCustomer obj = bucket.Customer;
			// Create/update customer with main/address
			var newDefault = obj.Local.MainContact.Address.DefaultId?.Value;
			Core.API.Customer impl = null;
			//After customer created successfully, it ensures system couldn't rollback customer creation if customer location fails later.
			WithTransaction(delegate ()
			{
				impl = cbapi.Put<Core.API.Customer>(obj.Local, obj.LocalID);
				obj.AddLocal(impl, impl.SyncID, impl.SyncTime);
				UpdateStatus(obj, operation);
			});

			if (bucket.ConnectorGeneratedAddress != null)
			{
				PX.Objects.CR.Location location = PXSelectJoin<PX.Objects.CR.Location,
			InnerJoin<PX.Objects.CR.BAccount, On<PX.Objects.CR.Location.locationID, Equal<PX.Objects.CR.BAccount.defLocationID>>>,
			Where<PX.Objects.CR.BAccount.noteID, Equal<Required<PX.Objects.CR.BAccount.noteID>>>>.Select(this, impl.SyncID);

				CustomerLocation addressImpl = cbapi.Put<CustomerLocation>(bucket.ConnectorGeneratedAddress, location.NoteID);
			}

			if (isLocationActive)
			{
				CustomerMaint graph;
				List<Location> locations;
				graph = PXGraph.CreateInstance<PX.Objects.AR.CustomerMaint>();
				graph.BAccount.Current = PXSelect<PX.Objects.AR.Customer, Where<PX.Objects.AR.Customer.acctCD,
										 Equal<Required<PX.Objects.AR.Customer.acctCD>>>>.Select(graph, impl.CustomerID.Value);

				locations = graph.GetExtension<CustomerMaint.LocationDetailsExt>().Locations.Select().RowCast<Location>().ToList();
				//create/update other address and create status line(including Main)
				var alladdresses = SelectStatusChildren(obj.SyncID).Where(s => s.EntityType == BCEntitiesAttribute.Address)?.ToList();
				Location location = graph.GetExtension<CustomerMaint.DefLocationExt>().DefLocation.SelectSingle();
				bool lastmodifiedUpdated = false;

				if (bucket.CustomerAddresses.Count > 0)
				{
					if (alladdresses.Count == 0)// we copy into main if its first location
					{
						//if multiple addresses existed and one of them has mapped to the default location, should find it first; otherwise use the first one
						var main = bucket.CustomerAddresses.FirstOrDefault(x => location?.NoteID != null && location?.NoteID == x.LocalID) ?? bucket.CustomerAddresses.FirstOrDefault();

						if (location != null && main.LocalID == null)
						{
							main.LocalID = location.NoteID; //if location already created
						}
					}
					foreach (var loc in bucket.CustomerAddresses)
					{
						try
						{
							loc.Local.Customer = impl.CustomerID;
							var status = EnsureStatus(loc, SyncDirection.Import, persist: false);
							if (loc.LocalID == null)
							{
								loc.Local.ShipVia = location.CCarrierID.ValueField();
							}
							else if (loc.LocalID != null && !locations.Any(x => x.NoteID == loc.LocalID)) continue; // means deletd location
							if (status == EntityStatus.Pending || Operation.SyncMethod == SyncMode.Force)
							{
								lastmodifiedUpdated = true;
								CustomerLocation addressImpl = cbapi.Put<CustomerLocation>(loc.Local, loc.LocalID);

								loc.AddLocal(addressImpl, addressImpl.SyncID, addressImpl.SyncTime);
								UpdateStatus(loc, operation);
							}
						}
						catch (Exception ex)
						{
							LogError(Operation.LogScope(loc), ex);
							UpdateStatus(loc, BCSyncOperationAttribute.LocalFailed, message: ex.Message);
						}

					}
				}

				DateTime? date = null;

				if (UpdateDefault(bucket, newDefault, locations, graph) || lastmodifiedUpdated)
				{
					date = helper.GetUpdatedDate(impl.CustomerID?.Value, date);
				}
				obj.AddLocal(impl, impl.SyncID, date ?? graph.BAccount.Current.LastModifiedDateTime);
				UpdateStatus(obj, operation);
			}
		}

		public virtual bool IsLocationModified(int? syncID, IList<CustomerAddressData> addresses, List<BCSyncStatus> addressReference = null)
		{
			bool changed = false;
			if (addressReference == null)
				addressReference = SelectStatusChildren(syncID).Where(s => s.EntityType == BCEntitiesAttribute.Address && s.Deleted == false && s.PendingSync == false)?.ToList();

			if (addresses != null && addressReference != null)
			{
				if (addressReference?.Count == addresses?.Count)
					foreach (var address in addresses)
					{
						var localObj = addressReference.FirstOrDefault(x => x.ExternID?.KeySplit(1) == address.Id.ToString());
						if (localObj == null) { changed = true; break; }
						address.FormFields = address.FormFields ?? new List<CustomerFormFieldData>();// as hash is calculated differently for null and empty list
						if (localObj.ExternHash != address.CalculateHash()) { changed = true; break; }
					}
				else
					changed = true;
			}
			else
				changed = true;
			return changed;
		}

		protected override void ProcessMappedListForImport(ref List<IMappedEntity> mappedList, bool updateFetchIncermentalTime = false)
		{
			var externIDs = mappedList.Select(x => x.ExternID).ToArray();

			List<BCSyncStatus> bcSyncStatusList = GetBCSyncStatusResult(mappedList.FirstOrDefault()?.EntityType, null, null, externIDs).Select(x => x.GetItem<BCSyncStatus>()).ToList();

			var syncIds = bcSyncStatusList.Select(x => x.SyncID)?.ToArray();
			IEnumerable<BCSyncStatus> addressReference = null;
			if (syncIds != null)
				addressReference = SelectStatusChildren(syncIds, BCEntitiesAttribute.Address);

			foreach (MappedCustomer oneMapped in mappedList)
			{
				EntityStatus status = EnsureStatusBulk(bcSyncStatusList, oneMapped, SyncDirection.Import);
				if (status == EntityStatus.Syncronized && isLocationActive)
				{
					var addressStatus = addressReference?.Where(s => s.ParentSyncID == oneMapped.SyncID && s.Deleted == false && s.PendingSync == false)?.ToList();
					bool changed = IsLocationModified(oneMapped.SyncID, oneMapped.Extern?.Addresses, addressStatus);
					if (changed)
					{
						oneMapped.ExternTimeStamp = DateTime.MaxValue;
						EnsureStatusBulk(bcSyncStatusList, oneMapped, SyncDirection.Import, false);
					}
				}
			}

			if (updateFetchIncermentalTime)
			{
				var lastEntity = mappedList.LastOrDefault();
				if (lastEntity != null)
					IncrFetchLastProcessedTime = ((MappedCustomer)lastEntity).Extern.DateModifiedUT.ToDate();
			}

			mappedList.Clear();
		}

		protected Guid? CheckIfExists(CustomerAddressData custAddr, List<PXResult<Location, PX.Objects.CR.Address, PX.Objects.CR.Contact, BAccount>> pXResults, List<MappedLocation> mappedLocations)
		{
			foreach (PXResult<Location, PX.Objects.CR.Address, PX.Objects.CR.Contact, BAccount> loc in pXResults)
			{
				Location location = loc.GetItem<Location>();
				PX.Objects.CR.Address address = loc.GetItem<PX.Objects.CR.Address>();
				PX.Objects.CR.Contact contact = loc.GetItem<PX.Objects.CR.Contact>();
				string stateID = null;
				if (!string.IsNullOrEmpty(custAddr.State))
				{
					States seekenState = countriesAndStates.FirstOrDefault(i => i.Key.Equals(custAddr.CountryCode)).Value.FirstOrDefault(i => string.Equals(i.StateID, custAddr.State, StringComparison.OrdinalIgnoreCase) ||
					string.Equals(i.State, custAddr.State, StringComparison.OrdinalIgnoreCase));
					var stateValue = seekenState?.StateID ?? custAddr.State;
					stateID = GetSubstituteLocalByExtern(BCSubstitute.GetValue(Operation.ConnectorType, BCSubstitute.State), stateValue, stateValue);
				}
				else
					stateID = string.Empty;
				string fullName = new String[] { contact.Attention, location.Descr }.FirstNotEmpty();
				if (CompareLocation(custAddr, location, address, contact, stateID, fullName))
				{
					if (!mappedLocations.Any(x => x.LocalID == location.NoteID))// as BC can have multiple same address 
						return location.NoteID;
				}
			}
			return null;
		}

		protected bool CompareLocation(CustomerAddressData custAddr, Location location, PX.Objects.CR.Address address, PX.Objects.CR.Contact contact, string stateID, string fullName)
		{
			return helper.CompareStrings(custAddr.City, address.City)
											&& (string.IsNullOrEmpty(custAddr.Company) || helper.CompareStrings(custAddr.Company, contact.FullName) || helper.CompareStrings(custAddr.Company, location.Descr))
											&& helper.CompareStrings(custAddr.CountryCode, address.CountryID)
											&& helper.CompareStrings(custAddr.FirstName, contact.FirstName ?? fullName.FieldsSplit(0, fullName))
											&& helper.CompareStrings(custAddr.LastName, contact.LastName ?? fullName.FieldsSplit(1, fullName))
											&& (string.IsNullOrEmpty(custAddr.Phone) || helper.CompareStrings(custAddr.Phone, contact.Phone1))
											&& helper.CompareStrings(stateID, address.State)
											&& helper.CompareStrings(custAddr.Address1, address.AddressLine1)
											&& helper.CompareStrings(custAddr.Address2, address.AddressLine2)
											&& helper.CompareStrings(custAddr.PostalCode, address.PostalCode);
		}

		protected bool UpdateDefault(BCCustomerEntityBucket bucket, int? newDefault, List<Location> locations, CustomerMaint graph)
		{
			var obj = bucket.Customer;
			bool updated = false;
			if (obj.LocalID != null)
			{
				var addressReferences = SelectStatusChildren(obj.SyncID).Where(s => s.EntityType == BCEntitiesAttribute.Address && s.Deleted == false)?.ToList();
				var deletedValues = addressReferences?.Where(x => bucket.CustomerAddresses.All(y => x.ExternID != y.ExternID)).ToList();
				var mappeddefault = bucket.CustomerAddresses.FirstOrDefault(x => x.Extern.Id == newDefault); // get new default mapped address
				int? previousDefault = helper.SetDefaultLocation(mappeddefault?.LocalID, locations, graph, ref updated);

				//mark status for address as deleted for addresses deleted at BC
				if (deletedValues != null && deletedValues.Count > 0)
				{
					var locGraph = PXGraph.CreateInstance<PX.Objects.AR.CustomerLocationMaint>();

					foreach (var value in deletedValues)
					{
						DeleteStatus(value, BCSyncOperationAttribute.NotFound);
						helper.DeactivateLocation(graph, previousDefault, locGraph, value);
					}

					updated = true;
				}
			}
			return updated;
		}


		protected bool ValidDefaultAddress(CustomerAddressData x, List<BCSyncStatus> addressStatus)
		{
			if (!addressStatus.Any(y => y.ExternID.KeySplit(1) == x.Id.ToString())) return true;
			else if (addressStatus.Any(y => y.ExternID.KeySplit(1) == x.Id.ToString() && y.Deleted == false)) return true;
			return true;
		}
		#endregion

		#region Export
		public override void FetchBucketsForExport(DateTime? minDateTime, DateTime? maxDateTime, PXFilterRow[] filters)
		{
			IEnumerable<Core.API.Customer> impls = cbapi.GetAll<Core.API.Customer>(new Core.API.Customer { CustomerID = new StringReturn() },
				minDateTime, maxDateTime, filters);

			int countNum = 0;
			List<IMappedEntity> mappedList = new List<IMappedEntity>();
			foreach (Core.API.Customer impl in impls)
			{
				IMappedEntity obj = new MappedCustomer(impl, impl.SyncID, impl.SyncTime);

				mappedList.Add(obj);
				countNum++;
				if (countNum % BatchFetchCount == 0)
				{
					ProcessMappedListForExport(ref mappedList);
				}
			}
			if (mappedList.Any())
			{
				ProcessMappedListForExport(ref mappedList);
			}
		}

		public override EntityStatus GetBucketForExport(BCCustomerEntityBucket bucket, BCSyncStatus bcstatus)
		{
			Core.API.Customer impl = cbapi.GetByID<Core.API.Customer>(bcstatus.LocalID, GetCustomFieldsForExport());
			if (impl == null) return EntityStatus.None;

			MappedCustomer obj = bucket.Customer = bucket.Customer.Set(impl, impl.SyncID, impl.SyncTime);
			EntityStatus status = EnsureStatus(bucket.Customer, SyncDirection.Export);
			if (!string.IsNullOrEmpty(impl.PriceClassID?.Value) && GetEntity(BCEntitiesAttribute.CustomerPriceClass)?.IsActive == true)
			{
				BCSyncStatus priceCLassStatus = PXSelectJoin<BCSyncStatus,
				LeftJoin<PX.Objects.AR.ARPriceClass, On<BCSyncStatus.localID, Equal<PX.Objects.AR.ARPriceClass.noteID>>>,
				Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
					And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
					And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
					And<PX.Objects.AR.ARPriceClass.priceClassID, Equal<Required<PX.Objects.AR.ARPriceClass.priceClassID>>>>>>>.Select(this, BCEntitiesAttribute.CustomerPriceClass, impl.PriceClassID?.Value);
				if (priceCLassStatus?.ExternID == null)
				{
					CustomerPriceClass priceClass = cbapi.Get<CustomerPriceClass>(new CustomerPriceClass() { PriceClassID = impl.PriceClassID });
					if (priceClass != null)
					{
						MappedGroup priceClassobj = bucket.CustomerPriceClass = bucket.CustomerPriceClass.Set(priceClass, priceClass.SyncID, priceClass.SyncTime);
						EntityStatus priceClassStatus = EnsureStatus(priceClassobj, SyncDirection.Export);
					}
				}
			}

			return status;
		}

		public override void MapBucketExport(BCCustomerEntityBucket bucket, IMappedEntity existing)
		{
			MappedCustomer customerObj = bucket.Customer;

			Core.API.Customer customerImpl = customerObj.Local;
			Core.API.Contact contactImpl = customerImpl.MainContact;
			Core.API.Contact primaryContact = customerImpl.PrimaryContact;
			Core.API.Address addressImpl = contactImpl.Address;
			CustomerData customerData = customerObj.Extern = new CustomerData();

			//Customer
			customerData.Id = customerObj.ExternID.ToInt();
			customerData.Company = contactImpl.FullName?.Value ?? customerImpl.CustomerName?.Value;

			//Contact	
			// Use primary contact firstname and last name as Customer's firstname and last name if primarycontact is present else use Customername
			customerData.FirstName = primaryContact?.FirstName?.Value ?? primaryContact?.LastName?.Value ?? customerImpl.CustomerName?.Value.FieldsSplit(0, customerImpl.CustomerName?.Value);
			customerData.LastName = primaryContact?.LastName?.Value ?? primaryContact?.FirstName?.Value ?? customerImpl.CustomerName?.Value.FieldsSplit(1, customerImpl.CustomerName?.Value);
			customerData.Email = contactImpl.Email?.Value;
			customerData.Phone = contactImpl.Phone2?.Value ?? contactImpl.Phone1?.Value;

			if (!string.IsNullOrEmpty(customerImpl.PriceClassID?.Value))
			{
				BCSyncStatus status = PXSelectJoin<BCSyncStatus,
					LeftJoin<PX.Objects.AR.ARPriceClass, On<BCSyncStatus.localID, Equal<PX.Objects.AR.ARPriceClass.noteID>>>,
					Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
						And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
						And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
						And<PX.Objects.AR.ARPriceClass.priceClassID, Equal<Required<PX.Objects.AR.ARPriceClass.priceClassID>>>>>>>.Select(this, BCEntitiesAttribute.CustomerPriceClass, customerImpl.PriceClassID?.Value);
				if (GetEntity(BCEntitiesAttribute.CustomerPriceClass)?.IsActive == true)
				{
					if (status?.ExternID == null) throw new PXException(BCMessages.PriceClassNotSyncronizedForItem, customerImpl.PriceClassID?.Value);
				}
				customerData.CustomerGroupId = status?.ExternID?.ToInt();
			}
			else
				customerData.CustomerGroupId = 0;

			//Address
			if (isLocationActive)
			{
				bucket.CustomerAddresses = new List<MappedLocation>();
				var addressReference = SelectStatusChildren(customerObj.SyncID).Where(s => s.EntityType == BCEntitiesAttribute.Address)?.ToList();

				var customerLocations = cbapi.GetAll<CustomerLocation>(new CustomerLocation()
				{
					Customer = new StringSearch() { Value = customerObj.Local.CustomerID.Value },
					ReturnBehavior = ReturnBehavior.All,
				});

				foreach (CustomerLocation customerLocation in customerLocations)
				{
					var mapped = addressReference.FirstOrDefault(x => x.LocalID == customerLocation.NoteID.Value);
					if (mapped == null || (mapped != null && mapped.Deleted == false))
					{
						if (mapped == null && customerLocation.LocationContact.Address.City?.Value == null && customerLocation.LocationContact.Address.AddressLine1?.Value == null
							 && customerLocation.LocationContact.Address.AddressLine2?.Value == null && customerLocation.LocationContact.Address.PostalCode?.Value == null)
							continue;// connector generated address
						MappedLocation mappedLocation = new MappedLocation(customerLocation, customerLocation.NoteID.Value, customerLocation.LastModifiedDateTime.Value, customerObj.SyncID);
						mappedLocation.Extern = MapLocationExport(mappedLocation);
						if (addressReference?.Count == 0 && existing != null)
						{
							mappedLocation.ExternID = CheckIfExists(mappedLocation.Local, existing, bucket.CustomerAddresses);
						}
						locationProcessor.RemapBucketExport(new BCLocationEntityBucket() { Address = mappedLocation }, null);
						bucket.CustomerAddresses.Add(mappedLocation);
					}

				}
			}
		}
		protected string CheckIfExists(CustomerLocation custAddr, IMappedEntity existing, List<MappedLocation> mappedLocations)
		{
			CustomerData data = (CustomerData)existing.Extern;
			foreach (CustomerAddressData address in data.Addresses)
			{
				string stateID = null;
				if (!string.IsNullOrEmpty(address.State))
				{
					States seekenState = countriesAndStates.FirstOrDefault(i => i.Key.Equals(address.CountryCode)).Value.FirstOrDefault(i => string.Equals(i.StateID, address.State, StringComparison.OrdinalIgnoreCase) ||
						string.Equals(i.State, address.State, StringComparison.OrdinalIgnoreCase));
					var stateValue = seekenState?.StateID ?? address.State;
					stateID = GetSubstituteLocalByExtern(BCSubstitute.GetValue(Operation.ConnectorType, BCSubstitute.State), stateValue, stateValue);
				}
				else
					stateID = string.Empty;
				string fullName = new String[] { custAddr.LocationContact.Attention?.Value, custAddr.LocationName?.Value }.FirstNotEmpty();
				if (CompareLocation(custAddr, address, stateID, fullName))
				{
					var id = new object[] { data.Id, address.Id }.KeyCombine();
					if (!mappedLocations.Any(x => x.ExternID == id))// as BC can have multiple same address 
						return id;
				}
			}
			return null;
		}

		protected bool CompareLocation(CustomerLocation custAddr, CustomerAddressData address, string stateID, string fullName)
		{
			return helper.CompareStrings(address.City, custAddr.LocationContact?.Address?.City?.Value)
											&& helper.CompareStrings(address.Company, custAddr.LocationContact?.FullName?.Value)
											&& (helper.CompareStrings(address.CountryCode, custAddr.LocationContact?.Address?.Country?.Value) || helper.CompareStrings(address.Country, custAddr.LocationContact?.Address?.Country?.Value))
											&& helper.CompareStrings(address.FirstName, fullName.FieldsSplit(0, fullName))
											&& helper.CompareStrings(address.LastName, fullName.FieldsSplit(1, fullName))
											&& (helper.CompareStrings(address.Phone, custAddr.LocationContact?.Phone1?.Value) || helper.CompareStrings(address.Phone, custAddr.LocationContact?.Phone2?.Value))
											&& helper.CompareStrings(stateID, custAddr.LocationContact?.Address?.State?.Value)
											&& helper.CompareStrings(address.Address1, custAddr.LocationContact?.Address?.AddressLine1?.Value)
											&& helper.CompareStrings(address.Address2, custAddr.LocationContact?.Address?.AddressLine2?.Value)
											&& helper.CompareStrings(address.PostalCode, custAddr.LocationContact?.Address?.PostalCode?.Value);
		}

		public override object GetAttribute(BCCustomerEntityBucket bucket, string attributeID)
		{
			MappedCustomer obj = bucket.Customer;
			Core.API.Customer impl = obj.Local;
			return impl.Attributes?.Where(x => string.Equals(x?.AttributeDescription?.Value, attributeID, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

		}
		public override void AddAttributeValue(BCCustomerEntityBucket bucket, string attributeID, object attributeValue)
		{
			MappedCustomer obj = bucket.Customer;
			Core.API.Customer impl = obj.Local;
			impl.Attributes = impl.Attributes ?? new List<AttributeValue>();
			AttributeValue attribute = new AttributeValue();
			attribute.AttributeID = new StringValue() { Value = attributeID };
			attribute.ValueDescription = new StringValue() { Value = attributeValue?.ToString() };
			impl.Attributes.Add(attribute);
		}

		public override void SaveBucketExport(BCCustomerEntityBucket bucket, IMappedEntity existing, string operation)
		{
			MappedCustomer obj = bucket.Customer;

			//Customer
			CustomerData customerData = null;
			IList<CustomerFormFieldData> formFields = obj.Extern.FormFields;
			//If customer form fields are not empty, it could cause an error		
			obj.Extern.FormFields = null;
			if (obj.ExternID == null || existing == null)
				customerData = customerDataProviderV3.Create(obj.Extern);
			else
				customerData = customerDataProviderV3.Update(obj.Extern);
			if (formFields?.Count > 0 && customerData.Id != null)
			{
				formFields.All(x => { x.CustomerId = customerData.Id; x.AddressId = null; x.Value = x.Value ?? string.Empty; return true; });
				customerData.FormFields = customerFormFieldRestDataProvider.UpdateAll((List<CustomerFormFieldData>)formFields);
			}
			obj.AddExtern(customerData, customerData.Id?.ToString(), customerData.DateModifiedUT.ToDate());

			UpdateStatus(obj, operation);

			#region Update ExternalRef
			string externalRef = APIHelper.ReferenceMake(customerData.Id?.ToString(), GetBinding().BindingName);

			string[] keys = obj.Local?.AccountRef?.Value?.Split(';');
			if (keys?.Contains(externalRef) != true)
			{
				if (!string.IsNullOrEmpty(obj.Local?.AccountRef?.Value))
					externalRef = new object[] { obj.Local?.AccountRef?.Value, externalRef }.KeyCombine();

				if (externalRef.Length < 50 && obj.Local.SyncID != null)
					PXDatabase.Update<BAccount>(
								  new PXDataFieldAssign(typeof(BAccount.acctReferenceNbr).Name, PXDbType.NVarChar, externalRef),
								  new PXDataFieldRestrict(typeof(BAccount.noteID).Name, PXDbType.UniqueIdentifier, obj.Local.NoteID?.Value)
								  );
			}
			#endregion

			//Address
			if (isLocationActive)
			{
				foreach (var address in bucket.CustomerAddresses)
				{
					try
					{
						address.Extern.CustomerId = customerData.Id;
						if (address.Local?.Active?.Value == false)
						{
							BCSyncStatus bCSyncStatus = PXSelect<BCSyncStatus,
									Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
									And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
									And<BCSyncStatus.entityType, Equal<Required<BCSyncStatus.entityType>>,
									And<BCSyncStatus.localID, Equal<Required<BCSyncStatus.localID>>>>>>>.Select(this, BCEntitiesAttribute.Address, address.LocalID);
							if (bCSyncStatus == null) continue; //New inactive location should not be synced
						}
						var status = EnsureStatus(address, SyncDirection.Export, persist: false);

						if (status == EntityStatus.Pending || Operation.SyncMethod == SyncMode.Force)
						{
							CustomerAddressData addressData = null;
							formFields = address.Extern.FormFields;
							address.Extern.FormFields = null;
							string locOperation;
							int? addressId = null;
							List<CustomerFormFieldData> updatedFormFields = null;
							if (address.ExternID == null || existing == null)
							{
								addressData = customerAddressDataProviderV3.Create(address.Extern);
								addressId = addressData.Id;
								locOperation = BCSyncOperationAttribute.ExternInsert;
							}
							else
							{
								addressId = address.Extern.Id = address.ExternID.KeySplit(1).ToInt();
								addressData = customerAddressDataProviderV3.Update(address.Extern);
								locOperation = BCSyncOperationAttribute.ExternUpdate;
							}

							if (addressData == null)// To remove after BC fixes issue(of not returning response) Support case 04607326
							{
								if (address.Extern.Id != null)
								{
									FilterAddresses filter = new FilterAddresses { Include = "formfields", CustomerId = customerData.Id.ToString(), Id = address.Extern.Id.ToString() };
									addressData = customerAddressDataProviderV3.GetAll(filter).FirstOrDefault();
								}
								else
									throw new PXException(BCMessages.AddressAlreadyExistsMSg, customerData.Id.ToString());
							}

							if (formFields?.Count > 0 && addressId != null)
							{
								formFields.All(x => { x.AddressId = addressId; x.CustomerId = null; return true; });
								updatedFormFields = customerFormFieldRestDataProvider.UpdateAll((List<CustomerFormFieldData>)formFields);
							}
							if (addressData != null)
								addressData.FormFields = updatedFormFields ?? new List<CustomerFormFieldData>();
							address.AddExtern(addressData, new object[] { customerData.Id, addressId }.KeyCombine(), (addressData ?? address.Extern).CalculateHash());
							address.ParentID = obj.SyncID;

							UpdateStatus(address, locOperation);
						}
					}
					catch (Exception ex)
					{

						LogError(Operation.LogScope(address), ex);
						UpdateStatus(address, BCSyncOperationAttribute.ExternFailed, message: ex.Message);
					}
				}

				var addressReferences = SelectStatusChildren(obj.SyncID).Where(s => s.EntityType == BCEntitiesAttribute.Address && s.Deleted == false)?.ToList();
				var deletedValues = addressReferences?.Where(x => bucket.CustomerAddresses.All(y => x.LocalID != y.LocalID)).ToList();

				if (deletedValues != null && deletedValues.Count > 0)
				{
					foreach (var value in deletedValues)
					{
						DeleteStatus(value, BCSyncOperationAttribute.NotFound);
					}
				}
			}

		}

		#endregion


	}
}
