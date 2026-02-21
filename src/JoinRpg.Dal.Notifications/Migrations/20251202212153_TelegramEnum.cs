using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoinRpg.Dal.Notifications.Migrations;

/// <inheritdoc />
public partial class TelegramEnum : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:Enum:notification_channel", "email,show_in_ui,telegram")
            .Annotation("Npgsql:Enum:notification_message_status", "failed,queued,sending,sent")
            .OldAnnotation("Npgsql:Enum:notification_channel", "email,show_in_ui")
            .OldAnnotation("Npgsql:Enum:notification_message_status", "failed,queued,sending,sent");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:Enum:notification_channel", "email,show_in_ui")
            .Annotation("Npgsql:Enum:notification_message_status", "failed,queued,sending,sent")
            .OldAnnotation("Npgsql:Enum:notification_channel", "email,show_in_ui,telegram")
            .OldAnnotation("Npgsql:Enum:notification_message_status", "failed,queued,sending,sent");
    }
}
