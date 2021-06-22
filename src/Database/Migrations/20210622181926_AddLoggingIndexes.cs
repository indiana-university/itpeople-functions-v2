using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class AddLoggingIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE INDEX logs_automation_timestamp_idx ON logs_automation (""timestamp"");
                CREATE INDEX logs_automation_function_name_idx ON logs_automation (""function_name"");
                CREATE INDEX logs_timestamp_idx ON logs (""timestamp"");
                CREATE INDEX logs_function_idx ON logs (""function"");
                CREATE INDEX logs_netid_idx ON logs (""netid"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX logs_automation_function_name_idx;
                DROP INDEX logs_automation_timestamp_idx;
                DROP INDEX logs_timestamp_idx;
                DROP INDEX logs_function_idx;
                DROP INDEX logs_netid_idx;
            ");
        }
    }
}
