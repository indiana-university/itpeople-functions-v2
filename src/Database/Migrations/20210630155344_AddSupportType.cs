using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Database.Migrations
{
    public partial class AddSupportType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "support_type_id",
                schema: "public",
                table: "support_relationships",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "support_types",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_support_types", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_support_relationships_support_type_id",
                schema: "public",
                table: "support_relationships",
                column: "support_type_id");

            migrationBuilder.AddForeignKey(
                name: "fk_support_relationships_support_types_support_type_id",
                schema: "public",
                table: "support_relationships",
                column: "support_type_id",
                principalSchema: "public",
                principalTable: "support_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
                
            migrationBuilder.Sql(@"
                INSERT INTO support_types(name)
                VALUES
                    ('Full Service'),
                    ('Desktop/Endpoint'),
                    ('Web/app Infrastructure'),
                    ('Research Infrastructure')
            ");       
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_support_relationships_support_types_support_type_id",
                schema: "public",
                table: "support_relationships");

            migrationBuilder.DropTable(
                name: "support_types",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_support_relationships_support_type_id",
                schema: "public",
                table: "support_relationships");

            migrationBuilder.DropColumn(
                name: "support_type_id",
                schema: "public",
                table: "support_relationships");
        }
    }
}
