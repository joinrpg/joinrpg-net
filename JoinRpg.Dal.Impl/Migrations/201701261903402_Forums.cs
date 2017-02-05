namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Forums : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Comments", "ClaimId", "dbo.Claims");
            DropForeignKey("dbo.ReadCommentWatermarks", "ClaimId", "dbo.Claims");
            DropForeignKey("dbo.UserSubscriptions", "ClaimId", "dbo.Claims");
            DropForeignKey("dbo.FinanceOperations", "ClaimId", "dbo.Claims");
            DropIndex("dbo.Comments", new[] { "ClaimId" });
            DropIndex("dbo.ReadCommentWatermarks", new[] { "ClaimId" });
            DropPrimaryKey("dbo.Claims");
            CreateTable(
                "dbo.ForumThreads",
                c => new
                    {
                        ForumThreadId = c.Int(nullable: false),
                        CharacterGroupId = c.Int(nullable: false),
                        ProjectId = c.Int(nullable: false),
                        Header = c.String(),
                        CreatedAt = c.DateTime(nullable: false),
                        ModifiedAt = c.DateTime(nullable: false),
                        AuthorUserId = c.Int(nullable: false),
                        IsVisibleToPlayer = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ForumThreadId)
                .ForeignKey("dbo.Users", t => t.AuthorUserId, cascadeDelete: true)
                .ForeignKey("dbo.CharacterGroups", t => t.CharacterGroupId, cascadeDelete: true)
                .ForeignKey("dbo.CommentDiscussions", t => t.ForumThreadId)
                .ForeignKey("dbo.Projects", t => t.ProjectId)
                .Index(t => t.ForumThreadId)
                .Index(t => t.CharacterGroupId)
                .Index(t => t.ProjectId)
                .Index(t => t.AuthorUserId);
            
            CreateTable(
                "dbo.CommentDiscussions",
                c => new
                    {
                        CommentDiscussionId = c.Int(nullable: false, identity: true),
                        ProjectId = c.Int(nullable: false),
                        ClaimId = c.Int(),
                        ForumThreadId = c.Int(),
                    })
                .PrimaryKey(t => t.CommentDiscussionId)
                .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: true)
                .Index(t => t.ProjectId)
                .Index(t => t.ClaimId, unique: true, name: "IX_Unique_ClaimId");
            
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
            
            AddColumn("dbo.Comments", "CommentDiscussionId", c => c.Int(nullable: true));
            AddColumn("dbo.ReadCommentWatermarks", "CommentDiscussionId", c => c.Int(nullable: true));
      Sql(@"INSERT INTO dbo.CommentDiscussions 
(ProjectId, ClaimId) 
SELECT ProjectId, ClaimId 
FROM Claims

UPDATE dbo.Comments 
SET CommentDiscussionId = CD.CommentDiscussionId
FROM dbo.Comments C
INNER JOIN dbo.CommentDiscussions CD ON CD.ClaimId = C.ClaimId

UPDATE dbo.ReadCommentWatermarks
SET CommentDiscussionId = CD.CommentDiscussionId
FROM dbo.Comments C
INNER JOIN dbo.CommentDiscussions CD ON CD.ClaimId = C.ClaimId");
      AlterColumn("dbo.Comments", "CommentDiscussionId", c => c.Int(nullable: false));
      AlterColumn("dbo.ReadCommentWatermarks", "CommentDiscussionId", c => c.Int(nullable: false));
      AlterColumn("dbo.Claims", "ClaimId", c => c.Int(nullable: false));
            AddPrimaryKey("dbo.Claims", "ClaimId");
            CreateIndex("dbo.Claims", "ClaimId");
            CreateIndex("dbo.Comments", "CommentDiscussionId");
            CreateIndex("dbo.ReadCommentWatermarks", "CommentDiscussionId");
            AddForeignKey("dbo.Comments", "CommentDiscussionId", "dbo.CommentDiscussions", "CommentDiscussionId", cascadeDelete: true);
            AddForeignKey("dbo.ReadCommentWatermarks", "CommentDiscussionId", "dbo.CommentDiscussions", "CommentDiscussionId", cascadeDelete: true);
            AddForeignKey("dbo.Claims", "ClaimId", "dbo.CommentDiscussions", "ClaimId");
            AddForeignKey("dbo.UserSubscriptions", "ClaimId", "dbo.Claims", "ClaimId");
            AddForeignKey("dbo.FinanceOperations", "ClaimId", "dbo.Claims", "ClaimId", cascadeDelete: true);
            DropColumn("dbo.Comments", "ClaimId");
            DropColumn("dbo.ReadCommentWatermarks", "ClaimId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ReadCommentWatermarks", "ClaimId", c => c.Int(nullable: false));
            AddColumn("dbo.Comments", "ClaimId", c => c.Int(nullable: false));
            DropForeignKey("dbo.FinanceOperations", "ClaimId", "dbo.Claims");
            DropForeignKey("dbo.UserSubscriptions", "ClaimId", "dbo.Claims");
            DropForeignKey("dbo.Claims", "ClaimId", "dbo.CommentDiscussions");
            DropForeignKey("dbo.UserForumSubscriptions", "ForumThreadId", "dbo.ForumThreads");
            DropForeignKey("dbo.UserForumSubscriptions", "UserId", "dbo.Users");
            DropForeignKey("dbo.ForumThreads", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.ForumThreads", "ForumThreadId", "dbo.CommentDiscussions");
            DropForeignKey("dbo.ReadCommentWatermarks", "CommentDiscussionId", "dbo.CommentDiscussions");
            DropForeignKey("dbo.CommentDiscussions", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.Comments", "CommentDiscussionId", "dbo.CommentDiscussions");
            DropForeignKey("dbo.ForumThreads", "CharacterGroupId", "dbo.CharacterGroups");
            DropForeignKey("dbo.ForumThreads", "AuthorUserId", "dbo.Users");
            DropIndex("dbo.UserForumSubscriptions", new[] { "UserId" });
            DropIndex("dbo.UserForumSubscriptions", new[] { "ForumThreadId" });
            DropIndex("dbo.ReadCommentWatermarks", new[] { "CommentDiscussionId" });
            DropIndex("dbo.Comments", new[] { "CommentDiscussionId" });
            DropIndex("dbo.CommentDiscussions", "IX_Unique_ClaimId");
            DropIndex("dbo.CommentDiscussions", new[] { "ProjectId" });
            DropIndex("dbo.ForumThreads", new[] { "AuthorUserId" });
            DropIndex("dbo.ForumThreads", new[] { "ProjectId" });
            DropIndex("dbo.ForumThreads", new[] { "CharacterGroupId" });
            DropIndex("dbo.ForumThreads", new[] { "ForumThreadId" });
            DropIndex("dbo.Claims", new[] { "ClaimId" });
            DropPrimaryKey("dbo.Claims");
            AlterColumn("dbo.Claims", "ClaimId", c => c.Int(nullable: false, identity: true));
            DropColumn("dbo.ReadCommentWatermarks", "CommentDiscussionId");
            DropColumn("dbo.Comments", "CommentDiscussionId");
            DropTable("dbo.UserForumSubscriptions");
            DropTable("dbo.CommentDiscussions");
            DropTable("dbo.ForumThreads");
            AddPrimaryKey("dbo.Claims", "ClaimId");
            CreateIndex("dbo.ReadCommentWatermarks", "ClaimId");
            CreateIndex("dbo.Comments", "ClaimId");
            AddForeignKey("dbo.FinanceOperations", "ClaimId", "dbo.Claims", "ClaimId", cascadeDelete: true);
            AddForeignKey("dbo.UserSubscriptions", "ClaimId", "dbo.Claims", "ClaimId");
            AddForeignKey("dbo.ReadCommentWatermarks", "ClaimId", "dbo.Claims", "ClaimId", cascadeDelete: true);
            AddForeignKey("dbo.Comments", "ClaimId", "dbo.Claims", "ClaimId", cascadeDelete: true);
        }
    }
}
