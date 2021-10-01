using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PX.Commerce.Shopify.API.REST;
using PX.Api;
using PX.Data;
using RestSharp;
using PX.Commerce.Core;
using PX.Objects.CA;
using PX.Commerce.Objects;
using PX.Objects.GL;
using PX.Objects.CS;

namespace PX.Commerce.Shopify
{
	public class BCShopifyStoreMaint : BCStoreMaint
	{
		public PXSelect<BCBindingShopify, Where<BCBindingShopify.bindingID, Equal<Current<BCBinding.bindingID>>>> CurrentBindingShopify;

		public BCShopifyStoreMaint()
		{
			base.Bindings.WhereAnd<Where<BCBinding.connectorType, Equal<SPConnector.spConnectorType>>>();

			PXStringListAttribute.SetList<BCBindingExt.visibility>(base.CurrentStore.Cache, null,
				new[]
				{
					BCItemVisibility.Visible,
					BCItemVisibility.Invisible,
				},
				new[]
				{
					BCCaptions.Visible,
					BCCaptions.Invisible,
				});
		}


		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Shopify Location")]
		[PXDefault(PersistingCheck = PXPersistingCheck.NullOrBlank)]
		public virtual void ExportBCLocations_ExternalLocationID_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Shopify Location")]
		[PXDefault(PersistingCheck = PXPersistingCheck.NullOrBlank)]
		public virtual void ImportBCLocations_ExternalLocationID_CacheAttached(PXCache sender) { }

		#region Actions
		public PXAction<BCBinding> TestConnection;
		[PXButton]
		[PXUIField(DisplayName = "Test Connection", Enabled = false)]
		protected virtual IEnumerable testConnection(PXAdapter adapter)
		{
			Actions.PressSave();

			BCBinding binding = Bindings.Current;
			BCBindingShopify bindingShopify = CurrentBindingShopify.Current ?? CurrentBindingShopify.Select();

			if (binding.ConnectorType != SPConnector.TYPE) return adapter.Get();
			if (binding == null || bindingShopify == null || bindingShopify.ShopifyApiBaseUrl == null 
				|| string.IsNullOrEmpty(bindingShopify.ShopifyApiKey) || string.IsNullOrEmpty(bindingShopify.ShopifyApiPassword))
			{
				throw new PXException(BCMessages.TestConnectionFailedParameters);
			}

			PXLongOperation.StartOperation(this, delegate
			{
				BCShopifyStoreMaint graph = PXGraph.CreateInstance<BCShopifyStoreMaint>();
				graph.Bindings.Current = binding;
				graph.CurrentBindingShopify.Current = bindingShopify;

				StoreRestDataProvider restClient = new StoreRestDataProvider(SPConnector.GetRestClient(bindingShopify));
				try
				{
					var store = restClient.Get();
					if (store == null || store.Id == null)
						throw new PXException(ShopifyMessages.TestConnectionStoreNotFound);

					graph.CurrentBindingShopify.Cache.SetValueExt(binding, nameof(BCBindingShopify.ShopifyStoreUrl), store.Domain);
					graph.CurrentBindingShopify.Cache.SetValueExt(binding, nameof(BCBindingShopify.ShopifyDefaultCurrency), store.Currency);
					graph.CurrentBindingShopify.Cache.SetValueExt(binding, nameof(BCBindingShopify.ShopifySupportCurrencies), string.Join(",", store.EnabledPresentmentCurrencies));
					graph.CurrentBindingShopify.Cache.SetValueExt(binding, nameof(BCBindingShopify.ShopifyStoreTimeZone), store.Timezone);
					graph.CurrentBindingShopify.Update(bindingShopify);

					graph.Persist();
				}
				catch (Exception ex)
				{
					throw new PXException(ex, BCMessages.TestConnectionFailedGeneral, ex.Message);
				}
			});

			return adapter.Get();
		}
		#endregion

