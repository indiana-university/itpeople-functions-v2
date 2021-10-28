using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Models
{
    [Flags]    
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Campus
    { 
        [Display(Name="Bloomington")]
        BL  =   0b0000000001,
        [Display(Name="IUPUI (Indianapolis)")]
        IN  =   0b0000000010, 
        [Display(Name="IUPUC (Columbus)")]
        CO  =   0b000000100,
        [Display(Name="East (Richmond)")]
        EA  =   0b000001000,
        [Display(Name="Fort Wayne")]
        FW  =   0b000010000,
        [Display(Name="Kokomo")]
        KO  =   0b000100000,
        [Display(Name="Northwest (Gary)")]
        NW  =   0b001000000,
        [Display(Name="South Bend")]
        SB  =   0b010000000,
        [Display(Name="Southeast (New Albany)")]
        SE  =   0b100000000
    }
}