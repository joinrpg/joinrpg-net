namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class SplitPlots : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PlotElementTexts",
                c => new
                    {
                        PlotElementId = c.Int(nullable: false),
                        Content_Contents = c.String(),
                        TodoField = c.String(),
                    })
                .PrimaryKey(t => t.PlotElementId)
                .ForeignKey("dbo.PlotElements", t => t.PlotElementId)
                .Index(t => t.PlotElementId);
      Sql(@"INSERT INTO [dbo].[PlotElementTexts] ([PlotElementId] ,[Content_Contents], [TodoField])
SELECT PlotElementId, Content_Contents, [TodoField] FROM [dbo].[PlotElements]
");

      DropColumn("dbo.PlotElements", "MasterSummary_Contents");
            DropColumn("dbo.PlotElements", "Content_Contents");
            DropColumn("dbo.PlotElements", "TodoField");
        }
        
        public override void Down()
        {
            AddColumn("dbo.PlotElements", "TodoField", c => c.String());
            AddColumn("dbo.PlotElements", "Content_Contents", c => c.String());
            AddColumn("dbo.PlotElements", "MasterSummary_Contents", c => c.String());
            DropForeignKey("dbo.PlotElementTexts", "PlotElementId", "dbo.PlotElements");
            DropIndex("dbo.PlotElementTexts", new[] { "PlotElementId" });
            DropTable("dbo.PlotElementTexts");
        }
    }
}
