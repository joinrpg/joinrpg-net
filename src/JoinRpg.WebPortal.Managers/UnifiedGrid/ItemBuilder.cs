using JoinRpg.Common.WebComponents;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.Interfaces;
using JoinRpg.Web.Claims;
using JoinRpg.Web.Claims.UnifiedGrid;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.Web.Models.Claims;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Managers.UnifiedGrid;

public static class ItemBuilder
{
    /// <param name="claims">
    /// Заявки, которые надо показать: агрегат несёт их все, а грид показывает отобранные фильтром.
    /// </param>
    /// <param name="scope">
    /// В капитанском скоупе текущий пользователь — капитан этих персонажей (это проверил сервис),
    /// в мастерском права дают обычные правила доступа.
    /// </param>
    /// <param name="problems">
    /// Проблемы заявок, если их надо показать (мастерский скоуп), иначе <c>null</c>.
    /// </param>
    /// <param name="unreadComments">
    /// Непрочитанные комментарии по обсуждениям заявок (мастерский скоуп, ADR027 §4), иначе <c>null</c>.
    /// </param>
    public static UgItemViewModel? BuildItem(
        CharacterInfo character,
        IReadOnlyCollection<CharacterClaimInfo> claims,
        ICurrentUserAccessor currentUserId,
        UgScopeView scope,
        IReadOnlyDictionary<ClaimIdentification, IReadOnlyCollection<ClaimProblem>>? problems = null,
        IReadOnlyDictionary<CommentDiscussionId, int>? unreadComments = null)
    {
        var accessArguments = AccessArgumentsFactory.Create(character, currentUserId);
        if (scope == UgScopeView.Captain)
        {
            accessArguments = accessArguments with { IsCapitan = true };
        }

        if (!accessArguments.CanViewCharacterName)
        {
            return null;
        }

        var characterViewModel = new UgCharacterViewModel(
           new CharacterLinkSlimViewModel(
               character.Id, character.CharacterName, character.IsActive, ViewModeSelector.Create(character.IsPublic, accessArguments.CanViewCharacterName)),
           new CharacterApplyViewModel(
               character.Id,
               character.GetBusyStatus(),
               character.CharacterTypeInfo.SlotLimit,
               character.CharacterTypeInfo.IsHot,
               ClaimValidator.IsAvailableForPlayer(character)),
           BuildGroups(character, accessArguments)
           );

        return new UgItemViewModel(
            characterViewModel,
            [.. claims.Select(claim => BuildClaimItem(new ClaimInCharacter(character, claim), accessArguments, problems?[claim.ClaimId], unreadComments?.GetValueOrDefault(claim.CommentDiscussionId)))]
            );
    }

    /// <summary>
    /// Группы персонажа — те же, что в сетке ролей (<see cref="CharacterInfo.IntrestingGroupsForDisplay"/>:
    /// активные обычные и группы вариантов поля, вверх до корня). Скрытые — только мастеру.
    /// </summary>
    private static IReadOnlyList<CharacterGroupLinkSlimViewModel> BuildGroups(CharacterInfo character, AccessArguments accessArguments)
        => [.. character.IntrestingGroupsForDisplay
            .Where(group => group.IsPublic || accessArguments.MasterAccess)
            .Select(group => new CharacterGroupLinkSlimViewModel(group))];

    private static UgClaimViewModel BuildClaimItem(
        ClaimInCharacter claimInCharacter,
        AccessArguments accessArguments,
        IReadOnlyCollection<ClaimProblem>? problems,
        int? unreadCommentsCount)
    {
        var claim = claimInCharacter.Claim;
        var lastModifiedAt = ClaimListBuilder.GetLastCommentTime(claim, accessArguments);

        var respMaster = claimInCharacter.ProjectInfo.GetMasterById(claim.ResponsibleMasterId).UserInfo;

        return new UgClaimViewModel(
            new UserLinkViewModel(claim.Player),
            ClaimStatusBuilders.CreateFullStatus(claim, accessArguments),
            lastModifiedAt,
            claim.CreateDate,
            claim.CheckInDate,
            new UserLinkViewModel(respMaster),
            claimInCharacter.CalculateBalance(),
            claim.ClaimId,
            claim.Player.DisplayName.FullName,
            problems?.Select(problem => new ProblemViewModel(problem)).ToList(),
            unreadCommentsCount
            );
    }
}
