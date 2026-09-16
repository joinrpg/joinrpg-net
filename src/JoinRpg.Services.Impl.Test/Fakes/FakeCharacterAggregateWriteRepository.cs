using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Write-репозиторий агрегата персонажа (ADR014) поверх <see cref="MockedProject"/>: отдаёт
/// согласованную четвёрку <see cref="Project"/>/<see cref="ProjectInfo"/>/<see cref="CharacterInfo"/>/
/// инициатор, собранную теми же фабриками, что и боевой код.
/// </summary>
internal sealed class FakeCharacterAggregateWriteRepository(MockedProject mock) : ICharacterAggregateWriteRepository
{
    public Task<ICharacterAggregateUpdateHandle> LoadCharacterForUpdate(
        CharacterIdentification characterId,
        UserIdentification initiatorId)
    {
        var character = FindCharacter(mock, characterId);
        return Task.FromResult<ICharacterAggregateUpdateHandle>(
            new Handle(mock, character, FindInitiator(mock, initiatorId)));
    }

    public Task<IClaimUpdateHandle> LoadClaimForUpdate(
        ClaimIdentification claimId,
        UserIdentification initiatorId)
    {
        var claim = FindClaim(mock, claimId);
        var character = FindCharacter(mock, claim.GetCharacterId());
        return Task.FromResult<IClaimUpdateHandle>(
            new ClaimHandle(mock, character, FindInitiator(mock, initiatorId), claim));
    }

    private static Character FindCharacter(MockedProject mock, CharacterIdentification characterId)
        => mock.Project.Characters.SingleOrDefault(c => c.CharacterId == characterId.CharacterId)
            ?? throw new JoinRpgEntityNotFoundException(characterId.CharacterId, "character");

    private static Claim FindClaim(MockedProject mock, ClaimIdentification claimId)
    {
        var claim = mock.Project.Claims.SingleOrDefault(c => c.ClaimId == claimId.ClaimId)
            ?? throw new JoinRpgEntityNotFoundException(claimId.ClaimId, "claim");

        // Достраиваем навигацию Player, которую боевой репозиторий грузит явным
        // Include(c => c.Player.Claims). У заявки, только что созданной сервисом, проставлен лишь
        // PlayerUserId: в бою она перечитывается из базы, здесь — остаётся тем же объектом.
        if (claim.Player is null)
        {
            claim.Player = mock.TryGetUser(claim.PlayerUserId)
                ?? throw new NotSupportedException($"Моку неизвестен пользователь {claim.PlayerUserId}");
            claim.Player.Claims.Add(claim);
        }

        return claim;
    }

    /// <summary>
    /// Инициатор ищется среди пользователей, известных моку: игроки заявок и мастера из ACL.
    /// </summary>
    private static User FindInitiator(MockedProject mock, UserIdentification initiatorId)
        => KnownUsers(mock).FirstOrDefault(u => u.UserId == initiatorId.Value)
            ?? throw new JoinRpgEntityNotFoundException(initiatorId.Value, "user");

    private static IEnumerable<User> KnownUsers(MockedProject mock)
    {
        yield return mock.Player;
        yield return mock.Master;
        foreach (var acl in mock.Project.ProjectAcls)
        {
            if (acl.User is { } user)
            {
                yield return user;
            }
        }
        foreach (var claim in mock.Project.Claims)
        {
            if (claim.Player is { } player)
            {
                yield return player;
            }
        }
    }

    private class Handle : ICharacterAggregateUpdateHandle
    {
        private readonly MockedProject mock;

        public Handle(MockedProject mock, Character character, User initiator)
        {
            this.mock = mock;
            Character = character;
            Initiator = initiator;

            // Снимок ДО, согласованный с текущим Project (как делает боевой репозиторий при загрузке).
            // Порядок важен: ReInitProjectInfo подменяет экземпляр ProjectInfo, а конструктор
            // CharacterInfo требует ровно тот же экземпляр (ADR013), поэтому агрегат строится ПОСЛЕ.
            mock.ReInitProjectInfo();
            ProjectInfo = mock.ProjectInfo;
            CharacterInfo = mock.GetCharacterInfo(character);
        }

