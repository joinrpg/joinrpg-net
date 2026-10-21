using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Write-репозиторий агрегата персонажа (ADR014) поверх <see cref="MockedProject"/>: отдаёт
/// согласованную тройку <see cref="Project"/>/<see cref="ProjectInfo"/>/<see cref="CharacterInfo"/>,
/// собранную теми же фабриками, что и боевой код.
/// </summary>
internal sealed class FakeCharacterAggregateWriteRepository(MockedProject mock) : ICharacterAggregateWriteRepository
{
    public Task<ICharacterAggregateUpdateHandle> LoadCharacterForUpdate(
        CharacterIdentification characterId)
    {
        var character = FindCharacter(mock, characterId);
        return Task.FromResult<ICharacterAggregateUpdateHandle>(
            new Handle(mock, character));
    }

    public Task<int> RevokeSensitiveDataConsents(ProjectIdentification projectId)
    {
        var consented = mock.Project.Claims
            .Where(c => c.ProjectId == projectId.Value && c.PlayerAllowedSenstiveData)
            .ToList();
        foreach (var claim in consented)
        {
            claim.PlayerAllowedSenstiveData = false;
        }
        return Task.FromResult(consented.Count);
    }

    /// <summary>Мок — один проект, поэтому «в любом проекте» значит «в этом».</summary>
    public Task<bool> IsClaimIdTaken(int claimId) => Task.FromResult(mock.Project.Claims.Any(c => c.ClaimId == claimId));

    public Task<bool> ClaimExists(ClaimIdentification claimId)
        => Task.FromResult(mock.Project.Claims.Any(c => c.ClaimId == claimId.ClaimId && c.ProjectId == claimId.ProjectId.Value));

    /// <summary>
    /// Оболочка заявки так же, как в бою: «предложена», с пустым обсуждением, с заданным id.
    /// </summary>
    public Task<bool> InsertRestoredClaimShell(RestoredClaimShell shell)
    {
        if (mock.Project.Claims.Any(c => c.ClaimId == shell.ClaimId.ClaimId))
        {
            return Task.FromResult(false);
        }

        var character = FindCharacter(mock, shell.CharacterId);
        var player = mock.TryGetUser(shell.PlayerId.Value)
            ?? throw new JoinRpgEntityNotFoundException(shell.PlayerId.Value, "user");
        var claim = mock.CreateClaim(character, player);
        claim.ClaimId = shell.ClaimId.ClaimId;
        claim.ClaimStatus = ClaimStatus.AddedByMaster;
        claim.ResponsibleMasterUserId = shell.ResponsibleMasterId.Value;
        claim.ResponsibleMasterUser = mock.TryGetUser(shell.ResponsibleMasterId.Value)
            ?? throw new JoinRpgEntityNotFoundException(shell.ResponsibleMasterId.Value, "user");
        claim.CreateDate = shell.CreatedAt;
        claim.PlayerAcceptedDate = shell.CreatedAt;
        claim.LastUpdateDateTime = shell.CreatedAt;
        return Task.FromResult(true);
    }

    /// <summary>
    /// Счётчик IDENTITY персонажей в моке: id не ниже него — «не потерянный» (как <c>IDENT_CURRENT</c> в бою).
    /// </summary>
    internal const int CharacterIdentityCurrent = 100_000;

