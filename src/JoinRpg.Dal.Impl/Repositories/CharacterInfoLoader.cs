using JoinRpg.DomainTypes.Characters;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

/// <summary>
/// Чистое ядро загрузки <see cref="CharacterInfo"/> (ADR013): один запрос с явной проекцией
/// в row-типы плюс <see cref="CharacterInfoMapper"/>.
/// </summary>
/// <remarks>
/// <para>
/// Сознательно НЕ обращается к <c>IProjectMetadataRepository</c>: готовый <see cref="ProjectInfo"/>
/// приходит снаружи. Это нужно пути записи, где <see cref="ProjectInfo"/> собирается свой
/// (<c>ProjectMetadataRepository.CreateInfoFromProject</c>) и должен быть ровно тем же экземпляром,
/// что и внутри <see cref="CharacterInfo"/> — иначе сработает проверка <c>ReferenceEquals</c>
/// в конструкторе агрегата.
/// </para>
/// <para>
/// Сознательно НЕ наследуется от <c>GameRepositoryImplBase</c>: тот прогревает контекст всем
/// проектом целиком (<c>LoadProjectFields</c>, <c>LoadProjectClaimsAndComments</c>,
/// <c>LoadMasters</c>) — главная причина медленной сетки ролей из ADR011. Здесь каждая загрузка
/// делает ровно один запрос с явной проекцией, без <c>Include</c> и без lazy load.
/// </para>
/// </remarks>
internal sealed class CharacterInfoLoader(MyDbContext ctx)
{
    /// <summary>
    /// Персонажи проекта <paramref name="projectInfo"/>, удовлетворяющие <paramref name="predicate"/>.
    /// Все они разделяют переданный экземпляр <see cref="ProjectInfo"/>.
    /// </summary>
    public async Task<IReadOnlyCollection<CharacterInfo>> LoadAsync(
        ProjectInfo projectInfo,
        Expression<Func<Character, bool>> predicate)
    {
        var projectId = projectInfo.ProjectId;

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
