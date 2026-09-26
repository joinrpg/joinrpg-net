namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class ModelCascadesToSchema : DbMigration
{
    public override void Up()
    {
        DropForeignKey("dbo.Characters", "CreatedById", "dbo.Users");
        DropForeignKey("dbo.Characters", "UpdatedById", "dbo.Users");
        DropForeignKey("dbo.AccommodationRequests", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.ProjectAccommodations", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.ForumThreads", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.GameReport2DTemplate", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.CharacterGroups", "CreatedById", "dbo.Users");
        DropForeignKey("dbo.CharacterGroups", "UpdatedById", "dbo.Users");
        DropForeignKey("dbo.Comments", "CommentDiscussionId", "dbo.CommentDiscussions");
        DropForeignKey("dbo.ReadCommentWatermarks", "CommentDiscussionId", "dbo.CommentDiscussions");
        DropForeignKey("dbo.ReadCommentWatermarks", "CommentId", "dbo.Comments");
        DropForeignKey("dbo.ReadCommentWatermarks", "UserId", "dbo.Users");
        DropForeignKey("dbo.ProjectFieldDropdownValues", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.GameReport2DTemplate", "CreatedById", "dbo.Users");
        DropForeignKey("dbo.GameReport2DTemplate", "FirstCharacterGroupId", "dbo.CharacterGroups");
        DropForeignKey("dbo.GameReport2DTemplate", "SecondCharacterGroupId", "dbo.CharacterGroups");
        DropForeignKey("dbo.GameReport2DTemplate", "UpdatedById", "dbo.Users");
        DropForeignKey("dbo.AccommodationInvites", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.AccommodationInvites", "ToClaimId", "dbo.Claims");
        AddForeignKey("dbo.Characters", "CreatedById", "dbo.Users", "UserId");
        AddForeignKey("dbo.Characters", "UpdatedById", "dbo.Users", "UserId");
        AddForeignKey("dbo.AccommodationRequests", "ProjectId", "dbo.Projects", "ProjectId");
        AddForeignKey("dbo.ProjectAccommodations", "ProjectId", "dbo.Projects", "ProjectId");
        AddForeignKey("dbo.ForumThreads", "ProjectId", "dbo.Projects", "ProjectId");
        AddForeignKey("dbo.GameReport2DTemplate", "ProjectId", "dbo.Projects", "ProjectId");
        AddForeignKey("dbo.CharacterGroups", "CreatedById", "dbo.Users", "UserId");
        AddForeignKey("dbo.CharacterGroups", "UpdatedById", "dbo.Users", "UserId");
        AddForeignKey("dbo.Comments", "CommentDiscussionId", "dbo.CommentDiscussions", "CommentDiscussionId");
        AddForeignKey("dbo.ReadCommentWatermarks", "CommentDiscussionId", "dbo.CommentDiscussions", "CommentDiscussionId");
        AddForeignKey("dbo.ReadCommentWatermarks", "CommentId", "dbo.Comments", "CommentId");
        AddForeignKey("dbo.ReadCommentWatermarks", "UserId", "dbo.Users", "UserId");
        AddForeignKey("dbo.ProjectFieldDropdownValues", "ProjectId", "dbo.Projects", "ProjectId");
        AddForeignKey("dbo.GameReport2DTemplate", "CreatedById", "dbo.Users", "UserId");
        AddForeignKey("dbo.GameReport2DTemplate", "FirstCharacterGroupId", "dbo.CharacterGroups", "CharacterGroupId");
        AddForeignKey("dbo.GameReport2DTemplate", "SecondCharacterGroupId", "dbo.CharacterGroups", "CharacterGroupId");
        AddForeignKey("dbo.GameReport2DTemplate", "UpdatedById", "dbo.Users", "UserId");
        AddForeignKey("dbo.AccommodationInvites", "ProjectId", "dbo.Projects", "ProjectId");
        AddForeignKey("dbo.AccommodationInvites", "ToClaimId", "dbo.Claims", "ClaimId");
    }

    public override void Down()
    {
        DropForeignKey("dbo.AccommodationInvites", "ToClaimId", "dbo.Claims");
        DropForeignKey("dbo.AccommodationInvites", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.GameReport2DTemplate", "UpdatedById", "dbo.Users");
        DropForeignKey("dbo.GameReport2DTemplate", "SecondCharacterGroupId", "dbo.CharacterGroups");
        DropForeignKey("dbo.GameReport2DTemplate", "FirstCharacterGroupId", "dbo.CharacterGroups");
        DropForeignKey("dbo.GameReport2DTemplate", "CreatedById", "dbo.Users");
        DropForeignKey("dbo.ProjectFieldDropdownValues", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.ReadCommentWatermarks", "UserId", "dbo.Users");
        DropForeignKey("dbo.ReadCommentWatermarks", "CommentId", "dbo.Comments");
        DropForeignKey("dbo.ReadCommentWatermarks", "CommentDiscussionId", "dbo.CommentDiscussions");
        DropForeignKey("dbo.Comments", "CommentDiscussionId", "dbo.CommentDiscussions");
        DropForeignKey("dbo.CharacterGroups", "UpdatedById", "dbo.Users");
        DropForeignKey("dbo.CharacterGroups", "CreatedById", "dbo.Users");
        DropForeignKey("dbo.GameReport2DTemplate", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.ForumThreads", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.ProjectAccommodations", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.AccommodationRequests", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.Characters", "UpdatedById", "dbo.Users");
        DropForeignKey("dbo.Characters", "CreatedById", "dbo.Users");
        AddForeignKey("dbo.AccommodationInvites", "ToClaimId", "dbo.Claims", "ClaimId", cascadeDelete: true);
        AddForeignKey("dbo.AccommodationInvites", "ProjectId", "dbo.Projects", "ProjectId", cascadeDelete: true);
        AddForeignKey("dbo.GameReport2DTemplate", "UpdatedById", "dbo.Users", "UserId", cascadeDelete: true);
        AddForeignKey("dbo.GameReport2DTemplate", "SecondCharacterGroupId", "dbo.CharacterGroups", "CharacterGroupId", cascadeDelete: true);
        AddForeignKey("dbo.GameReport2DTemplate", "FirstCharacterGroupId", "dbo.CharacterGroups", "CharacterGroupId", cascadeDelete: true);
        AddForeignKey("dbo.GameReport2DTemplate", "CreatedById", "dbo.Users", "UserId", cascadeDelete: true);
        AddForeignKey("dbo.ProjectFieldDropdownValues", "ProjectId", "dbo.Projects", "ProjectId", cascadeDelete: true);
        AddForeignKey("dbo.ReadCommentWatermarks", "UserId", "dbo.Users", "UserId", cascadeDelete: true);
        AddForeignKey("dbo.ReadCommentWatermarks", "CommentId", "dbo.Comments", "CommentId", cascadeDelete: true);
        AddForeignKey("dbo.ReadCommentWatermarks", "CommentDiscussionId", "dbo.CommentDiscussions", "CommentDiscussionId", cascadeDelete: true);
        AddForeignKey("dbo.Comments", "CommentDiscussionId", "dbo.CommentDiscussions", "CommentDiscussionId", cascadeDelete: true);
        AddForeignKey("dbo.CharacterGroups", "UpdatedById", "dbo.Users", "UserId", cascadeDelete: true);
        AddForeignKey("dbo.CharacterGroups", "CreatedById", "dbo.Users", "UserId", cascadeDelete: true);
        AddForeignKey("dbo.GameReport2DTemplate", "ProjectId", "dbo.Projects", "ProjectId", cascadeDelete: true);
        AddForeignKey("dbo.ForumThreads", "ProjectId", "dbo.Projects", "ProjectId", cascadeDelete: true);
        AddForeignKey("dbo.ProjectAccommodations", "ProjectId", "dbo.Projects", "ProjectId", cascadeDelete: true);
        AddForeignKey("dbo.AccommodationRequests", "ProjectId", "dbo.Projects", "ProjectId", cascadeDelete: true);
        AddForeignKey("dbo.Characters", "UpdatedById", "dbo.Users", "UserId", cascadeDelete: true);
        AddForeignKey("dbo.Characters", "CreatedById", "dbo.Users", "UserId", cascadeDelete: true);
    }
}
