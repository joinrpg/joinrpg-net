namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class Plots : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PlotElements",
                c => new
                    {
                        PlotElementId = c.Int(nullable: false, identity: true),
                        ProjectId = c.Int(nullable: false),
                        PlotFolderId = c.Int(nullable: false),
                        MasterSummary_Contents = c.String(),
                        Content_Contents = c.String(),
                        TodoField = c.String(),
                        CreatedDateTime = c.DateTime(nullable: false),
                        ModifiedDateTime = c.DateTime(nullable: false),
                        IsCompleted = c.Boolean(nullable: false),
                        IsObsolete = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.PlotElementId)
                .ForeignKey("dbo.PlotFolders", t => t.PlotFolderId, cascadeDelete: true)
                .ForeignKey("dbo.Projects", t => t.ProjectId)
                .Index(t => t.ProjectId)
                .Index(t => t.PlotFolderId);
            
            CreateTable(
                "dbo.PlotFolders",
                c => new
                    {
                        PlotFolderId = c.Int(nullable: false, identity: true),
                        ProjectId = c.Int(nullable: false),
                        MasterTitle = c.String(),
                        MasterSummary_Contents = c.String(),
                        TodoField = c.String(),
                        CreatedDateTime = c.DateTime(nullable: false),
                        ModifiedDateTime = c.DateTime(nullable: false),
                        IsObsolete = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.PlotFolderId)
                .ForeignKey("dbo.Projects", t => t.ProjectId)
                .Index(t => t.ProjectId);
            
            CreateTable(
                "dbo.PlotFolderCharacterGroups",
                c => new
                    {
                        PlotFolder_PlotFolderId = c.Int(nullable: false),
                        CharacterGroup_CharacterGroupId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.PlotFolder_PlotFolderId, t.CharacterGroup_CharacterGroupId })
                .ForeignKey("dbo.PlotFolders", t => t.PlotFolder_PlotFolderId, cascadeDelete: true)
                .ForeignKey("dbo.CharacterGroups", t => t.CharacterGroup_CharacterGroupId, cascadeDelete: true)
                .Index(t => t.PlotFolder_PlotFolderId)
                .Index(t => t.CharacterGroup_CharacterGroupId);
            
            CreateTable(
                "dbo.PlotElementCharacters",
                c => new
                    {
                        PlotElement_PlotElementId = c.Int(nullable: false),
                        Character_CharacterId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.PlotElement_PlotElementId, t.Character_CharacterId })
                .ForeignKey("dbo.PlotElements", t => t.PlotElement_PlotElementId, cascadeDelete: true)
                .ForeignKey("dbo.Characters", t => t.Character_CharacterId, cascadeDelete: true)
                .Index(t => t.PlotElement_PlotElementId)
                .Index(t => t.Character_CharacterId);
            
            CreateTable(
                "dbo.PlotElementCharacterGroups",
                c => new
                    {
                        PlotElement_PlotElementId = c.Int(nullable: false),
                        CharacterGroup_CharacterGroupId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.PlotElement_PlotElementId, t.CharacterGroup_CharacterGroupId })
                .ForeignKey("dbo.PlotElements", t => t.PlotElement_PlotElementId, cascadeDelete: true)
                .ForeignKey("dbo.CharacterGroups", t => t.CharacterGroup_CharacterGroupId, cascadeDelete: true)
                .Index(t => t.PlotElement_PlotElementId)
                .Index(t => t.CharacterGroup_CharacterGroupId);
            
            AddColumn("dbo.Characters", "PlotElementOrderData", c => c.String());
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PlotElementCharacterGroups", "CharacterGroup_CharacterGroupId", "dbo.CharacterGroups");
            DropForeignKey("dbo.PlotElementCharacterGroups", "PlotElement_PlotElementId", "dbo.PlotElements");
            DropForeignKey("dbo.PlotElementCharacters", "Character_CharacterId", "dbo.Characters");
            DropForeignKey("dbo.PlotElementCharacters", "PlotElement_PlotElementId", "dbo.PlotElements");
            DropForeignKey("dbo.PlotElements", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.PlotFolderCharacterGroups", "CharacterGroup_CharacterGroupId", "dbo.CharacterGroups");
            DropForeignKey("dbo.PlotFolderCharacterGroups", "PlotFolder_PlotFolderId", "dbo.PlotFolders");
            DropForeignKey("dbo.PlotFolders", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.PlotElements", "PlotFolderId", "dbo.PlotFolders");
            DropIndex("dbo.PlotElementCharacterGroups", new[] { "CharacterGroup_CharacterGroupId" });
            DropIndex("dbo.PlotElementCharacterGroups", new[] { "PlotElement_PlotElementId" });
            DropIndex("dbo.PlotElementCharacters", new[] { "Character_CharacterId" });
            DropIndex("dbo.PlotElementCharacters", new[] { "PlotElement_PlotElementId" });
            DropIndex("dbo.PlotFolderCharacterGroups", new[] { "CharacterGroup_CharacterGroupId" });
            DropIndex("dbo.PlotFolderCharacterGroups", new[] { "PlotFolder_PlotFolderId" });
            DropIndex("dbo.PlotFolders", new[] { "ProjectId" });
            DropIndex("dbo.PlotElements", new[] { "PlotFolderId" });
            DropIndex("dbo.PlotElements", new[] { "ProjectId" });
            DropColumn("dbo.Characters", "PlotElementOrderData");
            DropTable("dbo.PlotElementCharacterGroups");
            DropTable("dbo.PlotElementCharacters");
            DropTable("dbo.PlotFolderCharacterGroups");
            DropTable("dbo.PlotFolders");
            DropTable("dbo.PlotElements");
        }
    }
}
