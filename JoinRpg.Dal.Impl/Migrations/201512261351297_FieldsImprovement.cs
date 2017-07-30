namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class FieldsImprovement : DbMigration
    {
      public override void Up()
      {
        AddColumn("dbo.CharacterGroups", "IsSpecial", c => c.Boolean(nullable: false));
        AddColumn("dbo.ProjectCharacterFields", "FieldHint_Contents", c => c.String());
        AddColumn("dbo.ProjectCharacterFieldDropdownValues", "Description_Contents", c => c.String());

        Sql("UPDATE [dbo].[ProjectCharacterFields] SET FieldHint_Contents = FieldHint");
        Sql("UPDATE [dbo].[ProjectCharacterFieldDropdownValues] SET Description_Contents = Description_Contents");

        AddColumn("dbo.ProjectCharacterFieldDropdownValues", "CharacterGroupId", c => c.Int(nullable: false));
        AddColumn("dbo.ProjectCharacterFieldDropdownValues", "CharacterGroup_CharacterGroupId", c => c.Int());
        CreateIndex("dbo.ProjectCharacterFieldDropdownValues", "CharacterGroup_CharacterGroupId");
        AddForeignKey("dbo.ProjectCharacterFieldDropdownValues", "CharacterGroup_CharacterGroupId",
          "dbo.CharacterGroups", "CharacterGroupId");
        DropColumn("dbo.ProjectCharacterFields", "FieldHint");
        DropColumn("dbo.ProjectCharacterFieldDropdownValues", "Description");
      }

      public override void Down()
        {
            AddColumn("dbo.ProjectCharacterFieldDropdownValues", "Description", c => c.String());
            AddColumn("dbo.ProjectCharacterFields", "FieldHint", c => c.String());
            DropForeignKey("dbo.ProjectCharacterFieldDropdownValues", "CharacterGroup_CharacterGroupId", "dbo.CharacterGroups");
            DropIndex("dbo.ProjectCharacterFieldDropdownValues", new[] { "CharacterGroup_CharacterGroupId" });
            DropColumn("dbo.ProjectCharacterFieldDropdownValues", "CharacterGroup_CharacterGroupId");
            DropColumn("dbo.ProjectCharacterFieldDropdownValues", "CharacterGroupId");
            DropColumn("dbo.ProjectCharacterFieldDropdownValues", "Description_Contents");
            DropColumn("dbo.ProjectCharacterFields", "FieldHint_Contents");
            DropColumn("dbo.CharacterGroups", "IsSpecial");
        }
    }
}
