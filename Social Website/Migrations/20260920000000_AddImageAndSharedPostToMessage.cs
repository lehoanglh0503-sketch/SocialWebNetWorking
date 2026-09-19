using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Social_Website.Migrations
{
    public partial class AddImageAndSharedPostToMessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SharedPostId",
                table: "Messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SharedPostId",
                table: "Messages",
                column: "SharedPostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Posts_SharedPostId",
                table: "Messages",
                column: "SharedPostId",
                principalTable: "Posts",
                principalColumn: "PostId",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Posts_SharedPostId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_SharedPostId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "SharedPostId",
                table: "Messages");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
