using Newtonsoft.Json;

namespace AH.Objects.AHIG
{
    [JsonObject]
    public class JsonValues
    {
        [JsonProperty("entity_id")]
        public string EntityID;
        
        [JsonProperty("source_type")]
        public string SourceType;

        [JsonProperty("latitude")]
        public decimal? Latitude;
        
        [JsonProperty("longitude")]
        public decimal? Longitude;
    }
}