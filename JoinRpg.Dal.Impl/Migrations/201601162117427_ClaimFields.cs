namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;

  public partial class ClaimFields : DbMigration
  {
    public override void Up()
    {
      RenameTable(name: "dbo.ProjectCharacterFields", newName: "ProjectFields");
      RenameColumn("dbo.ProjectFields", "ProjectCharacterFieldId", "ProjectFieldId");
      RenameColumn("dbo.ProjectFields", "FieldHint_Contents", "Description_Contents");
      AddColumn("dbo.ProjectFields", "FieldBoundTo", c => c.Int(nullable: false, defaultValue: 0));
      RenameTable(name: "dbo.ProjectCharacterFieldDropdownValues", newName: "ProjectFieldDropdownValues");
      RenameColumn("dbo.ProjectFieldDropdownValues", "ProjectCharacterFieldDropdownValueId", "ProjectFieldDropdownValueId");
      RenameColumn("dbo.ProjectFieldDropdownValues", "ProjectCharacterFieldId", "ProjectFieldId");
    }

    public override void Down()
    {
      RenameColumn("dbo.ProjectFieldDropdownValues", "ProjectFieldDropdownValueId", "ProjectCharacterFieldDropdownValueId");
      RenameColumn("dbo.ProjectFieldDropdownValues", "ProjectFieldId","ProjectCharacterFieldId");
      RenameTable(newName: "ProjectCharacterFieldDropdownValues", name: "dbo.ProjectFieldDropdownValues");
      RenameColumn("dbo.ProjectFields", "ProjectFieldId", "ProjectCharacterFieldId");
      DropColumn("dbo.ProjectFields", "FieldBoundTo");
      RenameColumn("dbo.ProjectFields", "Description_Contents", "FieldHint_Contents");
      RenameTable(newName: "ProjectCharacterFields", name: "dbo.ProjectFields");
    }
  }
}
