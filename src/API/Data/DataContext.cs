using Models;

using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class DataContext : DbContext
    {
        public const string LocalConnectionString = "Server=localhost;Database=ItPeople;User Id=SA;Password=abcd1234@;";

        public DbSet<Person> People { get; set; }
        public DbSet<Department> Departments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // TODO: pull connection string from the environment
            string connectionString = LocalConnectionString;
            optionsBuilder.UseSqlServer(connectionString);
        }
    }
}