using System;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class StoreCountriesProvider : IRestDataReader<List<Countries>>
    {
        private readonly IBigCommerceRestClient _restClient;

        public StoreCountriesProvider(IBigCommerceRestClient restClient)
        {
            _restClient = restClient;
        }

        
        public List<Countries> Get()
        {
            const string resourceUrl = "v2/countries";
          
			var filter =  new Filter();
			var needGet = true;

			filter.Page = 1;
			filter.Limit = 250;
			
			List<Countries> Countries = new List<Countries>();
			while (needGet)
			{
				var request = _restClient.MakeRequest(resourceUrl);
				filter?.AddFilter(request);
				var country = _restClient.Get<List<Countries>>(request);
				Countries.AddRange(country);
				if(country.Count<250) needGet = false;
				filter.Page++;
			}
			
            return Countries;
        }

		
	}
}