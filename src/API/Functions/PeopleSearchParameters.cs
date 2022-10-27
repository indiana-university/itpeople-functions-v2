using Microsoft.Azure.WebJobs.Extensions.Http;
using CSharpFunctionalExtensions;
using API.Middleware;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Models;
using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace API.Functions
{
    public class BaseSearchParameters
    {
        public string Q { get; }
        public BaseSearchParameters(string q)
        {
            Q = q;
        }

        protected static T[] ParseEnumList<T>(string str, string typeDescription) where T : struct, IComparable 
            => string.IsNullOrWhiteSpace(str)
                ? new T[0]
                : ParseCommaSeparatedList(str)
                    .Select(r =>
                    {
                        if (!Enum.TryParse<T>(r, true, out T value))
                            throw new Exception($"{typeDescription} not recognized: '{r}'");
                        return value;
                    })
                    .ToArray();

        // " ,  ,"  😩  => [" ", "  "]
        protected static string[] ParseCommaSeparatedList(string str) 
            => string.IsNullOrWhiteSpace(str)
                ? new string[0]
                : str.Split(",", StringSplitOptions.RemoveEmptyEntries)
                    .Select(i => i.Trim())
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .ToArray();

    }

    public class UnitSearchParameters : BaseSearchParameters
    {
        public UnitSearchParameters(string q) : base(q)
        {}

        public static Result<UnitSearchParameters, Error> Parse(HttpRequest req) 
        {
            var queryParms = req.GetQueryParameterDictionary();
            queryParms.TryGetValue("q", out string q);
            return Pipeline.Success(new UnitSearchParameters(q));
        }
    }

    public class BuildingSearchParameters : BaseSearchParameters
    {
        public BuildingSearchParameters(string q) : base(q)
        {}

        public static Result<BuildingSearchParameters, Error> Parse(HttpRequest req) 
        {
            var queryParms = req.GetQueryParameterDictionary();
            queryParms.TryGetValue("q", out string q);
            return Pipeline.Success(new BuildingSearchParameters(q));
        }
    }

    public class DepartmentSearchParameters : BaseSearchParameters
    {
        public int Limit { get; }

        public DepartmentSearchParameters(string q, int limit = 0) : base(q)
        {
            Limit = limit;
        }

        public static Result<DepartmentSearchParameters, Error> Parse(HttpRequest req) 
        {
            var queryParms = req.GetQueryParameterDictionary();
            queryParms.TryGetValue("q", out string q);
            
            queryParms.TryGetValue("_limit", out string limitString);
            int.TryParse(limitString, out int limit);

            return Pipeline.Success(new DepartmentSearchParameters(q, limit));
        }
    }

    public class HrPeopleSearchParameters : BaseSearchParameters
    {
        [Required(ErrorMessage = "The query parameter 'q' is required.")]
        [MinLength(3, ErrorMessage = "The query parameter 'q' must be at least {1} characters long.")]
        public new string Q { get; }

        [Range(1, 100, ErrorMessage = "The query parameter '_limit' must be an integer between 1 and 100.")]
        public int Limit { get; }

        public HrPeopleSearchParameters(string q, int limit) : base(q)
        {
            Q = q;
            Limit = limit;
        }

        public static Result<HrPeopleSearchParameters, Error> Parse(HttpRequest req) 
            => Parse(req.GetQueryParameterDictionary());
        public static Result<HrPeopleSearchParameters, Error> Parse(IDictionary<string, string> queryParms)
        {
            queryParms.TryGetValue("q", out string q);
            queryParms.TryGetValue("_limit", out string limitString);
            
            int limit = 15;
            if(string.IsNullOrWhiteSpace(limitString) == false){
                int.TryParse(limitString, out limit);
            }

            var result = new HrPeopleSearchParameters(q, limit);
            
            return Pipeline.Success(result);
        }
    }

    public class SsspSupportRelationshipParameters : BaseSearchParameters
    {
        public string DepartmentName { get; set; }
        public SsspSupportRelationshipParameters(string dept) : base(dept)
        {
            DepartmentName = dept;
        }

        public static Result<SsspSupportRelationshipParameters, Error> Parse(HttpRequest req) 
        {
            var queryParms = req.GetQueryParameterDictionary();
            queryParms.TryGetValue("dept", out string dept);
            return Pipeline.Success(new SsspSupportRelationshipParameters(dept));
        }
    }

    public class PeopleSearchParameters : BaseSearchParameters
    {

        public PeopleSearchParameters(string q, Responsibilities responsibilities, string[] expertise, string[] campus, Role[] role, UnitPermissions[] permissions, Area[] areas)
            : base(q)
        {
            Responsibilities = responsibilities;
            Expertise = expertise;
            Campus = campus;         
            Roles = role;
            Permissions = permissions;
            Areas = areas;
        }
        
        public Responsibilities Responsibilities { get; }
        public string[] Expertise { get; }
        public string[] Campus { get; }        
        public Role[] Roles { get; }
        public UnitPermissions[] Permissions { get; }
        public Area[] Areas { get; set; }

        public static Result<PeopleSearchParameters, Error> Parse(HttpRequest req) 
            => Parse(req.GetQueryParameterDictionary());

        public static Result<PeopleSearchParameters, Error> Parse(IDictionary<string, string> queryParms)
        {
            queryParms.TryGetValue("q", out string q);
            queryParms.TryGetValue("class", out string jobClass);
            queryParms.TryGetValue("role", out string role);
            queryParms.TryGetValue("interest", out string interests);
            queryParms.TryGetValue("campus", out string campus);
            queryParms.TryGetValue("permission", out string permission);
            queryParms.TryGetValue("area", out string area);
            var responsibilities = string.IsNullOrWhiteSpace(jobClass)
                ? Responsibilities.None
                : (Responsibilities)Enum.Parse(typeof(Responsibilities), jobClass); 
            var areas = ParseEnumList<Area>(area, "Unit area");
            var roles = ParseEnumList<Role>(role, "Unit role");
            var permissions = ParseEnumList<UnitPermissions>(permission, "Unit permission");
            var expertises = ParseCommaSeparatedList(interests);
            var campuses = ParseCommaSeparatedList(campus);
            var result = new PeopleSearchParameters(q, responsibilities, expertises, campuses, roles, permissions, areas);

            return Pipeline.Success(result);
        }
    }

    public class ValidReportSupportingUnitsParameters : BaseSearchParameters
    {
		public int DepartmentId { get; set; }
        public int UnitId { get; set; }
		
        public ValidReportSupportingUnitsParameters(int departmentId, int unitId) : base(string.Empty)
		{
            DepartmentId = departmentId;
            UnitId = unitId;
		}

        public static Result<ValidReportSupportingUnitsParameters, Error> Parse(HttpRequest req)
        {
            var queryParms = req.GetQueryParameterDictionary();
            return Parse(queryParms);
        }

        public static Result<ValidReportSupportingUnitsParameters, Error> Parse(IDictionary<string, string> queryParams)
        {
            queryParams.TryGetValue("departmentId", out string departmentIdStr);
            var departmentId = int.Parse(departmentIdStr);
            
            queryParams.TryGetValue("unitId", out string unitIdStr);
            var unitId = int.Parse(unitIdStr);

            return Pipeline.Success(new ValidReportSupportingUnitsParameters(departmentId, unitId));
        }
    }

    public class NotificationsParameters : BaseSearchParameters
    {
        public bool ShowReviewed { get; set; }
        public NotificationsParameters(bool showReviewed = false) : base(string.Empty)
        {
            ShowReviewed = showReviewed;
        }

        public static Result<NotificationsParameters, Error> Parse(HttpRequest req) 
        {
            var queryParms = req.GetQueryParameterDictionary();
            return Parse(queryParms);
        }

        public static Result<NotificationsParameters, Error> Parse(IDictionary<string, string> queryParms)
        {
            queryParms.TryGetValue("showReviewed", out string showReviewedString);

            var truthyStrings = new List<string> { "true", "t", "yes", "y", "1" }; 
            bool result = truthyStrings.Any(ts => ts.Equals(showReviewedString, StringComparison.InvariantCultureIgnoreCase));

            return Pipeline.Success(new NotificationsParameters(result));
        }
    }
}
