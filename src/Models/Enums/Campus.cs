using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Models
{
    [Flags]    
    public enum Campus
    { 
        [Display(Name="Bloomington")]
        BL  =   0b00000000000000001,
        [Display(Name="IUPUI (Indianapolis)")]
        IN  =   0b00000000000000010, 
        [Display(Name="IUPUC (Columbus)")]
        CO  =   0b00000000000000100,
        [Display(Name="East (Richmond)")]
        EA  =   0b00000000000001000,
        [Display(Name="Fort Wayne")]
        FW  =   0b00000000000010000,
        [Display(Name="Kokomo")]
        KO  =   0b00000000000100000,
        [Display(Name="Northwest (Gary)")]
        NW  =   0b00000000001000000,
        [Display(Name="South Bend")]
        SB  =   0b00000000010000000,
        [Display(Name="Southeast (New Albany)")]
        SE  =   0b00000000100000000
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CampusPropDoc
    {
        BL  =   Campus.BL,
        IN  =   Campus.IN, 
        CO  =   Campus.CO,
        EA  =   Campus.EA,
        FW  =   Campus.FW,
        KO  =   Campus.KO,
        NW  =   Campus.NW,
        SB  =   Campus.SB,
        SE  =   Campus.SE        
    }

}