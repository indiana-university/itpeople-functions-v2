using System;
using Microsoft.EntityFrameworkCore;
using Models;
// using Npgsql.Logging;

namespace Database
{
    public class PeopleContext : DbContext
    {
        public const string LocalServerConnectionString = "Server=localhost;User Id=SA;Password=abcd1234@";
        public const string LocalDatabaseConnectionString = "Server=localhost;Database=ItPeople;User Id=SA;Password=abcd1234@";

        private readonly bool _calledFromEfCoreTools = false;

        // This constructor is needed for the dotnet-ef tool to make a new migration.
        public PeopleContext()
        {         
            _calledFromEfCoreTools = true;
        }

        public PeopleContext(DbContextOptions<PeopleContext> options)
        : base(options)
        {}

        public static PeopleContext Create()
        {
            var ConnectionString = System.Environment.GetEnvironmentVariable("DatabaseConnectionString");
            if (string.IsNullOrWhiteSpace(ConnectionString))
                throw new Exception("Missing environment variable: 'DatabaseConnectionString'");

            return Create(ConnectionString);
        }

        public static PeopleContext Create(string connectionString)
        {
            var options = new DbContextOptionsBuilder<PeopleContext>();
            ConfigureDatabaseOptions(options, connectionString);
            return new PeopleContext(options.Options);
        }

        public DbSet<Person> People { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<UnitMember> UnitMembers { get; set; }
        public DbSet<Tool> Tools { get; set; }
        public DbSet<ToolPermission> ToolPermissions { get; set; }
        public DbSet<MemberTool> MemberTools { get; set; }
        public DbSet<BuildingRelationship> BuildingRelationships { get; set; }
        public DbSet<SupportRelationship> SupportRelationships { get; set; }
        public DbSet<HrPerson> HrPeople { get; set; }
        public DbSet<HistoricalPerson> HistoricalPeople { get; set; }
        public DbSet<SupportType> SupportTypes { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {   
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("public");
        }

        // This method is needed for the dotnet-ef tool to make a new migration.
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (_calledFromEfCoreTools)
            {
                Console.WriteLine("Configuring database options to manage migrations for local DB instance.");
                ConfigureDatabaseOptions(options, LocalDatabaseConnectionString);
            }
        }

        private static void ConfigureDatabaseOptions(DbContextOptionsBuilder options, string connectionString)
        {
            options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging();
        }
    }
}
