namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class LastCommentDates : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.Claims", "LastMasterCommentAt", c => c.DateTimeOffset(precision: 7));
        AddColumn("dbo.Claims", "LastMasterCommentBy_Id", c => c.Int());
        AddColumn("dbo.Claims", "LastVisibleMasterCommentAt", c => c.DateTimeOffset(precision: 7));
        AddColumn("dbo.Claims", "LastVisibleMasterCommentBy_Id", c => c.Int());
        AddColumn("dbo.Claims", "LastPlayerCommentAt", c => c.DateTimeOffset(precision: 7));
        CreateIndex("dbo.Claims", "LastMasterCommentBy_Id");
        CreateIndex("dbo.Claims", "LastVisibleMasterCommentBy_Id");
        AddForeignKey("dbo.Claims", "LastMasterCommentBy_Id", "dbo.Users", "UserId");
        AddForeignKey("dbo.Claims", "LastVisibleMasterCommentBy_Id", "dbo.Users", "UserId");

        Sql(@"
UPDATE Claims 
SET 
	[LastPlayerCommentAt] = LastCommentTime
FROM (
SELECT 
  Claims.ClaimId,
  ROW_NUMBER() OVER (partition by PlayerComment.CommentDiscussionId order by PlayerComment.LastEditTime desc) AS PC_NUM,
  PlayerComment.AuthorUserId,
  PlayerComment.LastEditTime AS LastCommentTime
  FROM [dbo].[Claims]
  INNER JOIN Comments PlayerComment ON 
  PlayerComment.CommentDiscussionId = Claims.CommentDiscussionId 
  WHERE PlayerComment.AuthorUserId = Claims.PlayerUserId
  ) T
WHERE PC_NUM = 1 AND T.ClaimId = Claims.ClaimId"); // Players comments

        Sql(@"
UPDATE Claims 
SET 
	LastVisibleMasterCommentAt = LastCommentTime,
	LastVisibleMasterCommentBy_Id = AuthorUserId
FROM (
SELECT 
  Claims.ClaimId,
  ROW_NUMBER() OVER (partition by C.CommentDiscussionId order by C.LastEditTime desc) AS PC_NUM,
  C.AuthorUserId,
  C.LastEditTime AS LastCommentTime
  FROM [dbo].[Claims]
  INNER JOIN Comments C ON 
  C.CommentDiscussionId = Claims.CommentDiscussionId 
  WHERE C.AuthorUserId <> Claims.PlayerUserId AND C.IsVisibleToPlayer = 1
  ) T
WHERE PC_NUM = 1 AND T.ClaimId = Claims.ClaimId
");


        Sql(@"
UPDATE Claims 
SET 
	LastMasterCommentAt = LastCommentTime,
	LastMasterCommentBy_Id = AuthorUserId
FROM (
SELECT 
  Claims.ClaimId,
  ROW_NUMBER() OVER (partition by C.CommentDiscussionId order by C.LastEditTime desc) AS PC_NUM,
  C.AuthorUserId,
  C.LastEditTime AS LastCommentTime
  FROM [dbo].[Claims]
  INNER JOIN Comments C ON 
  C.CommentDiscussionId = Claims.CommentDiscussionId 
  WHERE C.AuthorUserId <> Claims.PlayerUserId 
  ) T
WHERE PC_NUM = 1 AND T.ClaimId = Claims.ClaimId
");
    }

    public override void Down()
    {
        DropForeignKey("dbo.Claims", "LastVisibleMasterCommentBy_Id", "dbo.Users");
        DropForeignKey("dbo.Claims", "LastMasterCommentBy_Id", "dbo.Users");
        DropIndex("dbo.Claims", new[] { "LastVisibleMasterCommentBy_Id" });
        DropIndex("dbo.Claims", new[] { "LastMasterCommentBy_Id" });
        DropColumn("dbo.Claims", "LastPlayerCommentAt");
        DropColumn("dbo.Claims", "LastVisibleMasterCommentBy_Id");
        DropColumn("dbo.Claims", "LastVisibleMasterCommentAt");
        DropColumn("dbo.Claims", "LastMasterCommentBy_Id");
        DropColumn("dbo.Claims", "LastMasterCommentAt");
    }
}
