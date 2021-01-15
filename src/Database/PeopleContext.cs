using System;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Database
{
    public class PeopleContext : DbContext
    {
        public const string LocalSqlServerConnectionString = "Server=localhost;User Id=SA;Password=abcd1234@;";
        public const string LocalPostgresConnectionString = "Server=localhost;Port=5432;User Id=postgres;Password=abcd1234@;";

        private readonly bool _makingMigration = false;

        // This constructor is needed for the dotnet-ef tool to make a new migration.
        public PeopleContext()
        {         
            _makingMigration = true;
        }

        public PeopleContext(DbContextOptions<PeopleContext> options)
        : base(options)
        {}

        public static PeopleContext Create()
        {
            var ConnectionString = System.Environment.GetEnvironmentVariable("SqlServerConnectionString");
            if (string.IsNullOrWhiteSpace(ConnectionString))
                throw new Exception("Missing environment variable: 'SqlServerConnectionString'");

            return Create(ConnectionString);
        }

        public static PeopleContext Create(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PeopleContext>();
            optionsBuilder.UseNpgsql(connectionString);
            return new PeopleContext(optionsBuilder.Options);
        }

        public DbSet<Person> People { get; set; }
        public DbSet<Department> Departments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {   
            modelBuilder.HasDefaultSchema("public");
        }

        // This method is needed for the dotnet-ef tool to make a new migration.
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (_makingMigration)
            {
                options.UseNpgsql(LocalPostgresConnectionString + ";Database=ItPeople");
            }
        }
    }
}
