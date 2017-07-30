namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class Comments : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Comments",
                c => new
                    {
                        CommentId = c.Int(nullable: false, identity: true),
                        ProjectId = c.Int(nullable: false),
                        ClaimId = c.Int(nullable: false),
                        ParentCommentId = c.Int(),
                        CommentText_Contents = c.String(),
                        CreatedTime = c.DateTime(nullable: false),
                        LastEditTime = c.DateTime(nullable: false),
                        AuthorUserId = c.Int(nullable: false),
                        IsCommentByPlayer = c.Boolean(nullable: false),
                        IsVisibleToPlayer = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.CommentId)
                .ForeignKey("dbo.Users", t => t.AuthorUserId)
                .ForeignKey("dbo.Comments", t => t.ParentCommentId)
                .ForeignKey("dbo.Projects", t => t.ProjectId)
                .ForeignKey("dbo.Claims", t => t.ClaimId, cascadeDelete: true)
                .Index(t => t.ProjectId)
                .Index(t => t.ClaimId)
                .Index(t => t.ParentCommentId)
                .Index(t => t.AuthorUserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Comments", "ClaimId", "dbo.Claims");
            DropForeignKey("dbo.Comments", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.Comments", "ParentCommentId", "dbo.Comments");
            DropForeignKey("dbo.Comments", "AuthorUserId", "dbo.Users");
            DropIndex("dbo.Comments", new[] { "AuthorUserId" });
            DropIndex("dbo.Comments", new[] { "ParentCommentId" });
            DropIndex("dbo.Comments", new[] { "ClaimId" });
            DropIndex("dbo.Comments", new[] { "ProjectId" });
            DropTable("dbo.Comments");
        }
    }
}
