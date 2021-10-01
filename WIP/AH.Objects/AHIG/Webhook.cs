using PX.Data;
using PX.Data.Webhooks;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Collections.Generic;
using AH.Objects.AHIG.DAC;

namespace AH.Objects.AHIG
{
    public class SurveyWebhookServerHandler : IWebhookHandler
    {
        private NameValueCollection _queryParameters;
        private const string cDeviceId = "DeviceID";
        private const string cPayloadType = "PayloadType";
        private const string cTodoPurgeAsNolongerNeeded = "Mode";
        private const string cLife330_PayloadType = "Life360";
        private const string cTile = "Tile";


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        async Task<IHttpActionResult> IWebhookHandler.ProcessRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            using (var scope = GetUserScope())
            {
                _queryParameters = HttpUtility.ParseQueryString(request.RequestUri.Query);
                //when we get into anonymous surveys we will likely point to a get a Survey ID for those as the collector will not yet exist 
                if (!_queryParameters.AllKeys.Contains(cDeviceId))
                    throw new Exception($"The {cDeviceId} Parameter was not specified in the Query String");
                var deviceId = _queryParameters[cDeviceId];
                CreateDeviceRecordIfItDoesNotYetExist(deviceId);
                
                
                if (!_queryParameters.AllKeys.Contains(cPayloadType))
                    throw new Exception($"The {cPayloadType} Parameter was not specified in the Query String");
                var payloadType =  _queryParameters[cPayloadType];

                var payLoadBody = request.Content.ReadAsStringAsync().Result;
                IoTDevicePayloadData dacPayload = CreateAndSavePayload(deviceId,payloadType,_queryParameters.ToString(),payLoadBody);
                HtmlActionResult resultsAsHtml = ProcessPayloadData(dacPayload);
                return resultsAsHtml;
                
            }
        }

        private void CreateDeviceRecordIfItDoesNotYetExist(string deviceName)
        {
            //todo: do a lookup for the device by device name
            //todo: if (the results above returns null)
            //{
                    //Auto create the device record.
            //}
        }

