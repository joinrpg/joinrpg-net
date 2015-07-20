namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Projects",
                c => new
                    {
                        ProjectId = c.Int(nullable: false, identity: true),
                        ProjectName = c.String(),
                        CreatedDate = c.DateTime(nullable: false),
                        CreatorUserId = c.Int(nullable: false),
                        Active = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ProjectId)
                .ForeignKey("dbo.Users", t => t.CreatorUserId)
                .Index(t => t.CreatorUserId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        UserId = c.Int(nullable: false, identity: true),
                        BornName = c.String(),
                        FatherName = c.String(),
                        SurName = c.String(),
                        LegacyAllRpgInp = c.Int(),
                        UserName = c.String(),
                        Email = c.String(),
                        PasswordHash = c.String(),
                        PhoneNumber = c.String(),
                    })
                .PrimaryKey(t => t.UserId);
            
            CreateTable(
                "dbo.ProjectAcls",
                c => new
                    {
                        UserId = c.Int(nullable: false),
                        ProjectId = c.Int(nullable: false),
                        ProjectAclId = c.Int(nullable: false),
                        CanChangeFields = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => new { t.UserId, t.ProjectId })
                .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.ProjectId);
            
            CreateTable(
                "dbo.ProjectCharacterFields",
                c => new
                    {
                        ProjectCharacterFieldId = c.Int(nullable: false, identity: true),
                        FieldName = c.String(),
                        FieldType = c.Int(nullable: false),
                        IsPublic = c.Boolean(nullable: false),
                        CanPlayerView = c.Boolean(nullable: false),
                        CanPlayerEdit = c.Boolean(nullable: false),
                        FieldHint = c.String(),
                        Order = c.Int(nullable: false),
                        ProjectId = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ProjectCharacterFieldId)
                .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: true)
                .Index(t => t.ProjectId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ProjectCharacterFields", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.Projects", "CreatorUserId", "dbo.Users");
            DropForeignKey("dbo.ProjectAcls", "UserId", "dbo.Users");
            DropForeignKey("dbo.ProjectAcls", "ProjectId", "dbo.Projects");
            DropIndex("dbo.ProjectCharacterFields", new[] { "ProjectId" });
            DropIndex("dbo.ProjectAcls", new[] { "ProjectId" });
            DropIndex("dbo.ProjectAcls", new[] { "UserId" });
            DropIndex("dbo.Projects", new[] { "CreatorUserId" });
            DropTable("dbo.ProjectCharacterFields");
            DropTable("dbo.ProjectAcls");
            DropTable("dbo.Users");
            DropTable("dbo.Projects");
        }
    }
}
