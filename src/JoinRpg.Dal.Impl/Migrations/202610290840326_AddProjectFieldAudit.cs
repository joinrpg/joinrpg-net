namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class AddProjectFieldAudit : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.ProjectFields", "CreatedAt", c => c.DateTime());
        AddColumn("dbo.ProjectFields", "CreatedById", c => c.Int());
        AddColumn("dbo.ProjectFields", "UpdatedAt", c => c.DateTime());
        AddColumn("dbo.ProjectFields", "UpdatedById", c => c.Int());

        // Когда и кем созданы старые поля, неизвестно: берём создание проекта и его владельца.
        // Без владельца — мастер с наименьшим UserId, в том числе снятый: проекты без владельца
        // остались от бага #5232, где уходил как раз владелец с наименьшим UserId.
        Sql(@"
UPDATE f SET
    CreatedAt = p.CreatedDate,
    UpdatedAt = p.CreatedDate,
    CreatedById = author.UserId,
    UpdatedById = author.UserId
FROM dbo.ProjectFields f
JOIN dbo.Projects p ON p.ProjectId = f.ProjectId
CROSS APPLY (
    SELECT TOP 1 a.UserId
    FROM dbo.ProjectAcls a
    WHERE a.ProjectId = f.ProjectId
    ORDER BY a.IsOwner DESC, a.UserId
) author");

        AlterColumn("dbo.ProjectFields", "CreatedAt", c => c.DateTime(nullable: false));
        AlterColumn("dbo.ProjectFields", "CreatedById", c => c.Int(nullable: false));
        AlterColumn("dbo.ProjectFields", "UpdatedAt", c => c.DateTime(nullable: false));
        AlterColumn("dbo.ProjectFields", "UpdatedById", c => c.Int(nullable: false));

        CreateIndex("dbo.ProjectFields", "CreatedById");
        CreateIndex("dbo.ProjectFields", "UpdatedById");
        AddForeignKey("dbo.ProjectFields", "CreatedById", "dbo.Users", "UserId");
        AddForeignKey("dbo.ProjectFields", "UpdatedById", "dbo.Users", "UserId");
    }

    public override void Down()
    {
        DropForeignKey("dbo.ProjectFields", "UpdatedById", "dbo.Users");
        DropForeignKey("dbo.ProjectFields", "CreatedById", "dbo.Users");
        DropIndex("dbo.ProjectFields", new[] { "UpdatedById" });
        DropIndex("dbo.ProjectFields", new[] { "CreatedById" });
        DropColumn("dbo.ProjectFields", "UpdatedById");
        DropColumn("dbo.ProjectFields", "UpdatedAt");
        DropColumn("dbo.ProjectFields", "CreatedById");
        DropColumn("dbo.ProjectFields", "CreatedAt");
    }
}
