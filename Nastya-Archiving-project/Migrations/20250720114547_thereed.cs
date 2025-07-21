using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nastya_Archiving_project.Migrations
{
    /// <inheritdoc />
    public partial class thereed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JoinedDocs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentRefrenceNO = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ChildRefrenceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BreafcaseNo = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JoinedDocs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JoinedDocs");
        }
    }
}
