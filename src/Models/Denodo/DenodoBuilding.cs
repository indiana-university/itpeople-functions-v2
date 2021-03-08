using Newtonsoft.Json;

namespace Models
{
    public class DenodoBuilding
    {
        [JsonProperty("building_code")] public string BuildingCode { get; set; }
        [JsonProperty("site_code")] public string SiteCode { get; set; }
        [JsonProperty("building_name")] public string Name { get; set; }
        [JsonProperty("building_long_description")] public string Description { get; set; }
        [JsonProperty("street")] public string Street { get; set; }
        [JsonProperty("city")] public string City { get; set; }
        [JsonProperty("state")] public string State { get; set; }
        [JsonProperty("zip")] public string Zip { get; set; }
    }
    
}