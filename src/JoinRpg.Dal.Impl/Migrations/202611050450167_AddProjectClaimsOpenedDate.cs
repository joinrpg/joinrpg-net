namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

/// <summary>
/// Дата первого открытия заявок в проекте. Бэкфилл: дата первой отправленной заявки;
/// если заявок не было, а приём заявок открыт — момент миграции.
/// </summary>
public partial class AddProjectClaimsOpenedDate : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.Projects", "ClaimsOpenedDate", c => c.DateTime());
        // Бэкфилл: дата первой отправленной заявки.
        Sql("UPDATE dbo.Projects SET ClaimsOpenedDate = (SELECT MIN(c.CreateDate) FROM dbo.Claims c WHERE c.ProjectId = dbo.Projects.ProjectId) "
            + "WHERE EXISTS (SELECT 1 FROM dbo.Claims c WHERE c.ProjectId = dbo.Projects.ProjectId)");
        // Заявок не было, но приём сейчас открыт — считаем датой открытия момент миграции.
        Sql("UPDATE dbo.Projects SET ClaimsOpenedDate = SYSUTCDATETIME() WHERE ClaimsOpenedDate IS NULL AND IsAcceptingClaims = 1");
    }

    public override void Down() => DropColumn("dbo.Projects", "ClaimsOpenedDate");
}
