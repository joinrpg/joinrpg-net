namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CharGroups : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CharacterGroups",
                c => new
                    {
                        CharacterGroupId = c.Int(nullable: false, identity: true),
                        ProjectId = c.Int(nullable: false),
                        CharacterGroupName = c.String(),
                        IsRoot = c.Boolean(nullable: false),
                        IsPublic = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.CharacterGroupId)
                .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: true)
                .Index(t => t.ProjectId);
            
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
            DropForeignKey("dbo.CharacterGroups", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.CharacterGroupCharacterGroups", "CharacterGroup_CharacterGroupId1", "dbo.CharacterGroups");
            DropForeignKey("dbo.CharacterGroupCharacterGroups", "CharacterGroup_CharacterGroupId", "dbo.CharacterGroups");
            DropIndex("dbo.CharacterGroupCharacterGroups", new[] { "CharacterGroup_CharacterGroupId1" });
            DropIndex("dbo.CharacterGroupCharacterGroups", new[] { "CharacterGroup_CharacterGroupId" });
            DropIndex("dbo.CharacterGroups", new[] { "ProjectId" });
            DropTable("dbo.CharacterGroupCharacterGroups");
            DropTable("dbo.CharacterGroups");
        }
    }
}
