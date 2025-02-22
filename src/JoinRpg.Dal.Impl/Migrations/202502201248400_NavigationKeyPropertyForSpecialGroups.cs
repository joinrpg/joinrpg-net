namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class NavigationKeyPropertyForSpecialGroups : DbMigration
{
    public override void Up()
    {
        RenameColumn(table: "dbo.ProjectFields", name: "CharacterGroup_CharacterGroupId", newName: "CharacterGroupId");
        RenameColumn(table: "dbo.ProjectFieldDropdownValues", name: "CharacterGroup_CharacterGroupId", newName: "CharacterGroupId");
        RenameIndex(table: "dbo.ProjectFields", name: "IX_CharacterGroup_CharacterGroupId", newName: "IX_CharacterGroupId");
        RenameIndex(table: "dbo.ProjectFieldDropdownValues", name: "IX_CharacterGroup_CharacterGroupId", newName: "IX_CharacterGroupId");
    }

    public override void Down()
    {
        RenameIndex(table: "dbo.ProjectFieldDropdownValues", name: "IX_CharacterGroupId", newName: "IX_CharacterGroup_CharacterGroupId");
        RenameIndex(table: "dbo.ProjectFields", name: "IX_CharacterGroupId", newName: "IX_CharacterGroup_CharacterGroupId");
        RenameColumn(table: "dbo.ProjectFieldDropdownValues", name: "CharacterGroupId", newName: "CharacterGroup_CharacterGroupId");
        RenameColumn(table: "dbo.ProjectFields", name: "CharacterGroupId", newName: "CharacterGroup_CharacterGroupId");
    }
}