		#region BCBinding Events
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(BCConnectorsAttribute), "DefaultConnector", SPConnector.TYPE)]
		public virtual void _(Events.CacheAttached<BCBinding.connectorType> e) { }

		public override void _(Events.RowSelected<BCBinding> e)
		{
			base._(e);

			BCBinding row = e.Row as BCBinding;
			if (row == null) return;

			//Actions
			TestConnection.SetEnabled(row.BindingID > 0 && row.ConnectorType == SPConnector.TYPE);
		}
		public override void _(Events.RowSelected<BCBindingExt> e)
        {
			base._(e);

			BCBindingExt row = e.Row as BCBindingExt;
			if (row == null) return;

			PXStringListAttribute.SetList<BCBindingExt.availability>(e.Cache, row, new[] {
					BCItemAvailabilities.AvailableTrack,
					BCItemAvailabilities.AvailableSkip,
					BCItemAvailabilities.DoNotUpdate,
					BCItemAvailabilities.Disabled,
				},
				new[]
				{
					BCCaptions.AvailableTrack,
					BCCaptions.AvailableSkip,
					BCCaptions.DoNotUpdate,
					BCCaptions.Disabled,
				});

			PXStringListAttribute.SetList<BCBindingExt.notAvailMode>(e.Cache, row, new[] {
					BCItemNotAvailModes.DoNothing,
					BCItemNotAvailModes.DisableItem,
				},
				new[]
				{
					BCCaptions.DoNothing,
					BCCaptions.DisableItem,
				});

		}

		public virtual void _(Events.RowSelected<BCBindingShopify> e)
		{
			BCBindingShopify row = e.Row;
			if (row == null) return;

			if(PXAccess.FeatureInstalled<FeaturesSet.shopifyPOS>() && row.ShopifyPOS == true && Entities.Select().RowCast<BCEntity>()?.FirstOrDefault(x => x.EntityType == BCEntitiesAttribute.Order)?.IsActive == true)
			{
				PXUIFieldAttribute.SetRequired<BCBindingShopify.pOSDirectOrderType>(e.Cache, true);
				PXUIFieldAttribute.SetRequired<BCBindingShopify.pOSShippingOrderType>(e.Cache, true);
				PXDefaultAttribute.SetPersistingCheck<BCBindingShopify.pOSDirectOrderType>(e.Cache, e.Row, PXPersistingCheck.NullOrBlank);
				PXDefaultAttribute.SetPersistingCheck<BCBindingShopify.pOSShippingOrderType>(e.Cache, e.Row, PXPersistingCheck.NullOrBlank);
			}
			else
			{
				PXUIFieldAttribute.SetRequired<BCBindingShopify.pOSDirectOrderType>(e.Cache, false);
				PXUIFieldAttribute.SetRequired<BCBindingShopify.pOSShippingOrderType>(e.Cache, false);
				PXDefaultAttribute.SetPersistingCheck<BCBindingShopify.pOSDirectOrderType>(e.Cache, e.Row, PXPersistingCheck.Nothing);
				PXDefaultAttribute.SetPersistingCheck<BCBindingShopify.pOSShippingOrderType>(e.Cache, e.Row, PXPersistingCheck.Nothing);
			}
		}

		public override void _(Events.RowInserted<BCBinding> e)
		{
			base._(e);

			bool dirty = CurrentBindingShopify.Cache.IsDirty;
			CurrentBindingShopify.Insert();
			CurrentBindingShopify.Cache.IsDirty = dirty;
		}
		protected virtual void _(Events.RowPersisting<BCBindingShopify> e)
		{
			BCBindingShopify row = e.Row as BCBindingShopify;
			FetchDataFromShopify(row);
		}

		protected virtual void _(Events.FieldVerifying<BCBindingShopify, BCBindingShopify.shopifyApiBaseUrl> e)
		{
			string val = e.NewValue?.ToString();
			if (val != null)
			{
				if (!val.EndsWith("/")) val += "/";
				if (val.ToLower().EndsWith(".myshopify.com/")) val += "admin/";
				if (!val.ToLower().EndsWith("/admin/"))
				{
					throw new PXSetPropertyException(ShopifyMessages.InvalidStoreUrl, PXErrorLevel.Warning);
				}
				e.NewValue = val;
			}
		}

		public override void _(Events.FieldUpdated<BCEntity, BCEntity.isActive> e)
		{
			base._(e);

			BCEntity row = e.Row;
			if (row == null || row.CreatedDateTime == null) return;

			if (row.IsActive == true)
			{
				if (row.EntityType == BCEntitiesAttribute.ProductWithVariant)
					if (PXAccess.FeatureInstalled<FeaturesSet.matrixItem>() == false)
					{
						EntityReturn(row.EntityType).IsActive = false;
						e.Cache.Update(EntityReturn(row.EntityType));
						throw new PXSetPropertyException(BCMessages.MatrixFeatureRequired);
					}
			}
		}

		protected void FetchDataFromShopify(BCBindingShopify row)
		{
			if (row == null || string.IsNullOrEmpty(row.ShopifyApiBaseUrl) || string.IsNullOrWhiteSpace(row.ShopifyApiKey) || string.IsNullOrWhiteSpace(row.ShopifyApiPassword))
				return;

			StoreRestDataProvider restClient = new StoreRestDataProvider(SPConnector.GetRestClient(row.ShopifyApiBaseUrl, row.ShopifyApiKey, row.ShopifyApiPassword, row.StoreSharedSecret, row.ApiCallLimit));
			try
			{
				var store = restClient.Get();
				//row.ShopifyStoreUrl = store.Domain;
				//row.ShopifyDefaultCurrency = store.Currency;
				//row.ShopifySupportCurrencies = string.Join(",", store.EnabledPresentmentCurrencies);
				//row.ShopifyStoreTimeZone = store.Timezone;
				CurrentBindingShopify.Cache.SetValueExt(row, nameof(row.ShopifyStoreUrl), store.Domain);
				CurrentBindingShopify.Cache.SetValueExt(row, nameof(row.ShopifyDefaultCurrency), store.Currency);
				CurrentBindingShopify.Cache.SetValueExt(row, nameof(row.ShopifySupportCurrencies), string.Join(",", store.EnabledPresentmentCurrencies));
				CurrentBindingShopify.Cache.SetValueExt(row, nameof(row.ShopifyStoreTimeZone), store.Timezone);
				CurrentBindingShopify.Cache.IsDirty = true;
				CurrentBindingShopify.Cache.Update(row);
			}
			catch (Exception ex)
			{
				//throw new PXException(ex.Message);
			}
		}
		#endregion
	}
}