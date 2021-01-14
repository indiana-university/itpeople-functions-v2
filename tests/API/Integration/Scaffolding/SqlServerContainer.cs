using System.Data.SqlClient;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using System;
using System.IO;
using System.Collections.Generic;
using Database;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Models;
using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Infrastructure;
// using Microsoft.EntityFrameworkCore.Migrations;
// using NUnit.Framework;

namespace Integration
{

    ///<summary>See https://hub.docker.com/_/microsoft-mssql-server for imags, tags, and usage notes.</summary>
    internal class SqlServerContainer : DockerContainer
    {        
        public SqlServerContainer(TextWriter progress, TextWriter error) 
            : base(progress, error, "mcr.microsoft.com/mssql/server:2019-latest", "integration-test-db")
        {
        }

        // Gotta wait until the database server is really available
        // or you'll get oddball test failures;)
        protected override async Task<bool> isReady()
        {
            try
            {
                using (var conn = new SqlConnection(PeopleContext.LocalMasterConnectionString))
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

        // Watch the port mapping here to avoid port
        // contention w/ other Sql Server installations
        public override HostConfig ToHostConfig() 
            => new HostConfig()
            {
                NetworkMode = NetworkName,
                PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        {
                            "1433/tcp",
                            new List<PortBinding>
                            {
                                new PortBinding
                                {
                                    HostPort = $"1433"
                                }
                            }
                        },
                    },
            };

        public override Config ToConfig() 
            => new Config
            {
                Env = new List<string> { "ACCEPT_EULA=Y", "SA_PASSWORD=abcd1234@", "MSSQL_PID=Developer" }
            };
        public static void ResetDatabase()
        {
            using (var peopleContext = PeopleContext.Create(PeopleContext.LocalConnectionString))
            {
                Console.WriteLine("Resetting schema...");
                ResetSchema(peopleContext);
                Console.WriteLine("Seeding test data...");
                SeedTestData(peopleContext);
            }
        }

        private static void ResetSchema(PeopleContext peopleContext)
        {
            var migrator = peopleContext.Database.GetService<IMigrator>();
            migrator.Migrate(Migration.InitialDatabase);
            migrator.Migrate();
        }

        private static void SeedTestData(PeopleContext peopleContext)
        {
            using (var transaction = peopleContext.Database.BeginTransaction())
            {
                Department parksDept = new Department() { Id = 1, Name = "Parks Department" };
                peopleContext.Departments.Add(parksDept);

                peopleContext.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [dbo].[Departments] ON;");
                peopleContext.SaveChanges();
                peopleContext.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [dbo].[Departments] OFF;");

                Person rswanson = new Person() { Id = 1, NetId = "rswanson", Name="Swanson, Ron", NameFirst = "Ron", NameLast = "Swanson", Position = "Parks and Rec Director", Location = "", Campus = "Pawnee", CampusPhone = "", CampusEmail = "rswanso@pawnee.in.us", Expertise = "Woodworking; Honor", Notes = "", PhotoUrl = "http://flavorwire.files.wordpress.com/2011/11/ron-swanson.jpg", Responsibilities = Responsibilities.ItLeadership, DepartmentId = parksDept.Id, Department = parksDept, IsServiceAdmin = false };
                Person lknope = new Person() { Id = 2, NetId = "lknope", Name="Knope, Leslie", NameFirst = "Leslie", NameLast = "Knope", Position = "Parks and Rec Deputy Director", Location = "", Campus = "Pawnee", CampusPhone = "", CampusEmail = "lknope@pawnee.in.us", Expertise = "Canvassing; Waffles", Notes = "", PhotoUrl = "https://en.wikipedia.org/wiki/Leslie_Knope#/media/File:Leslie_Knope_(played_by_Amy_Poehler).png", Responsibilities = Responsibilities.ItLeadership | Responsibilities.ItProjectMgt, DepartmentId = parksDept.Id, Department = parksDept, IsServiceAdmin = false };
                Person bwyatt = new Person() { Id = 3, NetId = "bwyatt", Name="Wyatt, Ben", NameFirst = "Ben", NameLast = "Wyatt", Position = "Auditor", Location = "", Campus = "Indianapolis", CampusPhone = "", CampusEmail = "bwyatt@pawnee.in.us", Expertise = "Board Games; Comic Books", Notes = "", PhotoUrl = "https://sasquatchbrewery.com/wp-content/uploads/2018/06/lil.jpg", Responsibilities = Responsibilities.ItProjectMgt, DepartmentId = parksDept.Id, Department = parksDept, IsServiceAdmin = false };
                peopleContext.People.AddRange(new List<Person> { rswanson, lknope, bwyatt });

                peopleContext.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [dbo].[People] ON;");
                peopleContext.SaveChanges();
                peopleContext.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [dbo].[People] OFF;");

                transaction.Commit();
            }
        }
    }
}