namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class FieldsImprovement2 : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.ProjectCharacterFields", "CharacterGroupId", c => c.Int(nullable: false));
        AddColumn("dbo.ProjectCharacterFields", "CharacterGroup_CharacterGroupId", c => c.Int());
        CreateIndex("dbo.ProjectCharacterFields", "CharacterGroup_CharacterGroupId");
        AddForeignKey("dbo.ProjectCharacterFields", "CharacterGroup_CharacterGroupId", "dbo.CharacterGroups", "CharacterGroupId");
    }

    public override void Down()
    {
        DropForeignKey("dbo.ProjectCharacterFields", "CharacterGroup_CharacterGroupId", "dbo.CharacterGroups");
        DropIndex("dbo.ProjectCharacterFields", new[] { "CharacterGroup_CharacterGroupId" });
        DropColumn("dbo.ProjectCharacterFields", "CharacterGroup_CharacterGroupId");
        DropColumn("dbo.ProjectCharacterFields", "CharacterGroupId");
    }
}
