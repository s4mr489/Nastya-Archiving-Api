using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nastya_Archiving_project.Migrations
{
    /// <inheritdoc />
    public partial class inital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DocType",
                table: "Arciving_Docs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_gp_Branches",
                table: "gp_Branches",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_gp_Branches",
                table: "gp_Branches");

            migrationBuilder.AlterColumn<int>(
                name: "DocType",
                table: "Arciving_Docs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