    /// <summary>
    /// Оболочка персонажа так же, как в бою: активный персонаж заданных типа и видимости в корневой группе.
    /// Мок — один проект, поэтому «занят другим проектом» не бывает.
    /// </summary>
    public Task<RestoredShellInsertResult> InsertRestoredCharacterShell(RestoredCharacterShell shell)
    {
        if (mock.Project.Characters.Any(c => c.CharacterId == shell.CharacterId.CharacterId))
        {
            return Task.FromResult(RestoredShellInsertResult.AlreadyExists);
        }
        if (shell.CharacterId.CharacterId >= CharacterIdentityCurrent)
        {
            return Task.FromResult(RestoredShellInsertResult.NotLostId);
        }

        var character = mock.CreateCharacter(shell.CharacterName);
        character.CharacterId = shell.CharacterId.CharacterId;
        character.CharacterType = shell.CharacterType;
        character.IsHot = shell.IsHot;
        character.CharacterSlotLimit = null;
        (character.IsPublic, character.HidePlayerForCharacter) = CharacterTypeInfo.GetVisibilityFlags(shell.Visibility);
        character.JsonData = "{}";
        character.Description = new MarkdownDbValue(shell.Description);
        character.ParentCharacterGroupIds = [shell.RootGroupId.CharacterGroupId];
        character.CreatedAt = character.UpdatedAt = shell.CreatedAt;
        character.CreatedById = character.UpdatedById = shell.CreatedBy.Value;
        return Task.FromResult(RestoredShellInsertResult.Inserted);
    }

    /// <summary>
    /// Счётчик IDENTITY комментариев в моке: id не ниже него — «не потерянный» (как <c>IDENT_CURRENT</c> в бою),
    /// а комментарий без id получает новый — выше счётчика.
    /// </summary>
    internal const int CommentIdentityCurrent = 100_000;

    /// <summary>
    /// Восстановленный комментарий так же, как в бою: занятость id решает обсуждение заявки, повтор
    /// без id узнаётся по автору, времени и тексту (побайтно). Обсуждения в моке не сохранены (id -1),
    /// поэтому «своё» обсуждение — тот же объект, а не тот же id.
    /// </summary>
    public Task<RestoredCommentInsertResult> InsertRestoredClaimComment(RestoredClaimCommentRow comment)
    {
        var claim = mock.Project.Claims.SingleOrDefault(
                c => c.ClaimId == comment.ClaimId.ClaimId && c.ProjectId == comment.ClaimId.ProjectId.Value)
            ?? throw new JoinRpgEntityNotFoundException(comment.ClaimId.ClaimId, "claim");
        var discussion = claim.CommentDiscussion;
        var allComments = AllClaimComments();
        Comment? parent = null;
        if (comment.ParentCommentId is { } parentId)
        {
            parent = discussion.Comments.SingleOrDefault(c => c.CommentId == parentId)
                ?? throw new InvalidOperationException($"Родителя {parentId} нет в обсуждении заявки {comment.ClaimId}");
        }

        int commentId;
        if (comment.CommentId is { } id)
        {
            if (allComments.FirstOrDefault(c => c.CommentId == id) is { } existing)
            {
                return Task.FromResult(discussion.Comments.Contains(existing)
                    ? RestoredCommentInsertResult.AlreadyExists
                    : RestoredCommentInsertResult.TakenElsewhere);
            }
            if (id <= 0 || id >= CommentIdentityCurrent)
            {
                return Task.FromResult(RestoredCommentInsertResult.NotLostId);
            }
            commentId = id;
        }
        else
        {
            if (discussion.Comments.Any(c => c.AuthorUserId == comment.AuthorId.Value
                && c.CreatedAt == comment.CreatedAt
                && string.Equals(c.CommentText.Text.Contents, comment.Text, StringComparison.Ordinal)))
            {
                return Task.FromResult(RestoredCommentInsertResult.AlreadyExists);
            }
            commentId = NextCommentId(allComments);
        }

        var inserted = NewComment(discussion, commentId, comment.AuthorId, comment.IsCommentByPlayer, comment.IsVisibleToPlayer,
            comment.ExtraAction, comment.CreatedAt, comment.Text);
        inserted.ParentCommentId = parent?.CommentId;
        inserted.Parent = parent;
        return Task.FromResult(RestoredCommentInsertResult.Inserted);
    }

