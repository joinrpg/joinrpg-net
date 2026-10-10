namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

/// <summary>
/// Блокировка проекта на время восстановления данных (ADR023). Ставит её скрипт DRP, поэтому все
/// существующие проекты получают false.
/// </summary>
public partial class AddProjectIsBlocked : DbMigration
{
    public override void Up() => AddColumn("dbo.Projects", "IsBlocked", c => c.Boolean(nullable: false, defaultValue: false));

    public override void Down() => DropColumn("dbo.Projects", "IsBlocked");
}
