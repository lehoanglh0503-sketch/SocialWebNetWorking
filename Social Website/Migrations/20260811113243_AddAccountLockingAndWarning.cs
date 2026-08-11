using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Social_Website.Migrations
{
    public partial class AddAccountLockingAndWarning : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovedReportCount",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedReportCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "Users");
        }
    }
}
