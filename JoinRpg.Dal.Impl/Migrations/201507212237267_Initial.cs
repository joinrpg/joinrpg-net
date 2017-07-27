namespace JoinRpg.Dal.Impl.Migrations
{
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
                "dbo.CharacterGroups",
                c => new
                    {
                        CharacterGroupId = c.Int(nullable: false, identity: true),
                        ProjectId = c.Int(nullable: false),
                        CharacterGroupName = c.String(),
                        IsRoot = c.Boolean(nullable: false),
                        IsPublic = c.Boolean(nullable: false),
                        AvaiableDirectSlots = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.CharacterGroupId)
                .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: true)
                .Index(t => t.ProjectId);
            
            CreateTable(
                "dbo.Characters",
                c => new
                    {
                        CharacterId = c.Int(nullable: false, identity: true),
                        ProjectId = c.Int(nullable: false),
                        CharacterName = c.String(),
                        IsPublic = c.Boolean(nullable: false),
                        IsAcceptingClaims = c.Boolean(nullable: false),
                        JsonData = c.String(),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.CharacterId)
                .ForeignKey("dbo.Projects", t => t.ProjectId)
                .Index(t => t.ProjectId);
            
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
                        WasEverUsed = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ProjectCharacterFieldId)
                .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: true)
                .Index(t => t.ProjectId);
            
            CreateTable(
                "dbo.CharacterCharacterGroups",
                c => new
                    {
                        Character_CharacterId = c.Int(nullable: false),
                        CharacterGroup_CharacterGroupId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Character_CharacterId, t.CharacterGroup_CharacterGroupId })
                .ForeignKey("dbo.Characters", t => t.Character_CharacterId, cascadeDelete: true)
                .ForeignKey("dbo.CharacterGroups", t => t.CharacterGroup_CharacterGroupId, cascadeDelete: true)
                .Index(t => t.Character_CharacterId)
                .Index(t => t.CharacterGroup_CharacterGroupId);
            
            CreateTable(
                "dbo.CharacterGroupCharacterGroups",
                c => new
                    {
                        CharacterGroup_CharacterGroupId = c.Int(nullable: false),
                        CharacterGroup_CharacterGroupId1 = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.CharacterGroup_CharacterGroupId, t.CharacterGroup_CharacterGroupId1 })
                .ForeignKey("dbo.CharacterGroups", t => t.CharacterGroup_CharacterGroupId)
                .ForeignKey("dbo.CharacterGroups", t => t.CharacterGroup_CharacterGroupId1)
                .Index(t => t.CharacterGroup_CharacterGroupId)
                .Index(t => t.CharacterGroup_CharacterGroupId1);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ProjectCharacterFields", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.Projects", "CreatorUserId", "dbo.Users");
            DropForeignKey("dbo.ProjectAcls", "UserId", "dbo.Users");
            DropForeignKey("dbo.ProjectAcls", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.Characters", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.CharacterGroups", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.CharacterGroupCharacterGroups", "CharacterGroup_CharacterGroupId1", "dbo.CharacterGroups");
            DropForeignKey("dbo.CharacterGroupCharacterGroups", "CharacterGroup_CharacterGroupId", "dbo.CharacterGroups");
            DropForeignKey("dbo.CharacterCharacterGroups", "CharacterGroup_CharacterGroupId", "dbo.CharacterGroups");
            DropForeignKey("dbo.CharacterCharacterGroups", "Character_CharacterId", "dbo.Characters");
            DropIndex("dbo.CharacterGroupCharacterGroups", new[] { "CharacterGroup_CharacterGroupId1" });
            DropIndex("dbo.CharacterGroupCharacterGroups", new[] { "CharacterGroup_CharacterGroupId" });
            DropIndex("dbo.CharacterCharacterGroups", new[] { "CharacterGroup_CharacterGroupId" });
            DropIndex("dbo.CharacterCharacterGroups", new[] { "Character_CharacterId" });
            DropIndex("dbo.ProjectCharacterFields", new[] { "ProjectId" });
            DropIndex("dbo.ProjectAcls", new[] { "ProjectId" });
            DropIndex("dbo.ProjectAcls", new[] { "UserId" });
            DropIndex("dbo.Characters", new[] { "ProjectId" });
            DropIndex("dbo.CharacterGroups", new[] { "ProjectId" });
            DropIndex("dbo.Projects", new[] { "CreatorUserId" });
            DropTable("dbo.CharacterGroupCharacterGroups");
            DropTable("dbo.CharacterCharacterGroups");
            DropTable("dbo.ProjectCharacterFields");
            DropTable("dbo.ProjectAcls");
            DropTable("dbo.Users");
            DropTable("dbo.Characters");
            DropTable("dbo.CharacterGroups");
            DropTable("dbo.Projects");
        }
    }
}
