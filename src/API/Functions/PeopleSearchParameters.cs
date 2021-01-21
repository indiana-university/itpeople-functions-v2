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
        public PeopleSearchParameters(string q, Responsibilities responsibilities, string[] expertise )
        {
            Q = q;
            Responsibilities = responsibilities;
            Expertise = expertise;
        }
        
        public string Q { get; }
        public Responsibilities Responsibilities { get; }
        public string[] Expertise { get; }


        public static Result<PeopleSearchParameters, Error> Parse(HttpRequest req) 
            => Parse(req.GetQueryParameterDictionary());

        public static Result<PeopleSearchParameters, Error> Parse(IDictionary<string, string> queryParms)
        {
            queryParms.TryGetValue("q", out string q);
            queryParms.TryGetValue("class", out string jobClass);
            queryParms.TryGetValue("interest", out string interests);
            var responsibilities = string.IsNullOrWhiteSpace(jobClass)
                ? Responsibilities.None
                : (Responsibilities)Enum.Parse(typeof(Responsibilities), jobClass);
            var expertises = string.IsNullOrWhiteSpace(interests)
                ? new string[0]
                : interests.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).ToArray();
            var result = new PeopleSearchParameters(q, responsibilities, expertises);
            return Pipeline.Success(result);
        }
    }
}
