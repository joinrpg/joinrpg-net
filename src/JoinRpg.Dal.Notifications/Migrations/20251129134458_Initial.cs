using JoinRpg.Dal.Notifications;
using JoinRpg.PrimitiveTypes.Notifications;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JoinRpg.Dal.Notifications.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:notification_channel", "email")
                .Annotation("Npgsql:Enum:notification_message_status", "failed,queued,sending,sent");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationMessageId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Header = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    InitiatorUserId = table.Column<int>(type: "integer", nullable: false),
                    RecipientUserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationMessageId);
                });

            migrationBuilder.CreateTable(
                name: "NotificationMessageChannels",
                columns: table => new
                {
                    NotificationMessageChannelId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NotificationMessageId = table.Column<int>(type: "integer", nullable: false),
                    Channel = table.Column<NotificationChannel>(type: "notification_channel", nullable: false),
                    ChannelSpecificValue = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    NotificationMessageStatus = table.Column<NotificationMessageStatus>(type: "notification_message_status", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationMessageChannels", x => x.NotificationMessageChannelId);
                    table.ForeignKey(
                        name: "FK_NotificationMessageChannels_Notifications_NotificationMessa~",
                        column: x => x.NotificationMessageId,
                        principalTable: "Notifications",
                        principalColumn: "NotificationMessageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMessageChannels_Channel_NotificationMessageChan~",
                table: "NotificationMessageChannels",
                columns: new[] { "Channel", "NotificationMessageChannelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMessageChannels_NotificationMessageId",
                table: "NotificationMessageChannels",
                column: "NotificationMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMessageChannels_NotificationMessageStatus_Chann~",
                table: "NotificationMessageChannels",
                columns: new[] { "NotificationMessageStatus", "Channel", "NotificationMessageChannelId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_InitiatorUserId",
                table: "Notifications",
                column: "InitiatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId",
                table: "Notifications",
                column: "RecipientUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationMessageChannels");

            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
