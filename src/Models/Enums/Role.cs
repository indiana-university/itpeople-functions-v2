using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;


namespace Models
{
    [Flags]
    public enum Role
    {
        [Display(Name="Related")]
        Related     = 0b00000000000000001,
        
        [Display(Name="Member")]
        Member      = 0b00000000000000010,
        [Display(Name="Sublead")]

        Sublead     = 0b00000000000000100,
        
        [Display(Name="Leader")]

        Leader      = 0b00000000000001000,
        
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RolePropDoc
    {
        /// This person has an ancillary relationship to this unit. This can apply to administrative assistants or self-supporting faculty.
        Related = Role.Related,
        /// This person is a regular member of this unit.
        Member = Role.Member,
        /// This person has some delegated authority within this unit. 
        Sublead = Role.Sublead,
        /// This person has primary responsibility for and authority over this unit. 
        Leader = Role.Leader
    }
}