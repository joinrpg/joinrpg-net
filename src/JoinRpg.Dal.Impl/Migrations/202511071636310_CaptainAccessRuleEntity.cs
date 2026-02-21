namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class CaptainAccessRuleEntity : DbMigration
{
    public override void Up()
    {
        CreateTable(
            "dbo.CaptainAccessRuleEntities",
            c => new
            {
                CaptainAccessRuleEntityId = c.Int(nullable: false, identity: true),
                ProjectId = c.Int(nullable: false),
                CharacterGroupId = c.Int(nullable: false),
                CaptainUserId = c.Int(nullable: false),
                CanApprove = c.Boolean(nullable: false),
            })
            .PrimaryKey(t => t.CaptainAccessRuleEntityId)
            .ForeignKey("dbo.Users", t => t.CaptainUserId)
            .ForeignKey("dbo.CharacterGroups", t => t.CharacterGroupId)
            .ForeignKey("dbo.Projects", t => t.ProjectId)
            .Index(t => t.ProjectId)
            .Index(t => t.CharacterGroupId)
            .Index(t => t.CaptainUserId);

    }

    public override void Down()
    {
        DropForeignKey("dbo.CaptainAccessRuleEntities", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.CaptainAccessRuleEntities", "CharacterGroupId", "dbo.CharacterGroups");
        DropForeignKey("dbo.CaptainAccessRuleEntities", "CaptainUserId", "dbo.Users");
        DropIndex("dbo.CaptainAccessRuleEntities", new[] { "CaptainUserId" });
        DropIndex("dbo.CaptainAccessRuleEntities", new[] { "CharacterGroupId" });
        DropIndex("dbo.CaptainAccessRuleEntities", new[] { "ProjectId" });
        DropTable("dbo.CaptainAccessRuleEntities");
    }
}
