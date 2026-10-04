namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class ProjectAclStatusAndProfile : DbMigration
{
    public override void Up()
    {
        // IX_ProjectId покрывается первой колонкой уникального индекса ниже.
        // IX_UserId остаётся как был: скаффолдер пересоздавал его без изменений, это убрано руками.
        DropIndex("dbo.ProjectAcls", new[] { "ProjectId" });
        // Все существующие мастера активны и видны, как до этой миграции (ADR019).
        AddColumn("dbo.ProjectAcls", "Status", c => c.Int(nullable: false, defaultValue: 0));
        AddColumn("dbo.ProjectAcls", "IsPublic", c => c.Boolean(nullable: false, defaultValue: true));
        // Роль существующим мастерам — «Мастер» всем, включая владельцев (ADR019, §4). TODO[Localize]
        // Через UPDATE с N-литералом, а не defaultValue: EF6 пишет default без префикса N,
        // и на базе с не-кириллической collation кириллица превратилась бы в «??????».
        AddColumn("dbo.ProjectAcls", "Role", c => c.String(maxLength: 100));
        Sql("UPDATE dbo.ProjectAcls SET Role = N'Мастер'");
        AlterColumn("dbo.ProjectAcls", "Role", c => c.String(nullable: false, maxLength: 100));
        AddColumn("dbo.ProjectAcls", "Description_Contents", c => c.String());
        AddColumn("dbo.ProjectDetails", "MastersOrdering", c => c.String());
        CreateIndex("dbo.ProjectAcls", new[] { "ProjectId", "UserId" }, unique: true, name: "IX_ProjectAcl_ProjectId_UserId");
    }

    public override void Down()
    {
        DropIndex("dbo.ProjectAcls", "IX_ProjectAcl_ProjectId_UserId");
        DropColumn("dbo.ProjectDetails", "MastersOrdering");
        DropColumn("dbo.ProjectAcls", "Description_Contents");
        DropColumn("dbo.ProjectAcls", "Role");
        DropColumn("dbo.ProjectAcls", "IsPublic");
        DropColumn("dbo.ProjectAcls", "Status");
        CreateIndex("dbo.ProjectAcls", "ProjectId");
    }
}
