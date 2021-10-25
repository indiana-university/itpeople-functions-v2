using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Models
{
     [Flags]    
    public enum Area
    { 
        
        /// University Information Technology Services (UITS) IT units are part of IU's central tech and support organization.
        [Display(Name="UITS Units")]
        Uits    =   0b00000000000000001,
        /// Edge IT units are integrated directly into colleges and academic departments.
        [Display(Name="Edge Units")]
        Edge    =   0b00000000000000010

    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum AreaPropDoc
    {
        Uits    =   Area.Uits,
        Edge    =   Area.Edge
    }

}