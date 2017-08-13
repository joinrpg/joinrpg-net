namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
  using JetBrains.Annotations;

  [UsedImplicitly]
  public partial class Report2D : DbMigration
  {
    public override void Up()
    {
      CreateTable(
          "dbo.GameReport2DTemplate",
          c => new
          {
            GameReport2DTemplateId = c.Int(nullable: false, identity: true),
            GameReport2DTemplateName = c.String(),
            ProjectId = c.Int(nullable: false),
            FirstCharacterGroupId = c.Int(nullable: false),
            SecondCharacterGroupId = c.Int(nullable: false),
            CreatedAt = c.DateTime(nullable: false),
            CreatedById = c.Int(nullable: false),
            UpdatedAt = c.DateTime(nullable: false),
            UpdatedById = c.Int(nullable: false),
          })
        .PrimaryKey(t => t.GameReport2DTemplateId)
        .ForeignKey("dbo.Users", t => t.CreatedById)
        .ForeignKey("dbo.CharacterGroups", t => t.FirstCharacterGroupId)
        .ForeignKey("dbo.Projects", t => t.ProjectId)
        .ForeignKey("dbo.CharacterGroups", t => t.SecondCharacterGroupId)
        .ForeignKey("dbo.Users", t => t.UpdatedById)
        .Index(t => t.ProjectId)
        .Index(t => t.FirstCharacterGroupId)
        .Index(t => t.SecondCharacterGroupId)
        .Index(t => t.CreatedById)
        .Index(t => t.UpdatedById);

      AddColumn("dbo.ProjectFieldDropdownValues", "MasterDescription_Contents", c => c.String());
      AddColumn("dbo.ProjectFieldDropdownValues", "ProgrammaticValue", c => c.String());
    }

    public override void Down()
    {
      DropForeignKey("dbo.GameReport2DTemplate", "UpdatedById", "dbo.Users");
      DropForeignKey("dbo.GameReport2DTemplate", "SecondCharacterGroupId", "dbo.CharacterGroups");
      DropForeignKey("dbo.GameReport2DTemplate", "ProjectId", "dbo.Projects");
      DropForeignKey("dbo.GameReport2DTemplate", "FirstCharacterGroupId", "dbo.CharacterGroups");
      DropForeignKey("dbo.GameReport2DTemplate", "CreatedById", "dbo.Users");
      DropIndex("dbo.GameReport2DTemplate", new[] {"UpdatedById"});
      DropIndex("dbo.GameReport2DTemplate", new[] {"CreatedById"});
      DropIndex("dbo.GameReport2DTemplate", new[] {"SecondCharacterGroupId"});
      DropIndex("dbo.GameReport2DTemplate", new[] {"FirstCharacterGroupId"});
      DropIndex("dbo.GameReport2DTemplate", new[] {"ProjectId"});
      DropColumn("dbo.ProjectFieldDropdownValues", "ProgrammaticValue");
      DropColumn("dbo.ProjectFieldDropdownValues", "MasterDescription_Contents");
      DropTable("dbo.GameReport2DTemplate");
    }
  }
}
