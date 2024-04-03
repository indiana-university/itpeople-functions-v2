using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Role
    {
        /// This person has an ancillary relationship to this unit. This can apply to administrative assistants or self-supporting faculty.
        Related=1,
        /// This person is a regular member of this unit.
        Member=2,
        /// This person has some delegated authority within this unit. 
        Sublead=3,
        /// This person has primary responsibility for and authority over this unit. 
        Leader=4
    }
}
