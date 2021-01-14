using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class AddDepartmentAndPeopleTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NetId = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    NameFirst = table.Column<string>(nullable: false),
                    NameLast = table.Column<string>(nullable: false),
                    Position = table.Column<string>(nullable: false),
                    Location = table.Column<string>(nullable: false),
                    Campus = table.Column<string>(nullable: false),
                    CampusPhone = table.Column<string>(nullable: false),
                    CampusEmail = table.Column<string>(nullable: false),
                    Expertise = table.Column<string>(nullable: true),
                    Notes = table.Column<string>(nullable: true),
                    PhotoUrl = table.Column<string>(nullable: true),
                    Responsibilities = table.Column<int>(nullable: false),
                    IsServiceAdmin = table.Column<bool>(nullable: false),
                    DepartmentId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                    table.ForeignKey(
                        name: "FK_People_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_People_DepartmentId",
                table: "People",
                column: "DepartmentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
