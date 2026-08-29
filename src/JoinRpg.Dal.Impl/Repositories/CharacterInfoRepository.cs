using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Interfaces;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

/// <summary>
/// Загрузчик <see cref="CharacterInfo"/> (ADR013).
/// </summary>
/// <remarks>
/// Сознательно НЕ наследуется от <c>GameRepositoryImplBase</c>: тот прогревает контекст всем
/// проектом целиком (<c>LoadProjectFields</c>, <c>LoadProjectClaimsAndComments</c>,
/// <c>LoadMasters</c>) — главная причина медленной сетки ролей из ADR011. Здесь каждый метод
/// делает ровно один запрос с явной проекцией, без <c>Include</c> и без lazy load.
/// </remarks>
internal class CharacterInfoRepository(MyDbContext ctx, IProjectMetadataRepository projectMetadataRepository)
    : ICharacterInfoRepository
{
    public async Task<CharacterInfo?> GetCharacterInfoOrDefault(CharacterIdentification characterId)
    {
        var characterIntId = characterId.CharacterId;
        var result = await GetCoreAsync(characterId.ProjectId, character => character.CharacterId == characterIntId);
        return result.SingleOrDefault();
    }

    public async Task<IReadOnlyCollection<CharacterInfo>> GetCharacterInfos(
        IReadOnlyCollection<CharacterIdentification> characterIds)
    {
        if (characterIds.Count == 0)
        {
            return [];
        }

        var projectId = characterIds.EnsureSameProject().First().ProjectId;
        var characterIntIds = characterIds.Select(c => c.CharacterId).ToArray();

        return await GetCoreAsync(projectId, character => characterIntIds.Contains(character.CharacterId));
    }

    public async Task<IReadOnlyCollection<CharacterInfo>> GetCharacterInfosByGroups(
        ProjectIdentification projectId,
        IReadOnlyCollection<CharacterGroupIdentification> groupIds)
    {
        if (groupIds.Count == 0)
        {
            return [];
        }

        return await GetCoreAsync(projectId, CharacterPredicates.ByGroup(groupIds));
    }

    public async Task<IReadOnlyCollection<CharacterInfo>> GetAllCharacterInfos(ProjectIdentification projectId)
        => await GetCoreAsync(projectId, character => true);

    private async Task<IReadOnlyCollection<CharacterInfo>> GetCoreAsync(
        ProjectIdentification projectId,
        Expression<Func<Character, bool>> predicate)
    {
        // Метаданные проекта берутся из своего репозитория (за ним в Portal стоит кеш запроса).
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var query =
            from character in ctx.Set<Character>().AsNoTracking().AsExpandable()
            where character.ProjectId == projectId.Value
            where predicate.Invoke(character)
            select new CharacterInfoRow
            {
                CharacterId = character.CharacterId,
                CharacterName = character.CharacterName,
                CharacterType = character.CharacterType,
                IsHot = character.IsHot,
                CharacterSlotLimit = character.CharacterSlotLimit,
                IsPublic = character.IsPublic,
                HidePlayerForCharacter = character.HidePlayerForCharacter,
                IsActive = character.IsActive,
                InGame = character.InGame,
                AutoCreated = character.AutoCreated,
                JsonData = character.JsonData,
                Description = character.Description,
                ParentGroups = character.ParentGroupsImpl,
                ApprovedClaimId = character.ApprovedClaimId,
                // Каст к int? превращает обращение к nullable-навигации в LEFT JOIN.
                OriginalCharacterSlotId = (int?)character.OriginalCharacterSlot!.CharacterId,
                CreatedAt = character.CreatedAt,
                CreatedById = character.CreatedById,
                UpdatedAt = character.UpdatedAt,
                UpdatedById = character.UpdatedById,
                Claims = character.Claims.Select(claim => new CharacterInfoClaimRow
                {
                    ClaimId = claim.ClaimId,
                    PlayerUserId = claim.PlayerUserId,
                    // Один join к User на всю выборку, без N+1: только то, из чего складывается
                    // отображаемое имя. Контакты и соцсети сюда не тянем (ADR011).
                    PlayerPrefferedName = claim.Player.PrefferedName,
                    PlayerBornName = claim.Player.BornName,
                    PlayerSurName = claim.Player.SurName,
                    PlayerFatherName = claim.Player.FatherName,
                    PlayerEmail = claim.Player.Email,
                    ClaimStatus = claim.ClaimStatus,
                    ClaimDenialStatus = claim.ClaimDenialStatus,
                    ResponsibleMasterUserId = claim.ResponsibleMasterUserId,
                    CreateDate = claim.CreateDate,
                    LastUpdateDateTime = claim.LastUpdateDateTime,
                    CheckInDate = claim.CheckInDate,
                    LastPlayerCommentAt = claim.LastPlayerCommentAt,
                    LastMasterCommentAt = claim.LastMasterCommentAt,
                    LastVisibleMasterCommentAt = claim.LastVisibleMasterCommentAt,
                    CurrentFee = claim.CurrentFee,
                    PreferentialFeeUser = claim.PreferentialFeeUser,
                    JsonData = claim.JsonData,
                    // Каст обязателен: без него EF6 падает на заявке без финансовых операций.
                    FeePaid = (int?)claim.FinanceOperations
                        .Where(fo => fo.State == FinanceOperationState.Approved)
                        .Sum(fo => fo.MoneyAmount),
                    AccommodationFee = (int?)claim.AccommodationRequest!.AccommodationType.Cost,
                }),
            };

        var rows = await query.ToListAsync();

        return [.. rows.Select(row => CharacterInfoMapper.Map(row, projectInfo))];
    }
}
