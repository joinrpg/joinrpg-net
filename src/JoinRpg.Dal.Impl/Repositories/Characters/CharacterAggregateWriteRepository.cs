using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DataModel.Extensions;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

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
        CharacterIdentification characterId,
        UserIdentification initiatorId)
    {
        var (project, projectInfo) = await LoadProject(characterId.ProjectId);

        var character = await LoadCharacterEntity(characterId);
        var characterInfo = await LoadCharacterInfo(characterId, projectInfo);
        var initiator = await LoadInitiator(initiatorId);

        return new CharacterAggregateUpdateHandle(
            ctx, loader, project, projectInfo, character, characterInfo, initiator);
    }

    public async Task<IClaimUpdateHandle> LoadClaimForUpdate(
        ClaimIdentification claimId,
        UserIdentification initiatorId)
    {
        var (project, projectInfo) = await LoadProject(claimId.ProjectId);

        var claim = await LoadClaimEntity(claimId);

        var characterId = claim.GetCharacterId();
        var character = await LoadCharacterEntity(characterId);
        var characterInfo = await LoadCharacterInfo(characterId, projectInfo);

        var claimInfo = characterInfo.Claims.SingleOrDefault(c => c.ClaimId == claimId)
            ?? throw new JoinRpgEntityNotFoundException(claimId.ClaimId, "claim");

        var initiator = await LoadInitiator(initiatorId);

        return new ClaimUpdateHandle(
            ctx, loader, project, projectInfo, character, characterInfo, initiator, claim, claimInfo);
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

    private async Task<User> LoadInitiator(UserIdentification initiatorId)
        => await ctx.Set<User>().FindAsync(initiatorId.Value)
            ?? throw new JoinRpgEntityNotFoundException(initiatorId.Value, "user");

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
            .Include(c => c.FinanceOperations);

    private class CharacterAggregateUpdateHandle(
        MyDbContext ctx,
        CharacterInfoLoader loader,
        Project project,
        ProjectInfo projectInfo,
        Character character,
        CharacterInfo characterInfo,
        User initiator)
        : ICharacterAggregateUpdateHandle
    {
        public Project Project { get; private set; } = project;

        public ProjectInfo ProjectInfo { get; private set; } = projectInfo;

        public Character Character { get; } = character;

        public CharacterInfo CharacterInfo { get; } = characterInfo;

        public User Initiator { get; } = initiator;

        public void Add(object entity) => _ = ctx.Set(entity.GetType()).Add(entity);

        public void Remove(object entity) => _ = ctx.Set(entity.GetType()).Remove(entity);

        public async Task<ProjectInfo> RefreshProjectInfo()
        {
            var projectId = ProjectInfo.ProjectId;
            Project = await ProjectLoaderCommon.GetProjectWithFieldsAsync(ctx, projectId.Value, skipCache: true)
                ?? throw new JoinRpgEntityNotFoundException(projectId.Value, "project");
            return ProjectInfo = ProjectMetadataRepository.CreateInfoFromProject(Project, projectId);
        }

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
    }

    private sealed class ClaimUpdateHandle(
        MyDbContext ctx,
        CharacterInfoLoader loader,
        Project project,
        ProjectInfo projectInfo,
        Character character,
        CharacterInfo characterInfo,
        User initiator,
        Claim claim,
        CharacterClaimInfo claimInfo)
        : CharacterAggregateUpdateHandle(ctx, loader, project, projectInfo, character, characterInfo, initiator),
            IClaimUpdateHandle
    {
        public Claim Claim { get; } = claim;

        public CharacterClaimInfo ClaimInfo { get; } = claimInfo;
    }
}
