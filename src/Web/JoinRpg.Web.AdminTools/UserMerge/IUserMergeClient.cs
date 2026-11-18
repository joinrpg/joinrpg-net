using JoinRpg.Common.WebComponents;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Projects;

namespace JoinRpg.Web.AdminTools.UserMerge;

/// <summary>
/// Админское слияние двух аккаунтов (ADR024).
/// </summary>
public interface IUserMergeClient
{
    /// <param name="primaryUserLink">Основной аккаунт — остаётся. Ссылка в любом виде, который понимает <c>JoinUserLinkEditor</c>.</param>
    /// <param name="secondaryUserLink">Поглощаемый аккаунт — удаляется.</param>
    Task<UserMergePreviewViewModel> GetPreview(string primaryUserLink, string secondaryUserLink);

    Task Merge(UserIdentification primary, UserIdentification secondary);
}

public record UserMergePreviewViewModel(
    UserMergeAccountViewModel Primary,
    UserMergeAccountViewModel Secondary,
    IReadOnlyCollection<UserMergeConflictViewModel> Conflicts,
    IReadOnlyCollection<string> DroppedLoginProviders,
    int SecondaryClaims,
    int SecondaryComments,
    int SecondaryMasterProjects);

public record UserMergeAccountViewModel(UserLinkViewModel User, string Email);

/// <param name="Project">Проект, в котором конфликт, — если он есть.</param>
/// <param name="Character">Персонаж — для <see cref="UserMergeConflictKind.BothClaimedCharacter"/>.</param>
/// <param name="GroupName">Группа — для <see cref="UserMergeConflictKind.BothCaptains"/>.</param>
public record UserMergeConflictViewModel(
    UserMergeConflictKind Kind,
    ProjectLinkViewModel? Project,
    CharacterLinkSlimViewModel? Character,
    string? GroupName);

public enum UserMergeConflictKind
{
    SameUser,
    VirtualUser,
    BothMasters,
    BothClaimedCharacter,
    ApprovedClaimInSingleCharacterProject,
    BothHaveCashPaymentType,
    MoneyTransfersBetween,
    BothCaptains,
    BlockedProject,
    SecondaryIsCurrentUser,
}
