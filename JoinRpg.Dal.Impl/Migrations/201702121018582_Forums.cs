namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class Forums : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Comments", "ClaimId", "dbo.Claims");
            DropForeignKey("dbo.ReadCommentWatermarks", "ClaimId", "dbo.Claims");
            DropIndex("dbo.Comments", new[] { "ClaimId" });
            DropIndex("dbo.ReadCommentWatermarks", new[] { "ClaimId" });
            CreateTable(
                "dbo.ForumThreads",
                c => new
                    {
                        ForumThreadId = c.Int(nullable: false, identity: true),
                        CharacterGroupId = c.Int(nullable: false),
                        ProjectId = c.Int(nullable: false),
                        Header = c.String(),
                        CommentDiscussionId = c.Int(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                        ModifiedAt = c.DateTime(nullable: false),
                        AuthorUserId = c.Int(nullable: false),
                        IsVisibleToPlayer = c.Boolean(nullable: false),
                        Project_ProjectId = c.Int(),
                    })
                .PrimaryKey(t => t.ForumThreadId)
                .ForeignKey("dbo.Users", t => t.AuthorUserId, cascadeDelete: true)
                .ForeignKey("dbo.CharacterGroups", t => t.CharacterGroupId, cascadeDelete: true)
                .ForeignKey("dbo.CommentDiscussions", t => t.CommentDiscussionId)
                .ForeignKey("dbo.Projects", t => t.ProjectId)
                .ForeignKey("dbo.Projects", t => t.Project_ProjectId)
                .Index(t => t.CharacterGroupId)
                .Index(t => t.ProjectId)
                .Index(t => t.CommentDiscussionId)
                .Index(t => t.AuthorUserId)
                .Index(t => t.Project_ProjectId);
            
            CreateTable(
                "dbo.CommentDiscussions",
                c => new
                    {
                        CommentDiscussionId = c.Int(nullable: false, identity: true),
                        ProjectId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.CommentDiscussionId)
                .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: true)
                .Index(t => t.ProjectId);
            
            CreateTable(
                "dbo.UserForumSubscriptions",
                c => new
                    {
                        UserForumSubscriptionId = c.Int(nullable: false, identity: true),
                        ForumThreadId = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.UserForumSubscriptionId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .ForeignKey("dbo.ForumThreads", t => t.ForumThreadId, cascadeDelete: true)
                .Index(t => t.ForumThreadId)
                .Index(t => t.UserId);


      AddColumn("dbo.Claims", "CommentDiscussionId", c => c.Int(nullable: true));
      AddColumn("dbo.Comments", "CommentDiscussionId", c => c.Int(nullable: true));
      AddColumn("dbo.ReadCommentWatermarks", "CommentDiscussionId", c => c.Int(nullable: true));
      Sql(@"
SET IDENTITY_INSERT dbo.CommentDiscussions ON;
GO

INSERT INTO dbo.CommentDiscussions 
(ProjectId, CommentDiscussionId) 
SELECT ProjectId, ClaimId
FROM Claims

UPDATE dbo.Claims
SET CommentDiscussionId = ClaimId

UPDATE dbo.Comments 
SET CommentDiscussionId = ClaimId

UPDATE dbo.ReadCommentWatermarks
SET CommentDiscussionId = ClaimId

GO

SET IDENTITY_INSERT dbo.CommentDiscussions OFF;
GO");
      AlterColumn("dbo.Claims", "CommentDiscussionId", c => c.Int(nullable: false));
      AlterColumn("dbo.Comments", "CommentDiscussionId", c => c.Int(nullable: false));
      AlterColumn("dbo.ReadCommentWatermarks", "CommentDiscussionId", c => c.Int(nullable: false));

      CreateIndex("dbo.Claims", "CommentDiscussionId");
            CreateIndex("dbo.Comments", "CommentDiscussionId");
            CreateIndex("dbo.ReadCommentWatermarks", "CommentDiscussionId");
            AddForeignKey("dbo.Comments", "CommentDiscussionId", "dbo.CommentDiscussions", "CommentDiscussionId", cascadeDelete: false);
            AddForeignKey("dbo.ReadCommentWatermarks", "CommentDiscussionId", "dbo.CommentDiscussions", "CommentDiscussionId", cascadeDelete: false);
            AddForeignKey("dbo.Claims", "CommentDiscussionId", "dbo.CommentDiscussions", "CommentDiscussionId");
            DropColumn("dbo.Comments", "ClaimId");
            DropColumn("dbo.ReadCommentWatermarks", "ClaimId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ReadCommentWatermarks", "ClaimId", c => c.Int(nullable: false));
            AddColumn("dbo.Comments", "ClaimId", c => c.Int(nullable: false));
            DropForeignKey("dbo.Claims", "CommentDiscussionId", "dbo.CommentDiscussions");
            DropForeignKey("dbo.ForumThreads", "Project_ProjectId", "dbo.Projects");
            DropForeignKey("dbo.UserForumSubscriptions", "ForumThreadId", "dbo.ForumThreads");
            DropForeignKey("dbo.UserForumSubscriptions", "UserId", "dbo.Users");
            DropForeignKey("dbo.ForumThreads", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.ForumThreads", "CommentDiscussionId", "dbo.CommentDiscussions");
            DropForeignKey("dbo.ReadCommentWatermarks", "CommentDiscussionId", "dbo.CommentDiscussions");
            DropForeignKey("dbo.CommentDiscussions", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.Comments", "CommentDiscussionId", "dbo.CommentDiscussions");
            DropForeignKey("dbo.ForumThreads", "CharacterGroupId", "dbo.CharacterGroups");
            DropForeignKey("dbo.ForumThreads", "AuthorUserId", "dbo.Users");
            DropIndex("dbo.UserForumSubscriptions", new[] { "UserId" });
            DropIndex("dbo.UserForumSubscriptions", new[] { "ForumThreadId" });
            DropIndex("dbo.ReadCommentWatermarks", new[] { "CommentDiscussionId" });
            DropIndex("dbo.Comments", new[] { "CommentDiscussionId" });
            DropIndex("dbo.CommentDiscussions", new[] { "ProjectId" });
            DropIndex("dbo.ForumThreads", new[] { "Project_ProjectId" });
            DropIndex("dbo.ForumThreads", new[] { "AuthorUserId" });
            DropIndex("dbo.ForumThreads", new[] { "CommentDiscussionId" });
            DropIndex("dbo.ForumThreads", new[] { "ProjectId" });
            DropIndex("dbo.ForumThreads", new[] { "CharacterGroupId" });
            DropIndex("dbo.Claims", new[] { "CommentDiscussionId" });
            DropColumn("dbo.ReadCommentWatermarks", "CommentDiscussionId");
            DropColumn("dbo.Comments", "CommentDiscussionId");
            DropColumn("dbo.Claims", "CommentDiscussionId");
            DropTable("dbo.UserForumSubscriptions");
            DropTable("dbo.CommentDiscussions");
            DropTable("dbo.ForumThreads");
            CreateIndex("dbo.ReadCommentWatermarks", "ClaimId");
            CreateIndex("dbo.Comments", "ClaimId");
            AddForeignKey("dbo.ReadCommentWatermarks", "ClaimId", "dbo.Claims", "ClaimId", cascadeDelete: true);
            AddForeignKey("dbo.Comments", "ClaimId", "dbo.Claims", "ClaimId", cascadeDelete: true);
        }
    }
}
