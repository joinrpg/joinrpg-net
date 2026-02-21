using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoinRpg.Dal.Notifications.Migrations;

/// <inheritdoc />
public partial class EntityRef : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:Enum:notification_channel", "email,show_in_ui")
            .Annotation("Npgsql:Enum:notification_message_status", "failed,queued,sending,sent")
            .OldAnnotation("Npgsql:Enum:notification_channel", "email")
            .OldAnnotation("Npgsql:Enum:notification_message_status", "failed,queued,sending,sent");

        migrationBuilder.AddColumn<string>(
            name: "EntityReference",
            table: "Notifications",
            type: "text",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EntityReference",
            table: "Notifications");

        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:Enum:notification_channel", "email")
            .Annotation("Npgsql:Enum:notification_message_status", "failed,queued,sending,sent")
            .OldAnnotation("Npgsql:Enum:notification_channel", "email,show_in_ui")
            .OldAnnotation("Npgsql:Enum:notification_message_status", "failed,queued,sending,sent");
    }
}