    /// <summary>
    /// Восстановленная финансовая операция так же, как в бою: прежний id «уже есть», только если это
    /// операция этого обсуждения; повтор без id — операция того же автора, вида и суммы в пределах минуты.
    /// </summary>
    public Task<RestoredFinanceOperationInsert> InsertRestoredFinanceOperation(RestoredFinanceOperationRow operation)
    {
        var claim = mock.Project.Claims.SingleOrDefault(
                c => c.ClaimId == operation.ClaimId.ClaimId && c.ProjectId == operation.ClaimId.ProjectId.Value)
            ?? throw new JoinRpgEntityNotFoundException(operation.ClaimId.ClaimId, "claim");
        var paymentType = mock.Project.PaymentTypes.SingleOrDefault(pt => pt.PaymentTypeId == operation.PaymentTypeId.PaymentTypeId
                && operation.PaymentTypeId.ProjectId == operation.ClaimId.ProjectId)
            ?? throw new InvalidOperationException($"Типа оплаты {operation.PaymentTypeId} нет в проекте");
        var extraAction = operation.OperationType switch
        {
            FinanceOperationType.Submit => CommentExtraAction.PaidFee,
            FinanceOperationType.Refund => CommentExtraAction.RefundFee,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation.OperationType, null),
        };
        var discussion = claim.CommentDiscussion;
        var allComments = AllClaimComments();

        int commentId;
        if (operation.OperationId is { } id)
        {
            if (allComments.FirstOrDefault(c => c.CommentId == id) is { } existing)
            {
                return Task.FromResult(new RestoredFinanceOperationInsert(
                    discussion.Comments.Contains(existing) && existing.Finance is not null
                        ? RestoredCommentInsertResult.AlreadyExists
                        : RestoredCommentInsertResult.TakenElsewhere,
                    id));
            }
            if (id <= 0 || id >= CommentIdentityCurrent)
            {
                return Task.FromResult(new RestoredFinanceOperationInsert(RestoredCommentInsertResult.NotLostId, id));
            }
            commentId = id;
        }
        else
        {
            if (discussion.Comments
                    .Where(c => c.AuthorUserId == operation.AuthorId.Value
                        && c.Finance is { } f && f.OperationType == operation.OperationType && f.MoneyAmount == operation.MoneyAmount
                        && (c.CreatedAt - operation.CreatedAt).Duration() <= TimeSpan.FromMinutes(1))
                    .OrderBy(c => (c.CreatedAt - operation.CreatedAt).Duration())
                    .ThenBy(c => c.CommentId)
                    .FirstOrDefault() is { } same)
            {
                return Task.FromResult(new RestoredFinanceOperationInsert(RestoredCommentInsertResult.AlreadyExists, same.CommentId));
            }
            commentId = NextCommentId(allComments);
        }

