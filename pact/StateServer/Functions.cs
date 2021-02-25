using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Database;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Models;

namespace StateServer
{
    public static class Functions
    {
        [FunctionName(nameof(Functions.Ping))]
        public static IActionResult Ping(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")] HttpRequest req) 
                => new OkObjectResult("Pong!");
       
       [FunctionName(nameof(Functions.State))]
        public static IActionResult State(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "state")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Resetting Postgres DB with test data...");
            var connStr = System.Environment.GetEnvironmentVariable("DatabaseConnectionString");
            using (var peopleContext = PeopleContext.Create(connStr))
            {
                ResetTestData(peopleContext);
            }            
            return new OkResult();
        } 

        private static void ResetTestData(PeopleContext peopleContext)
        {
            peopleContext.Database.ExecuteSqlRaw(@"
                TRUNCATE 
                    public.buildings, 
                    public.building_relationships, 
                    public.departments,
                    public.people, 
                    public.support_relationships, 
                    public.tools,
                    public.units,
                    public.unit_members,
                    public.unit_member_tools,
                    public.logs
                RESTART IDENTITY
                CASCADE;
            ");

            peopleContext.Buildings.AddRange(new List<Building> {
                TestEntities.Buildings.CityHall,
                TestEntities.Buildings.RonsCabin,
                TestEntities.Buildings.SmallPark,
            });

            peopleContext.BuildingRelationships.AddRange(new List<BuildingRelationship> {
                TestEntities.BuildingRelationships.CityHallCityOfPawnee,
                TestEntities.BuildingRelationships.RonsCabinCityOfPawnee,
            });

            peopleContext.Departments.AddRange(new List<Department> {
                TestEntities.Departments.Parks,
                TestEntities.Departments.Fire,
                TestEntities.Departments.Auditor
            });
            
            peopleContext.Units.AddRange(new List<Unit> {
                TestEntities.Units.CityOfPawnee,
                TestEntities.Units.ParksAndRecUnit,
                TestEntities.Units.Auditor,
            });

            peopleContext.People.AddRange(new List<Person> { 
                TestEntities.People.RSwanson, 
                TestEntities.People.LKnope,
                TestEntities.People.BWyatt,
                TestEntities.People.ServiceAdmin
            });  
            
            peopleContext.SupportRelationships.AddRange(new List<SupportRelationship> {
                TestEntities.SupportRelationships.ParksAndRecRelationship,
                TestEntities.SupportRelationships.PawneeUnitFire

            });

            peopleContext.UnitMembers.AddRange(new List<UnitMember> { 
                TestEntities.UnitMembers.RSwansonDirector,
                TestEntities.UnitMembers.LkNopeSublead,
                TestEntities.UnitMembers.BWyattAditor,
                TestEntities.UnitMembers.AdminLeader
            });
            
            peopleContext.Tools.AddRange(new List<Tool> { 
                TestEntities.Tools.Hammer
            });

            peopleContext.MemberTools.AddRange(new List<MemberTool> { 
                TestEntities.MemberTools.MemberTool,
                TestEntities.MemberTools.AdminMemberTool
            });
            
            peopleContext.SaveChanges();

            // Account for the identities of Units we've already added to the database.  Prevents error:
            //      duplicate key value violates unique constraint
            peopleContext.Database.ExecuteSqlRaw(@"
                SELECT
                    setval(pg_get_serial_sequence('public.units', 'id'), 
                    (SELECT MAX(id) FROM public.units));
                ");
            peopleContext.Database.ExecuteSqlRaw(@"
                SELECT
                    setval(pg_get_serial_sequence('public.building_relationships', 'id'), 
                    (SELECT MAX(id) FROM public.building_relationships));
                ");
            peopleContext.Database.ExecuteSqlRaw(@"
                SELECT
                    setval(pg_get_serial_sequence('public.support_relationships', 'id'), 
                    (SELECT MAX(id) FROM public.support_relationships));
                ");
            peopleContext.Database.ExecuteSqlRaw(@"
                SELECT
                    setval(pg_get_serial_sequence('public.unit_members', 'id'), 
                    (SELECT MAX(id) FROM public.unit_members));
                ");
        }
    }
}
