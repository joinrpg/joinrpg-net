namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class FieldDropdowns : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ProjectCharacterFieldDropdownValues",
                c => new
                    {
                        ProjectCharacterFieldDropdownValueId = c.Int(nullable: false, identity: true),
                        ProjectCharacterFieldId = c.Int(nullable: false),
                        ProjectId = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        WasEverUsed = c.Boolean(nullable: false),
                        Label = c.String(),
                        Description = c.String(),
                    })
                .PrimaryKey(t => t.ProjectCharacterFieldDropdownValueId)
                .ForeignKey("dbo.Projects", t => t.ProjectId)
                .ForeignKey("dbo.ProjectCharacterFields", t => t.ProjectCharacterFieldId, cascadeDelete: true)
                .Index(t => t.ProjectCharacterFieldId)
                .Index(t => t.ProjectId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ProjectCharacterFieldDropdownValues", "ProjectCharacterFieldId", "dbo.ProjectCharacterFields");
            DropForeignKey("dbo.ProjectCharacterFieldDropdownValues", "ProjectId", "dbo.Projects");
            DropIndex("dbo.ProjectCharacterFieldDropdownValues", new[] { "ProjectId" });
            DropIndex("dbo.ProjectCharacterFieldDropdownValues", new[] { "ProjectCharacterFieldId" });
            DropTable("dbo.ProjectCharacterFieldDropdownValues");
        }
    }
}
