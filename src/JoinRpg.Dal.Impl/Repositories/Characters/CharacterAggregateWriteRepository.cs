using JoinRpg.Dal.Impl.Repositories.Accommodation;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Dal.Impl.Repositories.Characters;

/// <summary>
/// Реализация <see cref="ICharacterAggregateWriteRepository"/> (ADR014).
/// </summary>
/// <remarks>
/// Не регистрируется в DI (см. <c>Registraton</c>): создаётся только из <c>MyDbContext</c>,
/// чтобы трекинг и <c>SaveChanges</c> шли через один и тот же контекст.
/// </remarks>
internal class CharacterAggregateWriteRepository(MyDbContext ctx) : ICharacterAggregateWriteRepository
{
    private readonly CharacterInfoLoader loader = new(ctx);

    public async Task<ICharacterAggregateUpdateHandle> LoadCharacterForUpdate(
        CharacterIdentification characterId)
    {
        var (project, projectInfo) = await LoadProject(characterId.ProjectId);

        var character = await LoadCharacterEntity(characterId);
        var characterInfo = await LoadCharacterInfo(characterId, projectInfo);

        return new CharacterAggregateUpdateHandle(
            ctx, loader, project, projectInfo, character, characterInfo);
    }

    public async Task<IClaimUpdateHandle> LoadClaimForUpdate(ClaimIdentification claimId)
    {
        var (project, projectInfo) = await LoadProject(claimId.ProjectId);

        var claim = await LoadClaimEntity(claimId);

        var characterId = claim.GetCharacterId();
        var character = await LoadCharacterEntity(characterId);
        var characterInfo = await LoadCharacterInfo(characterId, projectInfo);

        var claimInfo = characterInfo.Claims.SingleOrDefault(c => c.ClaimId == claimId)
            ?? throw new JoinRpgEntityNotFoundException(claimId.ClaimId, "claim");

        return new ClaimUpdateHandle(
            ctx, loader, project, projectInfo, character, characterInfo, claim, claimInfo);
    }

    private async Task<(Project, ProjectInfo)> LoadProject(ProjectIdentification projectId)
    {
        var project = await ProjectLoaderCommon.GetProjectWithFieldsAsync(ctx, projectId.Value, skipCache: false)
            ?? throw new JoinRpgEntityNotFoundException(projectId.Value, "project");

        // Единственный источник истины Project -> ProjectInfo, тот же, что у read-пути.
        return (project, ProjectMetadataRepository.CreateInfoFromProject(project, projectId));
    }

    private async Task<Character> LoadCharacterEntity(CharacterIdentification characterId)
        => await CharacterQuery(ctx).SingleOrDefaultAsync(
                c => c.CharacterId == characterId.CharacterId && c.ProjectId == characterId.ProjectId.Value)
            ?? throw new JoinRpgEntityNotFoundException(characterId.CharacterId, "character");

    private async Task<Claim> LoadClaimEntity(ClaimIdentification claimId)
        => await ClaimQuery(ctx).SingleOrDefaultAsync(
                c => c.ClaimId == claimId.ClaimId && c.ProjectId == claimId.ProjectId.Value)
            ?? throw new JoinRpgEntityNotFoundException(claimId.ClaimId, "claim");

    private async Task<CharacterInfo> LoadCharacterInfo(CharacterIdentification characterId, ProjectInfo projectInfo)
    {
        // Грузим через загрузчик с ЯВНЫМ ProjectInfo: конструктор CharacterInfo требует, чтобы это
        // был ровно тот же экземпляр, что у хэндла (ADR013).
        var characterIntId = characterId.CharacterId;
        var infos = await loader.LoadAsync(projectInfo, character => character.CharacterId == characterIntId);
        return infos.SingleOrDefault()
            ?? throw new JoinRpgEntityNotFoundException(characterId.CharacterId, "character");
    }

