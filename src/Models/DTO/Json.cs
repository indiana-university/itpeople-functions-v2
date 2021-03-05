using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Models
{
    public static class Json
    {
        public static JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,     
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy(),
                }
            };
    }
}