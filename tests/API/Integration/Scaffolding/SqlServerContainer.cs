using System.Data.SqlClient;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using System;
using System.IO;
using System.Collections.Generic;
using Database;
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
        /*
                public static void ResetDatabase()
                {
                    using (var printContext = GetDbContext())
                    {
                        ResetSchema(printContext);
                        SeedTestData(printContext);
                    }
                }

                private static void ResetSchema(PrintContext printContext)
                {
                    var migrator = printContext.Database.GetService<IMigrator>();
                    migrator.Migrate(Migration.InitialDatabase);
                    migrator.Migrate();
                }

                private static void SeedTestData(PrintContext printContext)
                {
                    var testEntities = new TestEntities(printContext);

                    using (var transaction = printContext.Database.BeginTransaction())
                    {
                        printContext.Devices.AddRange(
                            testEntities.Device1,
                            testEntities.Device2,
                            testEntities.Device3,
                            testEntities.DeviceInactive
                        );

                        printContext.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [dbo].[Devices] ON;");
                        printContext.SaveChanges();
                        printContext.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [dbo].[Devices] OFF;");

                        printContext.Clients.AddRange(testEntities.Client1, testEntities.Client2, testEntities.Client3, testEntities.ClientInactive);
                        printContext.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [dbo].[Clients] ON");
                        printContext.SaveChanges();
                        printContext.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [dbo].[Clients] OFF");

                        //Add Cost Centers for Clients.
                        printContext.CostCenters.AddRange(testEntities.Client1CostCenterA, testEntities.Client1CostCenterB, testEntities.Client2CostCenterA, testEntities.ClientInactiveCostCenterA);
                        printContext.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [dbo].[CostCenters] ON;");
                        printContext.SaveChanges();
                        printContext.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [dbo].[CostCenters] OFF;");

                        //Add Clients for Device1 and Device2                                
                        testEntities.Device1.Client = testEntities.Client1;
                        testEntities.Device2.Client = testEntities.Client1;
                        printContext.SaveChanges();

                        transaction.Commit();
                    }
                }

                public static PrintContext GetDbContext()
                {
                    var optionsBuilder = new DbContextOptionsBuilder<PrintContext>();
                    optionsBuilder.UseSqlServer(PrintContext.LocalConnectionString+";Database=Print");
                    return new PrintContext(optionsBuilder.Options);
                } 
            */
    }
}