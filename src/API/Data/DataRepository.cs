using System.Threading.Tasks;
using API.Middleware;
using CSharpFunctionalExtensions;
using Database;
using System;
using Microsoft.AspNetCore.Http;
using Models;
using Newtonsoft.Json;

namespace API.Data
{
    public abstract class DataRepository
    {
        protected static async Task<Result<T,Error>> ExecuteDbPipeline<T>(string description, Func<PeopleContext, Task<Result<T,Error>>> pipeline)
        {
            try
            {
                using (var db = PeopleContext.Create())
                {                    
                    return await pipeline(db);
                }
            }
            catch (System.Exception ex)
            {
                return Pipeline.InternalServerError($"Failed to {description}", ex);
            }
        }

        protected static void LogPrevious<T>(HttpRequest req, T value) where T : Entity
        {
            req.HttpContext.Items[LogProps.RecordBody] = JsonConvert.SerializeObject(value);
        }
    }
}