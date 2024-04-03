using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Campus
    { 
        [Display(Name="Bloomington")]
        BL  =   1,
        [Display(Name="IUPUI (Indianapolis)")]
        IN  =   2, 
        [Display(Name="IUPUC (Columbus)")]
        CO  =   3,
        [Display(Name="East (Richmond)")]
        EA  =   4,
        [Display(Name="Fort Wayne")]
        FW  =   5,
        [Display(Name="Kokomo")]
        KO  =   6,
        [Display(Name="Northwest (Gary)")]
        NW  =   7,
        [Display(Name="South Bend")]
        SB  =   8,
        [Display(Name="Southeast (New Albany)")]
        SE  =   9
    }
}