using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Models
{
    [Flags]
    public enum Responsibilities
    {
        [Display(Name="None")]
        None                  = 0b00000000000000000,

        [Display(Name="IT Leadership")]
        ItLeadership          = 0b00000000000000001,
        
        [Display(Name="Business System Analysis")]
        BizSysAnalysis        = 0b00000000000000010,
        [Display(Name="Data Administration/Analysis")]

        DataAdminAnalysis     = 0b00000000000000100,
        [Display(Name="Database Architecture/Design")]
        DatabaseArchDesign    = 0b00000000000001000,
        [Display(Name="Instructional Technology")]
        InstructionalTech     = 0b00000000000010000,
        [Display(Name="IT Project Management")]
        ItProjectMgt          = 0b00000000000100000,
        [Display(Name="IT Security/Privacy")]
        ItSecurityPrivacy     = 0b00000000001000000,
        [Display(Name="IT User Support")]
        ItUserSupport         = 0b00000000010000000,
        [Display(Name="IT Multiple Discipline")]
        ItMultiDiscipline     = 0b00000000100000000,
        [Display(Name="Networks")]
        Networks              = 0b00000001000000000,
        [Display(Name="Software Administration/Analysis")]
        SoftwareAdminAnalysis = 0b00000010000000000,
        [Display(Name="Software Developer/Engineer")]
        SoftwareDevEng        = 0b00000100000000000,
        [Display(Name="Systems Developer/Engineer")]
        SystemDevEng          = 0b00001000000000000,
        [Display(Name="User Experience")]
        UserExperience        = 0b00010000000000000,
        [Display(Name="Web Developer/Engineer")]
        WebAdminDevEng        = 0b00100000000000000,
    }


    [JsonConverter(typeof(StringEnumConverter))]
    public enum ResponsibilitiesPropDoc
    {
        None                  = Responsibilities.None,
        ItLeadership          = Responsibilities.ItLeadership,
        BizSysAnalysis        = Responsibilities.BizSysAnalysis,
        DataAdminAnalysis     = Responsibilities.DataAdminAnalysis,
        DatabaseArchDesign    = Responsibilities.DatabaseArchDesign,
        InstructionalTech     = Responsibilities.InstructionalTech,
        ItProjectMgt          = Responsibilities.ItProjectMgt,
        ItSecurityPrivacy     = Responsibilities.ItSecurityPrivacy,
        ItUserSupport         = Responsibilities.ItUserSupport,
        ItMultiDiscipline     = Responsibilities.ItMultiDiscipline,
        Networks              = Responsibilities.Networks,
        SoftwareAdminAnalysis = Responsibilities.SoftwareAdminAnalysis,
        SoftwareDevEng        = Responsibilities.SoftwareDevEng,
        SystemDevEng          = Responsibilities.SystemDevEng,
        UserExperience        = Responsibilities.UserExperience,
        WebAdminDevEng        = Responsibilities.WebAdminDevEng 
    }
}