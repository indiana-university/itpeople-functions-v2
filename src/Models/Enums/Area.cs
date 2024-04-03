using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;


namespace Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Area
    {
        
        /// University Information Technology Services (UITS) IT units are part of IU's central tech and support organization.
        [Display(Name="UITS Units")]
        Uits=1,
        /// Edge IT units are integrated directly into colleges and academic departments.
        [Display(Name="Edge Units")]
        Edge=2
    }

}