    /// <summary>
    /// Связи персонажа, нужные бизнес-логике, грузятся явно, а не ленивой загрузкой.
    /// </summary>
    private static IQueryable<Character> CharacterQuery(MyDbContext ctx)
        => ctx.Set<Character>()
            .Include(c => c.Project)
            .Include(c => c.Project.ProjectAcls)
            .Include(c => c.Claims);

    /// <summary>
    /// Связи заявки, нужные бизнес-логике, грузятся явно, а не ленивой загрузкой
    /// (ср. <c>ClaimsRepositoryImpl.GetClaimImpl</c>, который часть из них тянет лениво).
    /// </summary>
    /// <remarks>
    /// <para><c>Player.Claims</c> — иначе <c>OtherPendingClaimsForThisPlayer</c> вернёт пусто
    /// и автоотклонение при <c>StrictlyOneCharacter</c> тихо исчезнет.</para>
    /// <para><c>CommentDiscussion.Comments</c> — иначе не найти родительский комментарий
    /// и сломается финансовая модерация.</para>
    /// </remarks>
    private static IQueryable<Claim> ClaimQuery(MyDbContext ctx)
        => ctx.Set<Claim>()
            .Include(c => c.Project)
            .Include(c => c.Project.ProjectAcls)
            .Include(c => c.Character)
            .Include(c => c.Player)
            .Include(c => c.Player.Claims)
            .Include(c => c.CommentDiscussion.Comments)
            .Include(c => c.AccommodationRequest)
            // Группа проживающих с составом — «ручки» для записи (ADR022 §4): приём приглашения
            // переселяет заявки из состава трекаемой группы принимающего, не загружая каждую
            // отдельно.
            .Include(c => c.AccommodationRequest!.Subjects)
            .Include(c => c.FinanceOperations);

