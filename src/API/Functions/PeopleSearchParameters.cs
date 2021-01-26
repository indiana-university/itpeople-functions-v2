using Microsoft.Azure.WebJobs.Extensions.Http;
using CSharpFunctionalExtensions;
using API.Middleware;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Models;
using System;
using System.Linq;

namespace API.Functions
{
    public class PeopleSearchParameters
    {

        public PeopleSearchParameters(string q, Responsibilities responsibilities, string[] expertise, string[] campus, Role[] role, UnitPermissions[] permissions)
        {
            Q = q;
            Responsibilities = responsibilities;
            Expertise = expertise;
            Campus = campus;         
            Roles = role;
            Permissions = permissions;
        }
        
        public string Q { get; }
        public Responsibilities Responsibilities { get; }
        public string[] Expertise { get; }
        public string[] Campus { get; }        
        public Role[] Roles { get; }
        public UnitPermissions[] Permissions { get; }

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
            var responsibilities = string.IsNullOrWhiteSpace(jobClass)
                ? Responsibilities.None
                : (Responsibilities)Enum.Parse(typeof(Responsibilities), jobClass); 
            var roles = string.IsNullOrWhiteSpace(role)
                ? new Role[0]
                : ParseCommaSeparatedList(role)
                    .Select(r => {
                        if (!Enum.TryParse<Role>(r, true, out Role value))
                            throw new Exception($"Unit role not recognized: '{r}'");
                        return value;
                    })
                    .ToArray();
            var permissions = string.IsNullOrWhiteSpace(permission)
                ? new UnitPermissions[0]
                : ParseCommaSeparatedList(permission)
                    .Select(p => {
                        if (!Enum.TryParse<UnitPermissions>(p, true, out UnitPermissions value))
                            throw new Exception($"Unit permission not recognized: '{p}'");
                        return value;
                    })
                    .ToArray();
            var expertises = ParseCommaSeparatedList(interests);
            var campuses = ParseCommaSeparatedList(campus);
            var result = new PeopleSearchParameters(q, responsibilities, expertises, campuses, roles, permissions);
            return Pipeline.Success(result);
        }

        // " ,  ,"  😩  => [" ", "  "]
        private static string[] ParseCommaSeparatedList(string str) 
            => string.IsNullOrWhiteSpace(str)
                ? new string[0]
                : str.Split(",", StringSplitOptions.RemoveEmptyEntries)
                    .Select(i => i.Trim())
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .ToArray();
    }
}
