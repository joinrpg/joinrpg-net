using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.Domain.Problems;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Claims;
using JoinRpg.Web.Models.Claims;
using JoinRpg.Web.Models.UserProfile;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.Models.ClaimList;
public static class ClaimListBuilder
{
    internal static ClaimListItemViewModel BuildItem(Claim claim, ICurrentUserAccessor currentUserId, ProjectInfo projectInfo,
       IProblemValidator<Claim> claimValidator, Dictionary<int, int> unreadComments)
    {
        var accessArguments = AccessArgumentsFactory.Create(claim, currentUserId);
        (DateTime lastModifiedAt, User lastModifiedBy) = GetLastComment(claim, accessArguments);

        return new ClaimListItemViewModel(
            claim.Character.CharacterName,
            UserLinks.Create(claim.Player, ViewMode.Show),
            projectInfo.ProjectName,
            ClaimStatusBuilders.CreateFullStatus(claim, accessArguments),
            lastModifiedAt,
            claim.CreateDate,
            claim.CheckInDate,
            UserLinks.Create(claim.ResponsibleMasterUser, ViewMode.Show),
            FeePaid: claim.ClaimBalance(),
            FeeDue: claim.ClaimFeeDue(projectInfo),
            TotalFee: claim.ClaimTotalFee(projectInfo),
            UserLinks.Create(lastModifiedBy, ViewMode.Show),
            claim.GetId(),
            claimValidator.Validate(claim, projectInfo).Select(p => new ProblemViewModel(p)).ToList(),
            unreadComments.GetValueOrDefault(claim.CommentDiscussionId),
            claim.Player.FullName
            );
    }

    public static ClaimListItemForCaptainViewModel BuildItemForCaptain(Claim claim, ICurrentUserAccessor currentUserId, ProjectInfo projectInfo)
    {
        var accessArguments = AccessArgumentsFactory.Create(claim, currentUserId);
        (DateTime lastModifiedAt, _) = GetLastComment(claim, accessArguments);

        if (accessArguments.CanViewCharacterName)
        {
            return new ClaimListItemForCaptainViewModel(
                claim.Character.CharacterName,
                UserLinks.Create(claim.Player, ViewMode.Show),
                (ClaimStatusView)claim.ClaimStatus,
                lastModifiedAt,
                claim.CreateDate,
                claim.CheckInDate,
                UserLinks.Create(claim.ResponsibleMasterUser, ViewMode.Show),
                FeePaid: claim.ClaimBalance(),
                FeeDue: claim.ClaimFeeDue(projectInfo),
                TotalFee: claim.ClaimTotalFee(projectInfo),
                claim.GetId(),
                claim.Player.FullName
                );
        }
        else
        {
            return null;
        }
    }

    internal static ClaimListItemForExportViewModel BuildItemForExport(Claim claim, ICurrentUserAccessor currentUserId, ProjectInfo projectInfo)
    {
        var accessArguments = AccessArgumentsFactory.Create(claim, currentUserId);
        (DateTime lastModifiedAt, User lastModifiedBy) = GetLastComment(claim, accessArguments);

        string? PassportData, RegistrationAddress;
        if (claim.PlayerAllowedSenstiveData && projectInfo.ProfileRequirementSettings.SensitiveDataRequired)
        {
            PassportData = claim.Player.Extra.PassportData;
            RegistrationAddress = claim.Player.Extra.RegistrationAddress;
        }
        else
        {
            PassportData = RegistrationAddress = null;
        }

        return new ClaimListItemForExportViewModel(
            claim.Character.CharacterName,
            UserLinks.Create(claim.Player, ViewMode.Show),
            projectInfo.ProjectName,
            ClaimStatusBuilders.CreateFullStatus(claim, accessArguments),
            lastModifiedAt,
            claim.CreateDate,
            claim.CheckInDate,
            UserLinks.Create(claim.ResponsibleMasterUser, ViewMode.Show),
            FeePaid: claim.ClaimBalance(),
            FeeDue: claim.ClaimFeeDue(projectInfo),
            TotalFee: claim.ClaimTotalFee(projectInfo),
            UserLinks.Create(lastModifiedBy, ViewMode.Show),
            claim.GetId(),
            claim.AccommodationRequest?.AccommodationType.Name,
            claim.AccommodationRequest?.Accommodation?.Name,
            claim.PreferentialFeeUser,
            PassportData,
            RegistrationAddress,
            claim.GetFields(projectInfo).ToDictionary(x => x.Field.Id, x => x.DisplayString),
            claim.Player
            );
    }

    private static (DateTime At, User By) GetLastComment(Claim claim, AccessArguments accessArguments)
    {
        var lastComment = (At: claim.CreateDate, By: claim.Player);

        if (claim.LastPlayerCommentAt is not null && claim.LastPlayerCommentAt > lastComment.At)
        {
            lastComment = (At: claim.LastPlayerCommentAt.Value.DateTime, By: claim.Player);
        }

        if (claim.LastVisibleMasterCommentAt is not null && claim.LastVisibleMasterCommentAt > lastComment.At)
        {
            lastComment = (At: claim.LastVisibleMasterCommentAt.Value.DateTime, By: claim.LastVisibleMasterCommentBy!);
        }

        if (accessArguments.MasterAccess && claim.LastMasterCommentAt is not null && claim.LastMasterCommentAt > lastComment.At)
        {
            lastComment = (At: claim.LastMasterCommentAt.Value.DateTime, By: claim.LastMasterCommentBy!);
        }

        return lastComment;
    }
}
