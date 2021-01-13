using System;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Database
{
    public class PeopleContext : DbContext
    {
        public const string LocalMasterConnectionString = "Server=localhost;Database=master;User Id=SA;Password=abcd1234@;";
        public const string LocalConnectionString = "Server=localhost;Database=ItPeople;User Id=SA;Password=abcd1234@;";
        
        private readonly bool _makingMigration = false;

        // This constructor is only called by the dotnet-ef tool 
        // when scaffolding a new migration.
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

            var optionsBuilder = new DbContextOptionsBuilder<PeopleContext>();
            optionsBuilder.UseSqlServer(ConnectionString);
            return new PeopleContext(optionsBuilder.Options);
        }

        public DbSet<Person> People { get; set; }
        public DbSet<Department> Departments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (_makingMigration)
            {
                options.UseSqlServer(LocalConnectionString);
            }
        }
    }
}
