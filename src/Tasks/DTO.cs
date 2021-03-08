using System.Collections.Generic;
using System.Linq;
using Models;
using Newtonsoft.Json;

namespace Tasks
{
     public class DenodoResponse<T>
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("elements")] public IEnumerable<T> Elements { get; set; }
    }

    // BUILDINGS

    public class DenodoBuilding
    {
        [JsonProperty("building_name")] public string Name { get; set; }
        [JsonProperty("building_code")] public string BuildingCode { get; set; }
        [JsonProperty("site_code")] public string SiteCode { get; set; }
        [JsonProperty("building_long_description")] public string Description { get; set; }
        [JsonProperty("street")] public string Street { get; set; }
        [JsonProperty("city")] public string City { get; set; }
        [JsonProperty("state")] public string State { get; set; }
        [JsonProperty("zip")] public string Zip { get; set; }

        public void MapToBuilding(Building record)
        {
            record.Name = Description;
            record.Code = BuildingCode;
            record.Address = Street;
            record.City = City;
            record.State = State;
            record.PostCode = Zip;
            record.Country = "";
        }
    }

}
