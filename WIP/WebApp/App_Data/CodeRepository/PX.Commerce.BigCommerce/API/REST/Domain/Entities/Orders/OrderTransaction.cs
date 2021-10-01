using System;
using PX.Commerce.BigCommerce.API.REST;
using Newtonsoft.Json;
using PX.Commerce.Core;
using System.ComponentModel;

namespace PX.Commerce.BigCommerce.API.REST
{
	[CommerceDescription(BigCommerceCaptions.OrdersTransactionData)]
    public class OrdersTransactionData : BCAPIEntity
	{
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("order_id")]
		[CommerceDescription(BigCommerceCaptions.OrderId, FieldFilterStatus.Filterable, FieldMappingStatus.Import)]
        public string OrderId { get; set; }

        [JsonProperty("event")]
        public OrderPaymentEvent Event { get; set; }
		[JsonIgnore]
		public OrderPaymentEvent LastEvent { get; set; }

		[IncludeInHash]
		public OrderPaymentEvent Action { get; set; }

		// Allowed values: "credit_card", "electronic_wallet", "apple_pay_card", "apple_pay_token", "store_credit", "gift_certificate", "custom", "token", "nonce", "offsite", "offline"
		[JsonProperty("method")]
		[CommerceDescription(BigCommerceCaptions.PaymentMethod, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]		
		public String PaymentMethod { get; set; }

        [JsonProperty("amount")]
		[CommerceDescription(BigCommerceCaptions.Amount, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public double Amount { get; set; }

        [JsonProperty("currency")]
		[CommerceDescription(BigCommerceCaptions.Currency, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public string Currency { get; set; }

        [JsonProperty("gateway")]
		[CommerceDescription(BigCommerceCaptions.Gateway, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public string Gateway { get; set; }

        [JsonProperty("gateway_transaction_id")]
		[CommerceDescription(BigCommerceCaptions.GatewayTranscationId, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
        public string GatewayTransactionId { get; set; }

        [JsonProperty("date_created")]
		public virtual string DateCreatedUT { get; set; }

		[ShouldNotSerialize]
		[CommerceDescription(BigCommerceCaptions.DateCreatedUT, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public virtual DateTime CreatedDateTime
		{
			get
			{
				return this.DateCreatedUT != null ? (DateTime)this.DateCreatedUT.ToDate() : default(DateTime);
			}
			set
			{
				this.DateCreatedUT = value.ToString();
			}
		}

		[JsonProperty("test")]
        public bool Test { get; set; }

        [JsonProperty("status")]
		[CommerceDescription(BigCommerceCaptions.OrderPaymentStatus, FieldFilterStatus.Filterable, FieldMappingStatus.ImportAndExport)]
		public OrderPaymentStatus Status { get; set; }

        [JsonProperty("fraud_review")]
        public bool FraudReview { get; set; }

        [JsonProperty("reference_transaction_id")]
        public int? ReferenceTransactionId { get; set; }

        [JsonProperty("offline")]
		[CommerceDescription(BigCommerceCaptions.OfflinePayment, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public OfflinePayment OfflinePayment { get; set; }

        [JsonProperty("custom")]
		[CommerceDescription(BigCommerceCaptions.CustomPayment, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public CustomPayment CustomPayment { get; set; }

        [JsonProperty("payment_instrument_token")]
        public string PaymentInstrumentToken { get; set; }

        [JsonProperty("avs_result")]
		[CommerceDescription(BigCommerceCaptions.AvsResult, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public AvsResult AvsResult { get; set; }

        [JsonProperty("cvv_result")]
		[CommerceDescription(BigCommerceCaptions.CvvResult, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public CvvResult CvvResult { get; set; }

        [JsonProperty("credit_card")]
		[CommerceDescription(BigCommerceCaptions.CreditCard, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public CreditCard CreditCard { get; set; }

        [JsonProperty("gift_certificate")]
		[CommerceDescription(BigCommerceCaptions.GiftCertificate, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public GiftCertificate GiftCertificate { get; set; }

        [JsonProperty("store_credit")]
		[CommerceDescription(BigCommerceCaptions.StoreCredit, FieldFilterStatus.Skipped, FieldMappingStatus.ImportAndExport)]
		public StoreCredit StoreCredit { get; set; }

		[JsonIgnore]
		public string OrderPaymentMethod { get; set; }
	}
}
