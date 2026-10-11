using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoinRpg.Dal.Notifications.Migrations;

/// <inheritdoc />
public partial class EntityReferenceIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // То же, что сгенерировал бы CreateIndex по модели, но повторяемо: если CREATE INDEX CONCURRENTLY
        // прервётся (таймаут), Postgres оставит недействительный индекс, а запись о миграции не появится.
        // Без предварительного DROP следующий запуск упал бы на «already exists» и остановил все миграторы.
        migrationBuilder.Sql(
            """DROP INDEX CONCURRENTLY IF EXISTS "IX_Notifications_EntityReference";""",
            suppressTransaction: true);
        migrationBuilder.Sql(
            """CREATE INDEX CONCURRENTLY "IX_Notifications_EntityReference" ON "Notifications" ("EntityReference" text_pattern_ops);""",
            suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Notifications_EntityReference",
            table: "Notifications");
    }
}
