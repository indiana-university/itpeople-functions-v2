using System.Collections.Generic;
using Newtonsoft.Json;

namespace Models
{
    public class DenodoResponse<T>
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("elements")] public IEnumerable<T> Elements { get; set; }
    }
    
}