using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using PX.Commerce.Core;
using System.ComponentModel;

namespace PX.Commerce.BigCommerce.API.REST
{
	[JsonObject(Description = "Customer list (BigCommerce API v3 response)")]
	public class CustomerSingle: IEntityResponse<CustomerData>
	{
		[JsonProperty("data")]
		public CustomerData Data { get; set; }

		[JsonProperty("meta")]
		public Meta Meta { get; set; }
	}
	[JsonObject(Description = "Customer list (BigCommerce API v3 response)")]
	public class CustomerList : IEntitiesResponse<CustomerData>
	{
		public CustomerList()
		{
			Data = new List<CustomerData>();
		}

		[JsonProperty("data")]
		public List<CustomerData> Data { get; set; }

		[JsonProperty("meta")]
		public Meta Meta { get; set; }		
	}

	[JsonObject(Description = "Customer")]
	[CommerceDescription(BigCommerceCaptions.Customer)]
	public class CustomerData: BCAPIEntity
	{
        public CustomerData()
        {
            Addresses = new List<CustomerAddressData>();
        }

        [JsonProperty("id")]
        [CommerceDescription(BigCommerceCaptions.ID, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
        public virtual int? Id { get; set; }

        [JsonProperty("company")]
		[CommerceDescription(BigCommerceCaptions.CompanyName, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
        public virtual string Company { get; set; }

        [JsonProperty("first_name")]
		[CommerceDescription(BigCommerceCaptions.FirstName, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		[ValidateRequired]
		public virtual string FirstName { get; set; }

        [JsonProperty("last_name")]
		[CommerceDescription(BigCommerceCaptions.LastName, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		[ValidateRequired()]
		public virtual string LastName { get; set; }

        [JsonProperty("email")]
		[CommerceDescription(BigCommerceCaptions.EmailAddress, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		[ValidateRequired()]
		public virtual string Email { get; set; }

        [JsonProperty("phone")]
		[CommerceDescription(BigCommerceCaptions.PhoneNumber, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		[ValidateRequired(AutoDefault = true)]
        public virtual string Phone { get; set; }

        [JsonProperty("date_created")]
        [CommerceDescription(BigCommerceCaptions.DateCreatedUT, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
        public virtual string DateCreatedUT { get; set; }

        [Description(BigCommerceCaptions.DateCreatedUT)]
        [ShouldNotSerialize]
        public virtual DateTime CreatedDateTime
        {
            get
            {
                return this.DateCreatedUT != null ? (DateTime)this.DateCreatedUT.ToDate() : default(DateTime);
            }
        }

        [JsonProperty("date_modified")]
        [CommerceDescription(BigCommerceCaptions.DateModifiedUT, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
        public virtual string DateModifiedUT { get; set; }

        [Description(BigCommerceCaptions.DateModifiedUT)]
        [ShouldNotSerialize]
        public virtual DateTime ModifiedDateTime
        {
            get
            {
                return this.DateModifiedUT != null ? (DateTime)this.DateModifiedUT.ToDate() : default(DateTime);
            }
        }


        [JsonProperty("registration_ip_address")]
        public virtual string RegistrationIpAddress { get; set; }

        [JsonProperty("customer_group_id")]
		[CommerceDescription(BigCommerceCaptions.CustomerGroupId, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
		public virtual int? CustomerGroupId { get; set; }

        [JsonProperty("notes")]
        [CommerceDescription(BigCommerceCaptions.Notes, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
        public virtual string Notes { get; set; }

        [JsonProperty("tax_exempt_category")]
        [CommerceDescription(BigCommerceCaptions.TaxExemptCode, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
        public virtual string TaxExemptCode { get; set; }

        [JsonProperty("accepts_product_review_abandoned_cart_emails")]
        [CommerceDescription(BigCommerceCaptions.ReceiveACSOrReviewEmails, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
        public virtual bool ReceiveACSOrReviewEmails { get; set; }

        [JsonProperty("authentication")]
        [CommerceDescription(BigCommerceCaptions.Authentication, FieldFilterStatus.Skipped, FieldMappingStatus.Skipped)]
        public virtual Authentication Authentication { get; set; }
       
        [JsonProperty("addresses")]
		[CommerceDescription(BigCommerceCaptions.CustomerAddress, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
        public IList<CustomerAddressData> Addresses { get; set; }

        [JsonProperty("form_fields")]
        [CommerceDescription(BigCommerceCaptions.FormFields, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		[BCExternCustomField(BCConstants.FormFields)]
		public IList<CustomerFormFieldData> FormFields { get; set; }

        public void AddAddresses(CustomerAddressData address)
        {
            Addresses.Add(address);
        }

        public void AddAddresses(List<CustomerAddressData> addresses)
        {
            if (addresses != null)
            {
                ((List<CustomerAddressData>)Addresses).AddRange(addresses);
            }
        }

        //Conditional Serialization
        public bool ShouldSerializeId()
        {
            return Id != null ? true: false;
        }

        public bool ShouldSerializeDateCreatedUT()
        {
            return false;
        }

        public bool ShouldSerializeDateModifiedUT()
        {
            return false;
        }

        public bool ShouldSerializeAddresses()
        {
            return false;
        }
	}

    [JsonObject(Description = "Customer -> Authentication")]
    [CommerceDescription(BigCommerceCaptions.Authentication)]
    public class Authentication
    {
        [JsonProperty("force_password_reset")]
        [CommerceDescription(BigCommerceCaptions.ForcePasswordReset)]
        public virtual bool ForcePasswordReset { get; set; }

    }

}
