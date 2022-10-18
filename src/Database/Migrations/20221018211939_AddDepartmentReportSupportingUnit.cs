using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class AddDepartmentReportSupportingUnit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "report_supporting_unit_id",
                schema: "public",
                table: "departments",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_departments_report_supporting_unit_id",
                schema: "public",
                table: "departments",
                column: "report_supporting_unit_id");

            migrationBuilder.AddForeignKey(
                name: "fk_departments_units_report_supporting_unit_id",
                schema: "public",
                table: "departments",
                column: "report_supporting_unit_id",
                principalSchema: "public",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_departments_units_report_supporting_unit_id",
                schema: "public",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "ix_departments_report_supporting_unit_id",
                schema: "public",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "report_supporting_unit_id",
                schema: "public",
                table: "departments");
        }
    }
}
