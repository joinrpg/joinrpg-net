namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class VersionedPlot : DbMigration
    {
      public override void Up()
      {
        DropForeignKey("dbo.PlotElementTexts", "PlotElementId", "dbo.PlotElements");
        DropPrimaryKey("dbo.PlotElementTexts");
        AddColumn("dbo.PlotElements", "Published", c => c.Int());
        Sql(@"
UPDATE dbo.PlotElements 
SET Published = 0
FROM PlotElements PE
WHERE PE.IsCompleted =1
");
      AddColumn("dbo.PlotElementTexts", "Version", c => c.Int(nullable: false));
        AddColumn("dbo.PlotElementTexts", "ModifiedDateTime", c => c.DateTime(nullable: true));
        Sql(@"
UPDATE dbo.PlotElementTexts 
SET ModifiedDateTime = PE.ModifiedDateTime
FROM dbo.PlotElementTexts PET
INNER JOIN dbo.PlotElements PE ON PE.PlotElementId = PET.PlotElementId
");
      //Split current published texts with TodoField into 2 versions — 0.Published; 1.With TodoField not empty
      //so it correctly shown as "working on published plot element".
        Sql(@"INSERT INTO [dbo].[PlotElementTexts]
           ([PlotElementId]
           ,[Content_Contents]
           ,[TodoField]
           ,[Version]
           ,[ModifiedDateTime])
SELECT PET.PlotElementId, Content_Contents, TodoField, 1, PET.ModifiedDateTime
FROM PlotElementTexts PET 
INNER JOIN PlotElements PE ON PE.PlotElementId = PET.PlotElementId
WHERE ISNULL(TodoField, '') <> ''
AND PE.Published IS NOT NULL

UPDATE PlotElementTexts 
SET TodoField = NULL
FROM PlotElementTexts PET 
INNER JOIN PlotElements PE ON PE.PlotElementId = PET.PlotElementId
WHERE ISNULL(TodoField, '') <> ''
AND PE.Published = PET.Version");

        AlterColumn("dbo.PlotElementTexts", "ModifiedDateTime", c => c.DateTime(nullable: false));
        AddColumn("dbo.PlotElementTexts", "AuthorUserId", c => c.Int());
        AddPrimaryKey("dbo.PlotElementTexts", new[] {"PlotElementId", "Version"});
        CreateIndex("dbo.PlotElementTexts", "AuthorUserId");
        AddForeignKey("dbo.PlotElementTexts", "AuthorUserId", "dbo.Users", "UserId");
        AddForeignKey("dbo.PlotElementTexts", "PlotElementId", "dbo.PlotElements", "PlotElementId",
          cascadeDelete: true);
      }

      public override void Down()
        {
            DropForeignKey("dbo.PlotElementTexts", "PlotElementId", "dbo.PlotElements");
            DropForeignKey("dbo.PlotElementTexts", "AuthorUserId", "dbo.Users");
            DropIndex("dbo.PlotElementTexts", new[] { "AuthorUserId" });
            DropPrimaryKey("dbo.PlotElementTexts");
            DropColumn("dbo.PlotElementTexts", "AuthorUserId");
            DropColumn("dbo.PlotElementTexts", "ModifiedDateTime");
            DropColumn("dbo.PlotElementTexts", "Version");
            DropColumn("dbo.PlotElements", "Published");
            AddPrimaryKey("dbo.PlotElementTexts", "PlotElementId");
            AddForeignKey("dbo.PlotElementTexts", "PlotElementId", "dbo.PlotElements", "PlotElementId");
        }
    }
}
