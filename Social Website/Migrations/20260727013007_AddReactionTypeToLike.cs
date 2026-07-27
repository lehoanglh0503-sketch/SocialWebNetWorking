using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Social_Website.Migrations
{
    public partial class AddReactionTypeToLike : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReactionType",
                table: "PostLikes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReactionType",
                table: "PostLikes");
        }
    }
}
