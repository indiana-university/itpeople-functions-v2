using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class AddContentColumnsToLogsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Change logs.content to JSON 
            migrationBuilder.Sql(@"
                ALTER TABLE logs ALTER COLUMN content TYPE JSON USING content::JSON;
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

            // Change logs.content back to TEXT 
            migrationBuilder.Sql(@"
                ALTER TABLE logs ALTER COLUMN content TYPE TEXT USING content::TEXT;
            ");            
        }
    }
}
