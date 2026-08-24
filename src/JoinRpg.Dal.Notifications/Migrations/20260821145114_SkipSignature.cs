using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoinRpg.Dal.Notifications.Migrations
{
    /// <inheritdoc />
    public partial class SkipSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SkipSignature",
                table: "Notifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkipSignature",
                table: "Notifications");
        }
    }
}
