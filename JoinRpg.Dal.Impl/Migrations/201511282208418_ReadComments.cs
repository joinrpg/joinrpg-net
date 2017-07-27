namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class ReadComments : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ReadCommentWatermarks",
                c => new
                    {
                        ReadCommentWatermarkId = c.Int(nullable: false, identity: true),
                        ProjectId = c.Int(nullable: false),
                        ClaimId = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                        CommentId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ReadCommentWatermarkId)
                .ForeignKey("dbo.Claims", t => t.ClaimId, cascadeDelete: true)
                .ForeignKey("dbo.Comments", t => t.CommentId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.ClaimId)
                .Index(t => t.UserId)
                .Index(t => t.CommentId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ReadCommentWatermarks", "UserId", "dbo.Users");
            DropForeignKey("dbo.ReadCommentWatermarks", "CommentId", "dbo.Comments");
            DropForeignKey("dbo.ReadCommentWatermarks", "ClaimId", "dbo.Claims");
            DropIndex("dbo.ReadCommentWatermarks", new[] { "CommentId" });
            DropIndex("dbo.ReadCommentWatermarks", new[] { "UserId" });
            DropIndex("dbo.ReadCommentWatermarks", new[] { "ClaimId" });
            DropTable("dbo.ReadCommentWatermarks");
        }
    }
}
