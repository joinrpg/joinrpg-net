namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class SelectiveFields : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ProjectFieldCharacterGroups",
                c => new
                    {
                        ProjectField_ProjectFieldId = c.Int(nullable: false),
                        CharacterGroup_CharacterGroupId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.ProjectField_ProjectFieldId, t.CharacterGroup_CharacterGroupId })
                .ForeignKey("dbo.ProjectFields", t => t.ProjectField_ProjectFieldId, cascadeDelete: false)
                .ForeignKey("dbo.CharacterGroups", t => t.CharacterGroup_CharacterGroupId, cascadeDelete: false)
                .Index(t => t.ProjectField_ProjectFieldId)
                .Index(t => t.CharacterGroup_CharacterGroupId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ProjectFieldCharacterGroups", "CharacterGroup_CharacterGroupId", "dbo.CharacterGroups");
            DropForeignKey("dbo.ProjectFieldCharacterGroups", "ProjectField_ProjectFieldId", "dbo.ProjectFields");
            DropIndex("dbo.ProjectFieldCharacterGroups", new[] { "CharacterGroup_CharacterGroupId" });
            DropIndex("dbo.ProjectFieldCharacterGroups", new[] { "ProjectField_ProjectFieldId" });
            DropTable("dbo.ProjectFieldCharacterGroups");
        }
    }
}
