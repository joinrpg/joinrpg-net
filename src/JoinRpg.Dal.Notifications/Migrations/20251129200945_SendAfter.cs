using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoinRpg.Dal.Notifications.Migrations;

/// <inheritdoc />
public partial class SendAfter : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Attempts",
            table: "NotificationMessageChannels",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "SendAfter",
            table: "NotificationMessageChannels",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Attempts",
            table: "NotificationMessageChannels");

        migrationBuilder.DropColumn(
            name: "SendAfter",
            table: "NotificationMessageChannels");
    }
}
