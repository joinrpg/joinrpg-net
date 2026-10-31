using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel.Extensions;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.DataModel.Mocks.Fakes;

/// <summary>
/// Read-репозиторий заявок — общий для всех тестовых проектов. Работает в двух режимах:
/// поверх <see cref="MockedProject"/> (нужен, например, автоприёму, который перечитывает заявку
/// заново, а не смотрит в мутированный граф предыдущей операции — ADR014, §7) и без мока —
/// на идентификаторах, наполняемых тестом напрямую (сверке платежей ПСКБ хватает кортежей
/// «проект-игрок-заявка», без графа сущностей).
/// </summary>
/// <remarks>
/// Реализованы ровно те методы, которые нужны проверяемым операциям; остальные бросают
/// <see cref="NotSupportedException"/> намеренно — чтобы поход за незапланированными данными был
/// виден в тесте, а не подменялся пустышкой. То же с методами над моком, когда фейк создан
/// без него.
/// </remarks>
public sealed class FakeClaimsRepository(MockedProject? mock = null) : IClaimsRepository
{
    /// <summary>Заранее заготовленные заявки по (ProjectId, UserId) ответственного мастера.</summary>
    public Dictionary<(int ProjectId, int UserId), List<Claim>> ClaimsByResponsibleMaster { get; } = [];

    public Task<IReadOnlyCollection<Claim>> GetClaimsForMaster(int projectId, int userId, ClaimStatusSpec status)
        => Task.FromResult<IReadOnlyCollection<Claim>>(
            ClaimsByResponsibleMaster.TryGetValue((projectId, userId), out var claims) ? claims : []);

    public Task<Claim?> GetClaim(ClaimIdentification claimId)
        => Task.FromResult(Mock.Project.Claims.SingleOrDefault(
            claim => claim.ProjectId == claimId.ProjectId.Value && claim.ClaimId == claimId.ClaimId));

    public void Dispose() { }

    /// <summary>Мок, если фейк создан с ним; обращение без мока — незапланированный поход за данными.</summary>
    private MockedProject Mock => mock ?? throw new NotSupportedException("Фейк создан без MockedProject");

    public Task<IReadOnlyCollection<Claim>> GetClaims(int projectId, ClaimStatusSpec status) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(UserIdentification userId, ClaimStatusSpec status) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<MyClaimShortInfo>> GetMyActiveClaimsInActiveProjects(UserIdentification userId) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(ProjectIdentification projectId, UserIdentification userId, ClaimStatusSpec status) => throw new NotSupportedException();
    /// <summary>
    /// Заголовки заявок, которые вернёт <see cref="GetClaimHeadersWithPlayer(IReadOnlyCollection{ClaimIdentification})"/>.
    /// Наполняются тестом напрямую, а не выводятся из мока: игрок заголовка нарочно не обязан
    /// существовать в <c>MockedProject</c> — расчёту получателей хватает идентификатора.
    /// </summary>
    public List<ClaimWithPlayer> Headers { get; } = [];

    /// <remarks>
    /// Порядок выдачи намеренно обратный порядку запрошенных идентификаторов: настоящий запрос
    /// порядок не обещает (<c>ORDER BY</c> в нём нет), а текст уведомления от порядка зависит.
    /// Обратный порядок — самый дешёвый способ поймать код, который на порядок репозитория
    /// полагается.
    /// </remarks>
    public Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(IReadOnlyCollection<ClaimIdentification> claimIds)
        => Task.FromResult<IReadOnlyCollection<ClaimWithPlayer>>(
            [.. Headers.Where(header => claimIds.Contains(header.ClaimId)).Reverse()]);
    /// <remarks>В отличие от <see cref="Headers"/>, выводится из мока: здесь важен статус заявки.</remarks>
    public Task<IReadOnlyCollection<ClaimWithPlayer>> GetApprovedClaimHeadersWithoutAccommodation(ProjectIdentification projectId)
        => Task.FromResult<IReadOnlyCollection<ClaimWithPlayer>>(
            [.. Mock.Project.Claims
                .Where(claim => claim.ClaimStatus == ClaimStatus.Approved && claim.AccommodationRequest_Id == null)
                .Select(ToHeader)]);

    public Task<IReadOnlyCollection<ClaimIdentification>> GetActiveClaimIdsWithoutAccommodation(ProjectIdentification projectId) => throw new NotSupportedException();

    /// <summary>Заявки игроков в проектах — то, на что встанет потерянный платёж.</summary>
    private readonly List<PlayerClaimId> playerClaims = [];

    /// <summary>Комментарии, добавленные тестом напрямую через <see cref="AddClaimComment"/>.</summary>
    private readonly List<CommentClaimId> commentClaims = [];

