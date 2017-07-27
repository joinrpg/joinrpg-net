namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class ClaimApplyRules : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.ProjectAcls");
            CreateTable(
                "dbo.ProjectDetails",
                c => new
                    {
                        ProjectId = c.Int(nullable: false),
                        ClaimApplyRules_Contents = c.String(),
                    })
                .PrimaryKey(t => t.ProjectId)
                .ForeignKey("dbo.Projects", t => t.ProjectId)
                .Index(t => t.ProjectId);
            
            AddColumn("dbo.ProjectAcls", "CanChangeProjectProperties", c => c.Boolean(nullable: false, defaultValue: true));
            DropColumn("dbo.ProjectAcls", "ProjectAclId");
            AddColumn("dbo.ProjectAcls", "ProjectAclId", c => c.Int(nullable: false, identity:true));
            AddPrimaryKey("dbo.ProjectAcls", "ProjectAclId");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ProjectDetails", "ProjectId", "dbo.Projects");
            DropIndex("dbo.ProjectDetails", new[] { "ProjectId" });
            DropPrimaryKey("dbo.ProjectAcls");
            AlterColumn("dbo.ProjectAcls", "ProjectAclId", c => c.Int(nullable: false));
            DropColumn("dbo.ProjectAcls", "CanChangeProjectProperties");
            DropTable("dbo.ProjectDetails");
            AddPrimaryKey("dbo.ProjectAcls", new[] { "UserId", "ProjectId" });
        }
    }
}
