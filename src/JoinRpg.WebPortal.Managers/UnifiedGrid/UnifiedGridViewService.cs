using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.UnifiedGrid;
using JoinRpg.Interfaces;
using JoinRpg.Web.Claims.UnifiedGrid;

namespace JoinRpg.WebPortal.Managers.UnifiedGrid;

internal class UnifiedGridViewService(
    ICurrentUserAccessor currentUserAccessor,
    ICaptainRulesRepository captainRulesRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterInfoRepository characterInfoRepository) : IUnifiedGridClient
{
    async Task<IReadOnlyCollection<UgItemViewModel>> IUnifiedGridClient.GetGrid(
        ProjectIdentification projectId,
        UgScopeView scope,
        UgFilterView filter)
    {
        var status = ToDomain(filter.Status);

        var characters = await LoadScope(projectId, scope, status.GetCharacterStatusSpec());

        return
        [
            .. characters
                .Where(character => status.Matches(character))
                .Select(character => ItemBuilder.BuildItem(character, status.SelectClaims(character), currentUserAccessor, scope))
                .WhereNotNull()
        ];
    }

    /// <summary>
    /// Персонажи скоупа: мастеру — весь проект, капитану — поддеревья групп из его правил.
    /// Без прав на запрошенный скоуп грид пуст.
    /// </summary>
    private async Task<IReadOnlyCollection<CharacterInfo>> LoadScope(
        ProjectIdentification projectId,
        UgScopeView scope,
        CharacterStatusSpec spec)
    {
        switch (scope)
        {
            case UgScopeView.Master:
                var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
                if (!projectInfo.HasMasterAccess(currentUserAccessor.UserIdentificationOrDefault))
                {
                    return [];
                }
                return await characterInfoRepository.GetAllCharacterInfos(projectId, spec);

            case UgScopeView.Captain:
                var access = await captainRulesRepository.GetCaptainRules(projectId, currentUserAccessor.UserIdentification);
                if (access.Count == 0)
                {
                    return [];
                }
                var groups = (await projectMetadataRepository.GetProjectMetadata(projectId))
                    .GroupTree.GetChildGroupIdsIncludingThis([.. access.Select(x => x.CharacterGroup)]);
                return await characterInfoRepository.GetCharacterInfosByGroups(projectId, [.. groups], spec);

            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
        }
    }

    private static UnifiedGridStatus ToDomain(UgStatusFilterView status)
        => status switch
        {
            UgStatusFilterView.Active => UnifiedGridStatus.Active,
            UgStatusFilterView.Vacant => UnifiedGridStatus.Vacant,
            UgStatusFilterView.Discussion => UnifiedGridStatus.Discussion,
            UgStatusFilterView.Archive => UnifiedGridStatus.Archive,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };
}
