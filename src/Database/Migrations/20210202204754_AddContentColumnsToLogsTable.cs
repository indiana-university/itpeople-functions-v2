using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class AddContentColumnsToLogsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add logs.record to JSON 
            migrationBuilder.Sql(@"
                ALTER TABLE logs ADD COLUMN request JSON;
            ");
            // Add logs.record to JSON 
            migrationBuilder.Sql(@"
                ALTER TABLE logs ADD COLUMN record JSON;
            ");
        }   

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop logs.record
            migrationBuilder.Sql(@"
                ALTER TABLE logs DROP COLUMN record
            ");
            // Drop logs.record
            migrationBuilder.Sql(@"
                ALTER TABLE logs DROP COLUMN request
            ");
        }
    }
}
