using System.Threading.Tasks;
using API.Middleware;
using CSharpFunctionalExtensions;
using Database;
using System;

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
    }
}