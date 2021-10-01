using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PX.Commerce.Core;
using PX.Commerce.Core.Model;
using PX.Common;
using PX.Data;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Deserializers;
using RestSharp.Serializers;

namespace PX.Commerce.Shopify.API.REST
{
	public abstract class SPRestClientBase : RestClient
	{
		private const string COMMERCE_SHOPIFY_MAX_ATTEMPTS = "CommerceShopifyMaxApiCallAttempts";
		private const string COMMERCE_SHOPIFY_DELAY_TIME_IF_FAILED = "CommerceShopifyApiCallDelayTimeIfFailed";
		private static ConcurrentDictionary<string, LeakyController> controllers = new ConcurrentDictionary<string, LeakyController>();
		private readonly string apiIdentifyId;
		private readonly int maxAttemptRecallAPI = WebConfig.GetInt(COMMERCE_SHOPIFY_MAX_ATTEMPTS, 1000);
		private readonly int delayApiCallTime = WebConfig.GetInt(COMMERCE_SHOPIFY_DELAY_TIME_IF_FAILED, 500); //500ms
		protected ISerializer _serializer;
		protected IDeserializer _deserializer;
		public Serilog.ILogger Logger { get; set; } = null;
		protected SPRestClientBase(IDeserializer deserializer, ISerializer serializer, IRestOptions options, Serilog.ILogger logger)
		{
			_serializer = serializer;
			_deserializer = deserializer;
			AddHandler("application/json", deserializer);
			AddHandler("text/json", deserializer);
			AddHandler("text/x-json", deserializer);
			Authenticator = new HttpBasicAuthenticator(options.XAuthClient, options.XAuthTocken);
			apiIdentifyId = options.XAuthClient;
			try
			{
				BaseUrl = new Uri(options.BaseUri);
			}
			catch (UriFormatException e)
			{
				throw new UriFormatException("Invalid URL: The format of the URL could not be determined.", e);
			}
			Logger = logger;
		}

		public RestRequest MakeRequest(string url, Dictionary<string, string> urlSegments = null)
		{
			var request = new RestRequest(url) { JsonSerializer = _serializer, RequestFormat = DataFormat.Json };

			if (urlSegments != null)
			{
				foreach (var urlSegment in urlSegments)
				{
					request.AddUrlSegment(urlSegment.Key, urlSegment.Value);
				}
			}

			return request;
		}

		protected IRestResponse ExecuteRequest(IRestRequest request)
		{
			var leakyController = controllers.GetOrAdd(apiIdentifyId, _ => new LeakyController());
			IRestResponse response = null;
			if (leakyController != null)
			{
				int attemptRecallAPI = 1;
				while (attemptRecallAPI <= maxAttemptRecallAPI)
				{

					leakyController.GrantAccess();
					try
					{
						response = Execute(request);
						CheckResponse(response);
						leakyController.UpdateController(response.Headers);
						return response;
					}
					catch (RestShopifyApiCallLimitException ex)
					{
						attemptRecallAPI++;
						Task.Delay(delayApiCallTime);
					}
				}
			}
			throw new Exception(ShopifyMessages.TooManyApiCalls);
		}

		protected IRestResponse<TR> ExecuteRequest<TR>(IRestRequest request) where TR : class, new()
		{
			var leakyController = controllers.GetOrAdd(apiIdentifyId, _ => new LeakyController());
			IRestResponse<TR> response = null;
			if (leakyController != null)
			{
				int attemptRecallAPI = 1;
				while (attemptRecallAPI <= maxAttemptRecallAPI)
				{

					leakyController.GrantAccess();
					try
					{
						response = Execute<TR>(request);
						CheckResponse(response);
						leakyController.UpdateController(response.Headers);
						return response;
					}
					catch(RestShopifyApiCallLimitException ex)
					{
						attemptRecallAPI++;
						Task.Delay(delayApiCallTime);
					}
				}
			}
			throw new PXException(ShopifyMessages.TooManyApiCalls);
		}

		protected void LogError(Uri baseUrl, IRestRequest request, IRestResponse response)
		{
			//Get the values of the parameters passed to the API
			var parameters = string.Join(", ", request.Parameters.Select(x => x.Name.ToString() + "=" + (x.Value ?? "NULL")).ToArray());

			//Set up the information message with the URL, the status code, and the parameters.
			var info = "Request to " + baseUrl.AbsoluteUri + request.Resource + " failed with status code " + response.StatusCode + ", parameters: " + parameters;
			var description = "Response content: " + response.Content;

			//Acquire the actual exception
			var ex = (response.ErrorException?.Message) ?? info;

			//Log the exception and info message
			Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.Error(response.ErrorException, "{CommerceCaption}: {ResponseError}", BCCaptions.CommerceLogCaption, description);
		}

		protected void CheckResponse(IRestResponse response)
		{
			if(!string.IsNullOrEmpty(response?.StatusCode.ToString()) && int.TryParse(response?.StatusCode.ToString(), out var intCode) && intCode == 429)
			{
				throw new RestShopifyApiCallLimitException(response);
			}
		}
	}
}