    public void AddClaim(int projectId, int userId, int claimId)
        => playerClaims.Add(new PlayerClaimId(
            new UserIdentification(userId),
            new ClaimIdentification(projectId, claimId)));

    /// <summary>Комментарий в обсуждении заявки — так его нашёл бы настоящий запрос.</summary>
    public void AddClaimComment(int commentId, int projectId, int claimId)
        => commentClaims.Add(new CommentClaimId(commentId, new ClaimIdentification(projectId, claimId)));

    /// <summary>
    /// Запросы к заявкам игроков: проект и номера игроков, которых спрашивали одним вызовом, —
    /// чтобы тест видел пачкование.
    /// </summary>
    public List<(int ProjectId, int[] UserIds)> PlayerIdRequests { get; } = [];

    /// <summary>
    /// Номера комментариев, которых спрашивали одним вызовом, — чтобы тест видел пачкование.
    /// </summary>
    public List<int[]> CommentIdRequests { get; } = [];

    public Task<IReadOnlyCollection<PlayerClaimId>> GetClaimIdsForPlayers(ProjectIdentification projectId, IReadOnlyCollection<UserIdentification> userIds)
    {
        PlayerIdRequests.Add((projectId.Value, [.. userIds.Select(userId => userId.Value)]));

        IReadOnlyCollection<PlayerClaimId> found =
        [
            .. playerClaims.Where(claim => claim.ClaimId.ProjectId == projectId && userIds.Contains(claim.UserId)),
        ];
        return Task.FromResult(found);
    }

    public Task<IReadOnlyCollection<CommentClaimId>> GetClaimIdsByCommentIds(IReadOnlyCollection<int> commentIds)
    {
        CommentIdRequests.Add([.. commentIds]);

        IReadOnlyCollection<CommentClaimId> found =
        [
            .. commentClaims.Where(entry => commentIds.Contains(entry.CommentId)),
        ];
        return Task.FromResult(found);
    }

    /// <summary>Заголовок заявки из мока — так его собрал бы настоящий репозиторий.</summary>
    public static ClaimWithPlayer ToHeader(Claim claim)
        => new()
        {
            ClaimId = claim.GetId(),
            CharacterName = claim.Character.CharacterName,
            Player = claim.Player.ToUserInfoHeader(),
            ExtraNicknames = null,
            ResponsibleMasterUserId = new UserIdentification(claim.ResponsibleMasterUserId),
            CharacterId = new CharacterIdentification(claim.ProjectId, claim.CharacterId),
        };

    public Task<Claim?> GetClaimWithDetails(ClaimIdentification claimId) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<Claim>> GetClaimsForGroups(ProjectIdentification projectId, ClaimStatusSpec active, CharacterGroupIdentification[] characterGroupsIds) => throw new NotSupportedException();
    /// <remarks>
    /// Выводится из мока. Как и настоящий запрос, смотрит только на прямых родителей персонажа:
    /// дочерние группы раскрывает вызывающий. Поддержан только <see cref="ClaimStatusSpec.Approved"/>.
    /// </remarks>
    public Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(IReadOnlyCollection<CharacterGroupIdentification> characterGroupsIds, ClaimStatusSpec spec)
    {
        if (spec != ClaimStatusSpec.Approved)
        {
            throw new NotSupportedException();
        }
        var groupIds = characterGroupsIds.Select(g => g.CharacterGroupId).ToHashSet();
        return Task.FromResult<IReadOnlyCollection<ClaimWithPlayer>>(
            [.. Mock.Project.Claims
                .Where(claim => claim.IsApproved && claim.Character.ParentCharacterGroupIds.Any(groupIds.Contains))
                .Select(ToHeader)]);
    }
    public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(int projectId, ClaimStatusSpec claimStatusSpec, int userId) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimsHeadersForPlayer(ProjectIdentification projectId, ClaimStatusSpec claimStatusSpec, UserIdentification userId) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<ClaimCountByMaster>> GetClaimsCountByMasters(int projectId, ClaimStatusSpec claimStatusSpec) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(ProjectIdentification projectId, ClaimStatusSpec approved) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<Claim>> GetClaimsForRoomType(int projectId, ClaimStatusSpec claimStatusSpec, int? roomTypeId) => throw new NotSupportedException();
    public Task<Dictionary<CommentDiscussionId, int>> GetUnreadDiscussionsForClaims(int projectId, ClaimStatusSpec claimStatusSpec, int userId, bool hasMasterAccess) => throw new NotSupportedException();
    public Task<Dictionary<CommentDiscussionId, int>> GetUnreadDiscussionsForPlayerClaims(UserIdentification playerId, UserIdentification viewerId) => throw new NotSupportedException();
}
