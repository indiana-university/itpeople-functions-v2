using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class RenamePrimarySupportUnit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_departments_units_report_supporting_unit_id",
                schema: "public",
                table: "departments");

            migrationBuilder.RenameColumn(
                name: "report_supporting_unit_id",
                schema: "public",
                table: "departments",
                newName: "primary_support_unit_id");

            migrationBuilder.RenameIndex(
                name: "ix_departments_report_supporting_unit_id",
                schema: "public",
                table: "departments",
                newName: "ix_departments_primary_support_unit_id");

            migrationBuilder.AddForeignKey(
                name: "fk_departments_units_primary_support_unit_id",
                schema: "public",
                table: "departments",
                column: "primary_support_unit_id",
                principalSchema: "public",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_departments_units_primary_support_unit_id",
                schema: "public",
                table: "departments");

            migrationBuilder.RenameColumn(
                name: "primary_support_unit_id",
                schema: "public",
                table: "departments",
                newName: "report_supporting_unit_id");

            migrationBuilder.RenameIndex(
                name: "ix_departments_primary_support_unit_id",
                schema: "public",
                table: "departments",
                newName: "ix_departments_report_supporting_unit_id");

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
    }
}