        var comment = NewComment(discussion, commentId, operation.AuthorId, operation.IsCommentByPlayer, isVisibleToPlayer: true,
            extraAction, operation.CreatedAt, operation.Text);
        var finance = new FinanceOperation
        {
            CommentId = commentId,
            Comment = comment,
            ProjectId = claim.ProjectId,
            Project = mock.Project,
            ClaimId = claim.ClaimId,
            Claim = claim,
            MoneyAmount = operation.MoneyAmount,
            PaymentTypeId = paymentType.PaymentTypeId,
            PaymentType = paymentType,
            Created = operation.CreatedAt,
            Changed = operation.ChangedAt,
            OperationDate = operation.OperationDate,
            State = operation.State,
            OperationType = operation.OperationType,
        };
        comment.Finance = finance;
        claim.FinanceOperations ??= [];
        claim.FinanceOperations.Add(finance);
        return Task.FromResult(new RestoredFinanceOperationInsert(RestoredCommentInsertResult.Inserted, commentId));
    }

    private List<Comment> AllClaimComments() => [.. mock.Project.Claims.SelectMany(c => c.CommentDiscussion.Comments)];

    /// <summary>Новый id комментария — выше счётчика, как выдала бы IDENTITY.</summary>
    private static int NextCommentId(List<Comment> allComments)
        => Math.Max(CommentIdentityCurrent, allComments.Select(c => c.CommentId).DefaultIfEmpty().Max()) + 1;

    private Comment NewComment(CommentDiscussion discussion, int commentId, UserIdentification authorId, bool isCommentByPlayer,
        bool isVisibleToPlayer, CommentExtraAction? extraAction, DateTime createdAt, string text)
    {
        var comment = new Comment
        {
            CommentId = commentId,
            ProjectId = discussion.ProjectId,
            Project = mock.Project,
            CommentDiscussionId = discussion.CommentDiscussionId,
            Discussion = discussion,
            ParentCommentId = null,
            Author = mock.TryGetUser(authorId.Value)
                ?? throw new JoinRpgEntityNotFoundException(authorId.Value, "user"),
            AuthorUserId = authorId.Value,
            CommentText = new CommentText { CommentId = commentId, Text = new MarkdownDbValue(text) },
            IsCommentByPlayer = isCommentByPlayer,
            IsVisibleToPlayer = isVisibleToPlayer,
            ExtraAction = extraAction,
            CreatedAt = createdAt,
            LastEditTime = createdAt,
        };
        discussion.Comments.Add(comment);
        return comment;
    }

    public Task<IClaimUpdateHandle> LoadClaimForUpdate(ClaimIdentification claimId)
    {
        var claim = FindClaim(mock, claimId);
        var character = FindCharacter(mock, claim.GetCharacterId());
        return Task.FromResult<IClaimUpdateHandle>(
            new ClaimHandle(mock, character, claim));
    }

    /// <summary>
    /// Имитация relationship fixup EF6 при <c>DetectChanges</c> для групп проживающих: сервисы
    /// меняют состав группы ссылкой заявки (ключ и навигация вместе, ADR022 §4), а обратную
    /// коллекцию <see cref="AccommodationRequest.Subjects"/> EF синхронизирует сам. Без этого
    /// мок после выхода из группы держал бы заявку в двух группах сразу, и следующий план
    /// поселения не построился бы. Источник истины — навигация заявки.
    /// </summary>
    internal static void DetectAccommodationChanges(MockedProject mock)
    {
        foreach (var request in mock.AccommodationRequests)
        {
            foreach (var subject in request.Subjects.Where(subject => subject.AccommodationRequest != request).ToList())
            {
                _ = request.Subjects.Remove(subject);
            }
        }

        foreach (var claim in mock.Project.Claims)
        {
            if (claim.AccommodationRequest is { } request)
            {
                Handle.AddOnce(request.Subjects, claim);
                claim.AccommodationRequest_Id = request.Id;
            }
            else
            {
                claim.AccommodationRequest_Id = null;
            }
        }
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

        // То же и с коллекцией финансовых операций: ClaimCreationContext.NewClaim её не задаёт,
        // в бою она появляется при перечитывании заявки из базы. Без этого здесь падает с NRE
        // любое обращение к FinanceOperations, включая ApprovedFinanceOperations.
        claim.FinanceOperations ??= [];

        return claim;
    }

    private class Handle : ICharacterAggregateUpdateHandle
    {
        private readonly MockedProject mock;

        public Handle(MockedProject mock, Character character)
        {
            this.mock = mock;
            Character = character;

            // Снимок ДО, согласованный с текущим Project (как делает боевой репозиторий при загрузке).
            // Порядок важен: ReInitProjectInfo подменяет экземпляр ProjectInfo, а конструктор
            // CharacterInfo требует ровно тот же экземпляр (ADR013), поэтому агрегат строится ПОСЛЕ.
            mock.ReInitProjectInfo();
            ProjectInfo = mock.ProjectInfo;
            CharacterInfo = mock.GetCharacterInfo(character);
        }

        public Project Project => mock.Project;

        public ProjectInfo ProjectInfo { get; }

        public Character Character { get; }

        public CharacterInfo CharacterInfo { get; }

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
                    // Id выдаёт БД при сохранении; без него снимок заявки не построить — заявка-одиночка
                    // ссылается сама на себя положительным id (ADR022).
                    if (claim.ClaimId == 0)
                    {
                        claim.ClaimId = mock.Project.Claims.Select(c => c.ClaimId).DefaultIfEmpty().Max() + 1;
                    }
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
                case AccommodationRequest request:
                    // Как и у заявки: без Id снимок заявки из этой группы не построить (ADR022).
                    if (request.Id == 0)
                    {
                        request.Id = mock.AccommodationRequests.Select(r => r.Id).DefaultIfEmpty().Max() + 1;
                    }
                    AddOnce(mock.AccommodationRequests, request);
                    // EF6 связывает жильцов с новой заявкой на поселение обратной навигацией.
                    foreach (var subject in request.Subjects)
                    {
                        subject.AccommodationRequest = request;
                    }
                    break;
                case AccommodationInvite invite:
                    // Навигации на приглашения у проекта нет, поэтому мок держит их отдельной
                    // коллекцией — она и играет роль DbSet<AccommodationInvite>.
                    AddOnce(mock.AccommodationInvites, invite);
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
                case AccommodationRequest request:
                    // Навигации на заявки проживания у проекта нет, поэтому роль DbSet играют
                    // отдельные коллекции мока — см. Add.
                    _ = mock.AccommodationRequests.Remove(request);
                    break;
                case AccommodationInvite invite:
                    _ = mock.AccommodationInvites.Remove(invite);
                    break;
                default:
                    break;
            }
        }

        internal static void AddOnce<T>(ICollection<T> collection, T entity)
        {
            if (!collection.Contains(entity))
            {
                collection.Add(entity);
            }
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

        public Task<IReadOnlyCollection<AccommodationInvite>> LoadInvitesForClaim(ClaimIdentification claimId)
            => Task.FromResult<IReadOnlyCollection<AccommodationInvite>>(
                [.. mock.AccommodationInvites.Where(
                    i => i.ToClaimId == claimId.ClaimId || i.FromClaimId == claimId.ClaimId)]);

        public Task<AccommodationRequest?> LoadAccommodationGroup(AccommodationRequestIdentification groupId)
            => Task.FromResult(mock.AccommodationRequests.SingleOrDefault(
                request => request.Id == groupId.AccommodationRequestId));

        public async Task<RoomCategoryPlan> LoadRoomCategoryPlan(AccommodationTypeIdentification typeId)
        {
            // Как и боевой: неизвестный тип — исключение метаданных, а не null.
            _ = ProjectInfo.AccommodationSettings.GetTypeById(typeId);

            var plan = await new FakeRoomCategoryPlanRepository(mock).GetPlanForTypeOrDefault(typeId)
                ?? throw new InvalidOperationException($"Нет плана для типа {typeId}");

            // Фейковый загрузчик строит план на mock.ProjectInfo. Хэндл взял тот же экземпляр при
            // создании; если тест между ними пересобрал метаданные, план уже не на ProjectInfo
            // хэндла — боевой код такого не допускает (ADR013), пусть и тест падает громко.
            if (!ReferenceEquals(plan.ProjectInfo, ProjectInfo))
            {
                throw new InvalidOperationException("План построен не на ProjectInfo хэндла");
            }

            return plan;
        }

        public Task<AccommodationInvite> LoadInvite(AccommodationInviteIdentification inviteId)
            => Task.FromResult(
                mock.AccommodationInvites.SingleOrDefault(
                    invite => invite.Id == inviteId.AccommodationInviteId
                        && invite.ProjectId == inviteId.ProjectId.Value)
                ?? throw new JoinRpgEntityNotFoundException(
                    inviteId.AccommodationInviteId, nameof(AccommodationInvite)));
    }

    private sealed class ClaimHandle : Handle, IClaimUpdateHandle
    {
        public ClaimHandle(MockedProject mock, Character character, Claim claim)
            : base(mock, character)
        {
            Claim = claim;
            // Ровно тот же экземпляр, что лежит в CharacterInfo.Claims — так делает боевой
            // репозиторий, и на этом держатся проверки, читающие снимок заявки через агрегат.
            var claimInfo = CharacterInfo.Claims.SingleOrDefault(c => c.ClaimId.ClaimId == claim.ClaimId)
                ?? throw new JoinRpgEntityNotFoundException(claim.ClaimId, "claim");
            ClaimSnapshot = new ClaimInCharacter(CharacterInfo, claimInfo);
        }

        public Claim Claim { get; }

        public ClaimInCharacter ClaimSnapshot { get; }
    }
}
