using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.AdminTools.UserMerge;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Projects;
using JoinRpg.WebPortal.Managers.Claims;

namespace JoinRpg.WebPortal.Managers.AdminTools;

internal class UserMergeViewService(
    IUserMergeService userMergeService,
    IUserLinkResolver userLinkResolver,
    IUserRepository userRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterInfoRepository characterInfoRepository) : IUserMergeClient
{
    public async Task<UserMergePreviewViewModel> GetPreview(string primaryUserLink, string secondaryUserLink)
    {
        var primary = await userLinkResolver.ResolveAsync(primaryUserLink);
        var secondary = await userLinkResolver.ResolveAsync(secondaryUserLink);

        var preview = await userMergeService.GetPreview(primary, secondary);

        var users = (await userRepository.GetRequiredUserInfos([.. new[] { primary, secondary }.Distinct()]))
            .ToDictionary(u => u.UserId);
        var conflicts = new List<UserMergeConflictViewModel>();
        foreach (var conflict in preview.Conflicts)
        {
            conflicts.Add(await ToViewModel(conflict));
        }

        return new UserMergePreviewViewModel(
            ToAccount(users[primary]),
            ToAccount(users[secondary]),
            conflicts,
            preview.DroppedLoginProviders,
            preview.SecondaryStats.Claims,
            preview.SecondaryStats.Comments,
            preview.SecondaryStats.MasterProjects);
    }

    public Task Merge(UserIdentification primary, UserIdentification secondary)
        => userMergeService.Merge(primary, secondary);

    private static UserMergeAccountViewModel ToAccount(UserInfo user)
        => new(new UserLinkViewModel(user.UserId, user.DisplayName.DisplayName, ViewMode.Show), user.Email.Value);

    private async Task<UserMergeConflictViewModel> ToViewModel(UserMergeConflict conflict) => conflict switch
    {
        UserMergeConflict.SameUser => new(UserMergeConflictKind.SameUser, null, null, null),
        UserMergeConflict.VirtualUser => new(UserMergeConflictKind.VirtualUser, null, null, null),
        UserMergeConflict.BothMasters c => new(UserMergeConflictKind.BothMasters, await Project(c.ProjectId), null, null),
        UserMergeConflict.BothClaimedCharacter c => new(
            UserMergeConflictKind.BothClaimedCharacter, await Project(c.CharacterId.ProjectId), await Character(c.CharacterId), null),
        UserMergeConflict.ApprovedClaimInSingleCharacterProject c => new(
            UserMergeConflictKind.ApprovedClaimInSingleCharacterProject, await Project(c.ProjectId), null, null),
        UserMergeConflict.BothHaveCashPaymentType c => new(UserMergeConflictKind.BothHaveCashPaymentType, await Project(c.ProjectId), null, null),
        UserMergeConflict.MoneyTransfersBetween c => new(UserMergeConflictKind.MoneyTransfersBetween, await Project(c.ProjectId), null, null),
        UserMergeConflict.BlockedProject c => new(UserMergeConflictKind.BlockedProject, await Project(c.ProjectId), null, null),
        UserMergeConflict.BothCaptains c => new(
            UserMergeConflictKind.BothCaptains,
            await Project(c.CharacterGroupId.ProjectId),
            null,
            (await projectMetadataRepository.GetProjectMetadata(c.CharacterGroupId.ProjectId)).GroupTree
                .GetGroupByIdOrDefault(c.CharacterGroupId)?.Name ?? $"№{c.CharacterGroupId.CharacterGroupId}"),
        _ => throw new ArgumentOutOfRangeException(nameof(conflict), conflict, null),
    };

    private async Task<ProjectLinkViewModel> Project(ProjectIdentification projectId)
        => new(projectId, (await projectMetadataRepository.GetProjectMetadata(projectId)).ProjectName);

    private async Task<CharacterLinkSlimViewModel?> Character(CharacterIdentification characterId)
        => await characterInfoRepository.GetCharacterInfoOrDefault(characterId) is { } character
            ? new CharacterLinkSlimViewModel(character.Id, character.CharacterName, character.IsActive, ViewMode.Show)
            : null;
}