        private HtmlActionResult ProcessPayloadData(IoTDevicePayloadData payload)
        {
            string htmlResponse;
            switch (payload.PayloadType)
            {
                case cLife330_PayloadType:
                    IoTProcessorGenericPayload life360Payload = new IoTProcessorGenericPayload(payload);
                    htmlResponse = life360Payload.Process();
                    break;
                case cTile:
                    IoTProcessorTile tile = new IoTProcessorTile(payload);
                    htmlResponse = tile.Process();
                    break;
                //case cForceSensor:
                //    IoTProcessorForceSensor forceSensor = new IoTProcessorForceSensor(payload);
                //    htmlResponse = forceSensor.Process();
                //    break;
                //case cUltrasonicSensor:
                //    IoTProcessorForceNewSensor newSensor = new IoTProcessorForceSensor(payload);
                //    htmlResponse = newSensor.Process();
                //    break;
                //case cTemperatureSensor:
                //    IoTProcessorForceNewSensor newSensor = new IoTProcessorForceSensor(payload);
                //    htmlResponse = newSensor.Process();
                //    break;
                //case cHumiditySensor:
                //    IoTProcessorForceNewSensor newSensor = new IoTProcessorForceSensor(payload);
                //    htmlResponse = newSensor.Process();
                //    break;
                //case cVolitileOrganicCompoundSensor:
                //    IoTProcessorForceNewSensor newSensor = new IoTProcessorForceSensor(payload);
                //    htmlResponse = newSensor.Process();
                //    break;
                //case cTotalVolatileOrganicSensor:
                //    IoTProcessorForceNewSensor newSensor = new IoTProcessorForceSensor(payload);
                //    htmlResponse = newSensor.Process();
                //    break;
                //case ceCO2Sensor:
                //    IoTProcessorForceNewSensor newSensor = new IoTProcessorForceSensor(payload);
                //    htmlResponse = newSensor.Process();
                //    break;
                //case ceNFCScanSensor:
                //    IoTProcessorForceNewSensor newSensor = new IoTProcessorForceSensor(payload);
                //    htmlResponse = newSensor.Process();
                //    break;

                default:
                    IoTProcessorGenericPayload genericPayload = new IoTProcessorGenericPayload(payload);
                    htmlResponse = genericPayload.Process();
                    break;
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(htmlResponse)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return new HtmlActionResult(htmlResponse);
        }

        private IoTDevicePayloadData CreateAndSavePayload(string deviceName, string payloadType, string queryParameters, string payLoadBody)
        {
            var devGraph = PXGraph.CreateInstance<IotDeviceMaint>();
            var dacPayload = new IoTDevicePayloadData
            {
                DeviceCD = deviceName,
                PayloadType = payloadType,
                Payload = payLoadBody,
                QueryParameters = queryParameters
            };
            var newPayload = devGraph.PayLoadView.Insert(dacPayload);
            devGraph.Persist();
            return newPayload;
        }

        private string ReturnModeNotRecognized(string sMode)
        {
            var view = @"
<!DOCTYPE html>
<html>
<body>
<h1>Mode {0} Not Recognized</h1>
</body>
</html>
";

            return String.Format(view, sMode);
        }

        private string SubmitSurvey(string collectorToken, HttpRequestMessage request)
        {
            var body = request.Content.ReadAsStringAsync().Result;
            var uri = request.RequestUri;
            var props = request.Properties;
            //var ipAddress = GetIPAddress(request);
            SaveSurveySubmission(
                collectorToken,
                body,
                uri,
                props);
            //todo: Theoretically this html template below would be stored on and configurable on the 
            //      survey record itself. it could then be modified as needed to bedizen it up to its
            //      fullest potential via anything that can be done with HTML5

            var view = @"
<!DOCTYPE html>
<html>
<body>
<h1>Survey {2} {0}</h1>
Thank You Your Submitted your answer was {1}
</body>
</html>
";
            return string.Format(view, collectorToken, body, cDeviceId);
        }

        //public static string GetIPAddress(HttpRequestMessage request) {
        //    var props = request.Properties;
        //    if (props.ContainsKey("MS_HttpContext")) {
        //        return ((HttpContextWrapper)props["MS_HttpContext"]).Request.UserHostAddress;
        //    } else if (props.ContainsKey(RemoteEndpointMessageProperty.Name)) {
        //        var prop = (RemoteEndpointMessageProperty)props[RemoteEndpointMessageProperty.Name];
        //        return prop.Address;
        //    } else {
        //        return null;
        //    }
        //}

        private void SaveSurveySubmission(string collectorToken, string payload, Uri uri, IDictionary<string, object> props)
        {
            //var graph = PXGraph.CreateInstance<SurveyCollectorMaint>();
            //var queryParams = props != null ? JsonConvert.SerializeObject(props) : null;
            //var data = new SurveyCollectorData
            //{
            //    CollectorToken = collectorToken,
            //    Uri = uri.ToString(),
            //    Payload = payload,
            //    QueryParameters = queryParams
            //};
            //var inserted = graph.CollectedAnswers.Insert(data);
            //graph.Persist();
        }

        private string GetSurvey(string collectorToken)
        {

            //todo: We will need to dig into the HTML attributes needed to send the results directly to the URi we need to go to 
            //      We should also be able to find flags to send the answers in the body as opposed to Query parameters which 
            //      I believe this will do.

            //todo: we need to dynamically get the current endpoint out of the request as to be able to correctly parse the 
            //      desired redirect.
            //      For Prototyping Purposes we will use a hard coded value. We will obviously want to look this up or reference the webhook record in Survey preferences 
            //      once we get rolling
            var listningEndPoint = "https://desktop-vm0inj5/SUV20_104_0012/Webhooks/Company/176e3e36-c871-4827-810a-ccd04e6177e3";


            //todo: if the survey has already been awnsered or expired for this collector we need to pass back an alternate to indicate so to the 
            //      user who  clicked the link.

            var submitUrl = $"{listningEndPoint}?{cDeviceId}={collectorToken}&{cTodoPurgeAsNolongerNeeded}={cTodoPurgeAsNoLongerNeededcSubmitSurveyMode}";

            //todo: Theoretically this html template below would be stored on and configurable on the 
            //      survey record itself. it could then be modified as needed to bedizen it up to its
            //      fullest potential via anything that can be done with HTML5


            //todo: another ideal feature is we should implement a action on the survey record that will auto compile 
            //      a basic html survey as to provide a starting point for any further refinement.


            //todo: purge this out when the sample no longer works 
            var view = @"
<!DOCTYPE html>
<html>
<body>
<h1>Survey For Collector ID {0}</h1>
<form action=""{1}""  method=""post"">
                <input type=""checkbox"" id=""COVSYMPTOM"" name=""COVSYMPTOM"" value=""YES"">
                <label for= ""COVSYMPTOM""> Are you experiencing any of these symptoms? (fever, cough, shortness of breath, sore throat or diarrhea)</label><br>
                <label for= ""COVCONTACT""> Have you had any contact with individuals diagnosed with COVID-19?</label><br>
                <select id = ""COVCONTACT"" name = ""COVCONTACT"">
                    <option value = ""NO"">NO</option>
                    <option value = ""YES"">YES</option>
                </select><br>
                <label for= ""COVTEMP"">Self Temperature</label>
                <input type =""number"" id = ""COVTEMP"" name = ""COVTEMP"" min =""96"" max = ""105""><br>

                <label for=""COVTRAVEL"">What Locations Have you Traveled to ?</label><br>
                <input type = ""text"" id = ""COVTRAVEL"" name = ""COVTRAVEL"" value = """" ><br><br>
                <input type =""submit"" value =""Submit"">
</form>
</body>
</html>
";

            return String.Format(view, collectorToken, submitUrl);
        }


        /// <summary>
        /// Defines the LoginScope to be used for the WebHooks
        /// </summary>
        /// <returns></returns>
        private IDisposable GetUserScope()
        {

            //todo: For now we will use admin but we will want to throttle back to a 
            //      user with restricted access as to reduce any risk of attack.
            //      perhaps this can be configured in the Surveys Preferences/Setup page.
            var userName = "admin";
            if (PXDatabase.Companies.Length > 0)
            {
                var company = PXAccess.GetCompanyName();
                if (string.IsNullOrEmpty(company))
                {
                    company = PXDatabase.Companies[0];
                }
                userName = userName + "@" + company;
            }
            return new PXLoginScope(userName);
        }

    }

    internal class IoTProcessorTile
    {
        private IoTDevicePayloadData _payload;

        public IoTProcessorTile(IoTDevicePayloadData payload)
        {
            _payload = payload;
        }

        public string Process()
        {
            //todo: parse the Jason and update records

            var resultHtml = @"
<!DOCTYPE html>
<html>
<body>
<h1>Processed {0}</h1>
</body>
</html>
";
            return string.Format(resultHtml, _payload.DeviceID, _payload.PayloadType);
        }


    }


    //pulled from the Team Beta Hackathon
    //this will likely lead to what we need for the return
    public class HtmlActionResult : IHttpActionResult
    {

        private string _view;

        public HtmlActionResult(string view)
        {
            _view = view;

        }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(_view);

            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return Task.FromResult(response);
        }

    }
}


////old webhook
//using System.IO;
//using System.Net;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Web.Http;
//using AH.Objects.AHIG.DAC;
//using PX.Data.Webhooks;
//using Newtonsoft.Json;
//using PX.Data;

//namespace AH.Objects.AHIG
//{
//    public class HomieHook2 : IWebhookHandler
//    {
//        public async Task<IHttpActionResult> ProcessRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
//        {

//            var r = request.Content.ReadAsStreamAsync().Result;
//            var s = new StreamReader(r, Encoding.UTF8);
//            var m = s.ReadToEnd();
//            var u = JsonConvert.DeserializeObject<JsonValues>(m);


//            const string htmlResponse = "success";

//            PersistValues(u);


//            return new HtmlActionResult(htmlResponse);
//            //return new ;
//        }

//        private void PersistValues(JsonValues j)
//        {
//            var entityID = "DEVICEONE";
//            var devGraph = PXGraph.CreateInstance<IotDeviceMaint>();
//            var rec = (IoTDevice)devGraph.SingleDeviceView.Select(entityID);
//            devGraph.PagePrimaryView.Current = rec;
//            var payRec = (IoTDeviceLocationBreadCrumb)devGraph.BreadCrumbView.Cache.CreateInstance();

//            payRec.DeviceCD = entityID;
//            payRec.Latitude = j.Latitude;
//            payRec.Longitude = j.Longitude;
//            devGraph.BreadCrumbView.Cache.Update(payRec);
//            devGraph.Persist();
//        }

//        //public IHttpActionResult Get()
//        //{
//        //    string myResult = "Bla";
//        //    return Ok(myResult);
//        //}

//        //pulled from the Team Beta Hackathon
//        //this will likely lead to what we need for the return
//        public class HtmlActionResult : IHttpActionResult
//        {

//#pragma warning disable IDE0044 // Add readonly modifier
//            private string _view;
//#pragma warning restore IDE0044 // Add readonly modifier

//            public HtmlActionResult(string view)
//            {
//                _view = view;

//            }

//            public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
//            {
//                var response = new HttpResponseMessage(HttpStatusCode.OK);
//                response.Content = new StringContent(_view);

//                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
//                return Task.FromResult(response);
//            }

//        }

//    }
//}