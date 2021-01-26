using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Models
{
    [Flags]
    public enum Responsibilities
    {
        None                  = 0b00000000000000000,
        ItLeadership          = 0b00000000000000001,
        BizSysAnalysis        = 0b00000000000000010,
        DataAdminAnalysis     = 0b00000000000000100,
        DatabaseArchDesign    = 0b00000000000001000,
        InstructionalTech     = 0b00000000000010000,
        ItProjectMgt          = 0b00000000000100000,
        ItSecurityPrivacy     = 0b00000000001000000,
        ItUserSupport         = 0b00000000010000000,
        ItMultiDiscipline     = 0b00000000100000000,
        Networks              = 0b00000001000000000,
        SoftwareAdminAnalysis = 0b00000010000000000,
        SoftwareDevEng        = 0b00000100000000000,
        SystemDevEng          = 0b00001000000000000,
        UserExperience        = 0b00010000000000000,
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