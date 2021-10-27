using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;


namespace Models
{
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Role
    {
        [Display(Name = "Related")]
        Related = 0b0001,

        [Display(Name = "Member")]
        Member = 0b0010,
        [Display(Name = "Sublead")]
        Sublead = 0b0100,

        [Display(Name = "Leader")]
        Leader = 0b1000,
    }
}