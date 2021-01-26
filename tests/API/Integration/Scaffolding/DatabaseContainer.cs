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
                    public.people, 
                    public.departments,
                    public.units,
                    public.unit_members,
                    public.unit_member_tools
                RESTART IDENTITY
                CASCADE;
            ");

            peopleContext.Departments.AddRange(new List<Department> {
                TestEntities.Departments.Parks
            });
            
            peopleContext.Units.AddRange(new List<Unit> {
                TestEntities.Units.ParentUnit,
                TestEntities.Units.Unit
            });

            peopleContext.People.AddRange(new List<Person> { 
                TestEntities.People.RSwanson, 
                TestEntities.People.LKnope,
                TestEntities.People.BWyatt
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
        }
    }
}