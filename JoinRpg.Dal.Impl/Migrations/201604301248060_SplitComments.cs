namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class SplitComments : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CommentTexts",
                c => new
                    {
                        CommentId = c.Int(nullable: false),
                        Text_Contents = c.String(),
                    })
                .PrimaryKey(t => t.CommentId)
                .ForeignKey("dbo.Comments", t => t.CommentId)
                .Index(t => t.CommentId);
          Sql(@"INSERT INTO [dbo].[CommentTexts] ([CommentId] ,[Text_Contents])
SELECT CommentId, CommentText_Contents FROM [dbo].[Comments]
");            
            DropColumn("dbo.Comments", "CommentText_Contents");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Comments", "CommentText_Contents", c => c.String());
            DropForeignKey("dbo.CommentTexts", "CommentId", "dbo.Comments");
            DropIndex("dbo.CommentTexts", new[] { "CommentId" });
            DropTable("dbo.CommentTexts");
        }
    }
}
