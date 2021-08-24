using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class UpdateSupportTypeMakeNameUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ONLY public.support_types  
                ADD CONSTRAINT unq_support_type_name UNIQUE (name);
            ");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ONLY public.support_types  
                DROP CONSTRAINT unq_support_type_name;
            ");
        }
    }
}
