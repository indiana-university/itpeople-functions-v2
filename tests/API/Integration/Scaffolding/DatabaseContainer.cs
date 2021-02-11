using System.Threading.Tasks;
using System;
using System.IO;
using System.Collections.Generic;
using Database;
using Microsoft.EntityFrameworkCore;
using Models;
using System.Data.Common;

namespace Integration
{
    public abstract class DatabaseContainer : DockerContainer
    {
        protected DatabaseContainer(TextWriter progress, TextWriter error, string imageName) 
            : base(progress, error, imageName, "integration-test-db")
        {
        }

        public static string ConnectionString { get; private set; }

        // Gotta wait until the database server is really available
        // or you'll get oddball test failures;)
        protected override async Task<bool> isReady()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();
                    return true;
                }
            }
            catch (Exception e)
            {
                Progress.WriteLine($"🤔 {ContainerName} is not yet ready: {e.Message}");
                return false;
            }
        }

        protected abstract DbConnection GetConnection();

        public void ResetDatabase()
        {
            using (var peopleContext = PeopleContext.Create(PeopleContext.LocalDatabaseConnectionString))
            {
                ResetTestData(peopleContext);
            }
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
                    public.units,
                    public.unit_members,
                    public.unit_member_tools
                RESTART IDENTITY
                CASCADE;
            ");

            peopleContext.Buildings.AddRange(new List<Building> {
                TestEntities.Buildings.CityHall,
                TestEntities.Buildings.RonsCabin,
                TestEntities.Buildings.SmallPark,
            });

            peopleContext.BuildingRelationships.AddRange(new List<BuildingRelationship> {
                TestEntities.BuildingRelationships.CityHallCityOfPawnee
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
                TestEntities.SupportRelationships.ParksAndRecRelationship
            });

            peopleContext.UnitMembers.AddRange(new List<UnitMember> { 
                TestEntities.UnitMembers.RSwansonDirector,
                TestEntities.UnitMembers.LkNopeSublead,
                TestEntities.UnitMembers.BWyattAditor
            });
            // peopleContext.MemberTools.AddRange(new List<MemberTool> { 
            //     TestEntities.MemberTools.MemberTool
            // });
            
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
        }
    }
}