    private class CharacterAggregateUpdateHandle(
        MyDbContext ctx,
        CharacterInfoLoader loader,
        Project project,
        ProjectInfo projectInfo,
        Character character,
        CharacterInfo characterInfo)
        : ICharacterAggregateUpdateHandle
    {
        public Project Project { get; } = project;

        public ProjectInfo ProjectInfo { get; } = projectInfo;

        public Character Character { get; } = character;

        public CharacterInfo CharacterInfo { get; } = characterInfo;

        public void Add(object entity) => _ = ctx.Set(entity.GetType()).Add(entity);

        public void Remove(object entity) => _ = ctx.Set(entity.GetType()).Remove(entity);

        public async Task<Claim> LoadOtherClaim(ClaimIdentification claimId)
            => await ClaimQuery(ctx).SingleOrDefaultAsync(
                    c => c.ClaimId == claimId.ClaimId && c.ProjectId == claimId.ProjectId.Value)
                ?? throw new JoinRpgEntityNotFoundException(claimId.ClaimId, "claim");

        public async Task<(Character Entity, CharacterInfo Info)> LoadOtherCharacter(CharacterIdentification characterId)
        {
            var entity = await CharacterQuery(ctx).SingleOrDefaultAsync(
                    c => c.CharacterId == characterId.CharacterId && c.ProjectId == characterId.ProjectId.Value)
                ?? throw new JoinRpgEntityNotFoundException(characterId.CharacterId, "character");

            var characterIntId = characterId.CharacterId;
            var infos = await loader.LoadAsync(ProjectInfo, c => c.CharacterId == characterIntId);
            var info = infos.SingleOrDefault()
                ?? throw new JoinRpgEntityNotFoundException(characterId.CharacterId, "character");

            return (entity, info);
        }

        /// <summary>
        /// Повторяет запрос <c>PlotRepositoryImpl.GetDirectPlotsForCharacter</c> один в один, но
        /// на <c>DbContext</c> этого хэндла — иначе добавление персонажа в
        /// <c>PlotElement.TargetCharacters</c> трекалось бы в чужом контексте и не сохранилось.
        /// </summary>
        public async Task<IReadOnlyCollection<PlotElement>> LoadDirectPlotsForCharacter(CharacterIdentification characterId)
        {
            var characterIntId = characterId.CharacterId;
            return await ctx.Set<PlotElement>()
                .Include(e => e.Texts)
                .Include(e => e.TargetCharacters)
                .Include(e => e.TargetGroups)
                .Where(e => e.TargetCharacters.Any(ch => ch.CharacterId == characterIntId))
                .ToListAsync();
        }

        /// <summary>
        /// Повторяет запрос, который снятие приглашений делало на собственном <c>DbContext</c>, но
        /// на контексте этого хэндла. Заявки-участницы (<c>From</c>/<c>To</c>) грузятся сразу:
        /// вызывающие читают их, чтобы понять, чьи приглашения затронуты.
        /// </summary>
        public async Task<IReadOnlyCollection<AccommodationInvite>> LoadInvitesForClaim(ClaimIdentification claimId)
        {
            var claimIntId = claimId.ClaimId;
            var projectIntId = claimId.ProjectId.Value;
            return await ctx.Set<AccommodationInvite>()
                .Include(i => i.From)
                .Include(i => i.To)
                .Where(i => i.ProjectId == projectIntId)
                .Where(i => i.ToClaimId == claimIntId || i.FromClaimId == claimIntId)
                .ToListAsync();
        }

        public async Task<AccommodationRequest?> LoadAccommodationGroup(AccommodationRequestIdentification groupId)
        {
            var groupIntId = groupId.AccommodationRequestId;
            return await AccommodationGroupQuery(groupId.ProjectId.Value)
                .SingleOrDefaultAsync(request => request.Id == groupIntId);
        }

        /// <summary>
        /// То же ядро, что у <see cref="RoomCategoryPlanRepository"/>, но на <see cref="ProjectInfo"/>
        /// хэндла: иначе конструктор плана не признал бы типы проживания своими (ADR013). Запрос
        /// без трекинга, поэтому трекаемые сущности мутации он не задевает.
        /// </summary>
        public async Task<RoomCategoryPlan> LoadRoomCategoryPlan(AccommodationTypeIdentification typeId)
        {
            // Категорию по типу знают только метаданные (ADR018, §2).
            var type = ProjectInfo.AccommodationSettings.GetTypeById(typeId);
            return await new RoomCategoryPlanLoader(ctx).LoadOneAsync(ProjectInfo, type.RoomCategoryId)
                ?? throw new InvalidOperationException(
                    $"Room category {type.RoomCategoryId} of accommodation type {typeId} is not found");
        }

        public async Task<AccommodationInvite> LoadInvite(AccommodationInviteIdentification inviteId)
        {
            var inviteIntId = inviteId.AccommodationInviteId;
            var projectIntId = inviteId.ProjectId.Value;
            return await ctx.Set<AccommodationInvite>()
                    .Include(invite => invite.From)
                    .Include(invite => invite.To)
                    .SingleOrDefaultAsync(invite => invite.Id == inviteIntId && invite.ProjectId == projectIntId)
                ?? throw new JoinRpgEntityNotFoundException(inviteIntId, nameof(AccommodationInvite));
        }

        /// <summary>
        /// Строка группы без связей. Решения о составе и свободном месте принимаются по плану
        /// поселения (ADR022 §4), а от трекаемой группы приглашению нужны только её существование
        /// и тип — поэтому ни состав, ни жильцы комнаты здесь не грузятся.
        /// </summary>
        private IQueryable<AccommodationRequest> AccommodationGroupQuery(int projectId)
            => ctx.Set<AccommodationRequest>()
                .Where(request => request.ProjectId == projectId);
    }

    private sealed class ClaimUpdateHandle(
        MyDbContext ctx,
        CharacterInfoLoader loader,
        Project project,
        ProjectInfo projectInfo,
        Character character,
        CharacterInfo characterInfo,
        Claim claim,
        CharacterClaimInfo claimInfo)
        : CharacterAggregateUpdateHandle(ctx, loader, project, projectInfo, character, characterInfo),
            IClaimUpdateHandle
    {
        public Claim Claim { get; } = claim;

        public ClaimInCharacter ClaimSnapshot { get; } = new(characterInfo, claimInfo);
    }
}
