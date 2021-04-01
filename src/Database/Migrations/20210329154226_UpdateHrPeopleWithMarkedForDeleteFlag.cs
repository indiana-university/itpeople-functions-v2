using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class UpdateHrPeopleWithMarkedForDeleteFlag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "marked_for_delete",
                schema: "public",
                table: "hr_people",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "marked_for_delete",
                schema: "public",
                table: "hr_people");
        }
    }
}
