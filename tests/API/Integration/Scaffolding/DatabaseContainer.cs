using System.Threading.Tasks;
using System;
using System.IO;
using System.Collections.Generic;
using Database;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Models;
using System.Data.Common;

namespace Integration
{
    public abstract class DatabaseContainer : DockerContainer
    {
        protected DatabaseContainer(TextWriter progress, TextWriter error, string imageName, string connectionString) 
            : base(progress, error, imageName, "integration-test-db")
        {
            ConnectionString = connectionString;
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
            using (var peopleContext = PeopleContext.Create(ConnectionString+";Database=ItPeople"))
            {
                ResetSchema(peopleContext);
                SeedTestData(peopleContext);
            }
        }

        protected void ResetSchema(PeopleContext peopleContext)
        {
            var migrator = peopleContext.Database.GetService<IMigrator>();
            migrator.Migrate();
        }

        private static void SeedTestData(PeopleContext peopleContext)
        {
            peopleContext.Database.ExecuteSqlRaw(@"
                TRUNCATE 
                    public.people, 
                    public.departments 
                RESTART IDENTITY
                CASCADE;
            ");

            using (var transaction = peopleContext.Database.BeginTransaction())
            {
                Department parksDept = new Department() { Id = 1, Name = "Parks Department", Description = "Your local Parks department." };
                peopleContext.Departments.Add(parksDept);

                // peopleContext.Database.ExecuteSqlRaw("SET IDENTITY_INSERT departments ON;");
                // peopleContext.SaveChanges();
                // peopleContext.Database.ExecuteSqlRaw("SET IDENTITY_INSERT departments OFF;");

                Person rswanson = new Person() { Id = 1, Netid = "rswanson", Name="Swanson, Ron", NameFirst = "Ron", NameLast = "Swanson", Position = "Parks and Rec Director", Location = "", Campus = "Pawnee", CampusPhone = "", CampusEmail = "rswanso@pawnee.in.us", Expertise = "Woodworking; Honor", Notes = "", PhotoUrl = "http://flavorwire.files.wordpress.com/2011/11/ron-swanson.jpg", Responsibilities = Responsibilities.ItLeadership, DepartmentId = parksDept.Id, Department = parksDept, IsServiceAdmin = false };
                Person lknope = new Person() { Id = 2, Netid = "lknope", Name="Knope, Leslie", NameFirst = "Leslie", NameLast = "Knope", Position = "Parks and Rec Deputy Director", Location = "", Campus = "Pawnee", CampusPhone = "", CampusEmail = "lknope@pawnee.in.us", Expertise = "Canvassing; Waffles", Notes = "", PhotoUrl = "https://en.wikipedia.org/wiki/Leslie_Knope#/media/File:Leslie_Knope_(played_by_Amy_Poehler).png", Responsibilities = Responsibilities.ItLeadership | Responsibilities.ItProjectMgt, DepartmentId = parksDept.Id, Department = parksDept, IsServiceAdmin = false };
                Person bwyatt = new Person() { Id = 3, Netid = "bwyatt", Name="Wyatt, Ben", NameFirst = "Ben", NameLast = "Wyatt", Position = "Auditor", Location = "", Campus = "Indianapolis", CampusPhone = "", CampusEmail = "bwyatt@pawnee.in.us", Expertise = "Board Games; Comic Books", Notes = "", PhotoUrl = "https://sasquatchbrewery.com/wp-content/uploads/2018/06/lil.jpg", Responsibilities = Responsibilities.ItProjectMgt, DepartmentId = parksDept.Id, Department = parksDept, IsServiceAdmin = false };
                peopleContext.People.AddRange(new List<Person> { rswanson, lknope, bwyatt });

                // peopleContext.Database.ExecuteSqlRaw("SET IDENTITY_INSERT people ON;");
                peopleContext.SaveChanges();
                // peopleContext.Database.ExecuteSqlRaw("SET IDENTITY_INSERT people OFF;");

                transaction.Commit();
            }
        }
 
    }
}