        public Project Project => mock.Project;

        public ProjectInfo ProjectInfo { get; private set; }

        public Character Character { get; }

        public CharacterInfo CharacterInfo { get; }

        public User Initiator { get; }

        /// <summary>Всё, что сервис добавил в контекст, в порядке добавления.</summary>
        public List<object> Added { get; } = [];

        /// <summary>Всё, что сервис удалил из контекста, в порядке удаления.</summary>
        public List<object> Removed { get; } = [];

        public void Add(object entity)
        {
            Added.Add(entity);
            // Имитация relationship fixup EF6: реальный DbContext синхронно связывает добавленную
            // сущность с уже загруженными navigation-коллекциями того же контекста.
            switch (entity)
            {
                case Character character:
                    AddOnce(mock.Project.Characters, character);
                    break;
                case Claim claim:
                    AddOnce(mock.Project.Claims, claim);
                    if (claim.Character is { } claimCharacter)
                    {
                        AddOnce(claimCharacter.Claims, claim);
                    }
                    if (claim.Player is { } player)
                    {
                        AddOnce(player.Claims, claim);
                    }
                    break;
                case Comment comment:
                    if (comment.Discussion is { } discussion)
                    {
                        AddOnce(discussion.Comments, comment);
                    }
                    break;
                default:
                    break;
            }
        }

        public void Remove(object entity)
        {
            Removed.Add(entity);
            switch (entity)
            {
                case Character character:
                    _ = mock.Project.Characters.Remove(character);
                    break;
                case Claim claim:
                    _ = mock.Project.Claims.Remove(claim);
                    _ = claim.Character?.Claims.Remove(claim);
                    _ = claim.Player?.Claims.Remove(claim);
                    break;
                case Comment comment:
                    _ = comment.Discussion?.Comments.Remove(comment);
                    break;
                default:
                    break;
            }
        }

        private static void AddOnce<T>(ICollection<T> collection, T entity)
        {
            if (!collection.Contains(entity))
            {
                collection.Add(entity);
            }
        }

        public Task<ProjectInfo> RefreshProjectInfo()
        {
            mock.ReInitProjectInfo();
            return Task.FromResult(ProjectInfo = mock.ProjectInfo);
        }

        public Task<Claim> LoadOtherClaim(ClaimIdentification claimId)
            => Task.FromResult(FindClaim(mock, claimId));

        public Task<(Character Entity, CharacterInfo Info)> LoadOtherCharacter(CharacterIdentification characterId)
        {
            var entity = FindCharacter(mock, characterId);
            return Task.FromResult((entity, mock.GetCharacterInfo(entity)));
        }

        public Task<IReadOnlyCollection<PlotElement>> LoadDirectPlotsForCharacter(CharacterIdentification characterId)
            => Task.FromResult<IReadOnlyCollection<PlotElement>>(
                [.. mock.PlotElements.Where(e => e.TargetCharacters.Any(c => c.CharacterId == characterId.CharacterId))]);
    }

    private sealed class ClaimHandle : Handle, IClaimUpdateHandle
    {
        public ClaimHandle(MockedProject mock, Character character, User initiator, Claim claim)
            : base(mock, character, initiator)
        {
            Claim = claim;
            // Ровно тот же экземпляр, что лежит в CharacterInfo.Claims — так делает боевой
            // репозиторий, и на этом держатся проверки, читающие снимок заявки через агрегат.
            ClaimInfo = CharacterInfo.Claims.SingleOrDefault(c => c.ClaimId.ClaimId == claim.ClaimId)
                ?? throw new JoinRpgEntityNotFoundException(claim.ClaimId, "claim");
        }

        public Claim Claim { get; }

        public CharacterClaimInfo ClaimInfo { get; }
    }
}
