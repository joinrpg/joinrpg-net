using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Impl.Characters;

namespace JoinRpg.Services.Impl.Claims;

/// <summary>
/// Комментарий, который операция создания заявки «заказала» у сервиса, но который ещё не создан.
/// </summary>
/// <remarks>
/// Отложен не ради красоты: <c>CommentHelper.CreateClaimCommentWithNotification</c> кладёт
/// комментарий в <c>claim.CommentDiscussion</c>, а до первого сохранения у дискуссии
/// <c>CommentDiscussionId == -1</c>. Поэтому комментарий материализуется только между двумя
/// сохранениями — см. <c>CharacterPropsService.CreateClaim</c>.
/// </remarks>
internal sealed class DeferredComment(
    string CommentText,
    CommentExtraAction? ExtraAction,
    ClaimOperationType OperationType,
    Claim? TargetClaim = null)
{
    public string CommentText { get; } = CommentText;
    public CommentExtraAction? ExtraAction { get; } = ExtraAction;
    public ClaimOperationType OperationType { get; } = OperationType;

    /// <summary>
    /// Заявка, к которой относится комментарий. <c>null</c> — создаваемая заявка; иначе это соседняя
    /// заявка, которую мутирует та же операция: выход на вторую роль комментирует и старую заявку.
    /// </summary>
    public Claim? TargetClaim { get; } = TargetClaim;

    /// <summary>Уведомление создаётся, но не отправляется — см. <see cref="PendingComment.Silent"/>.</summary>
    public bool IsSilent { get; private set; }

    /// <inheritdoc cref="PendingComment.Silent"/>
    public DeferredComment Silent()
    {
        IsSilent = true;
        return this;
    }

    /// <summary>
    /// Дополнения уведомления, накопленные до его создания: само уведомление появится только между
    /// двумя сохранениями, а операция знает, чем его дополнить, уже сейчас.
    /// </summary>
    internal List<Func<ClaimSimpleChangedNotification, ClaimSimpleChangedNotification>> Decorators { get; } = [];

    /// <inheritdoc cref="PendingComment.Decorate"/>
    public DeferredComment Decorate(Func<ClaimSimpleChangedNotification, ClaimSimpleChangedNotification> decorator)
    {
        Decorators.Add(decorator);
        return this;
    }
}

/// <summary>
/// Контекст создания заявки (ADR014). Персонаж уже существует и трекается, а заявки ещё нет —
/// её строит фабрика операции через <see cref="NewClaim(ClaimStatus, bool)"/>.
/// </summary>
/// <param name="Character">Трекаемая EF-сущность персонажа, на которого подаётся заявка.</param>
/// <param name="CharacterInfo">Доменный снимок персонажа; по нему считаются правила подачи.</param>
/// <param name="Initiator">
/// Текущий пользователь как EF-сущность — только для легаси-канала писем.
/// </param>
/// <param name="Player">
/// Игрок, на которого оформляется заявка. При <see cref="ClaimOperation.AddByMaster"/> это
/// <b>не</b> тот, кто выполняет операцию. <c>null</c> — если операция игрока заранее не знает:
/// при выходе на вторую роль он берётся из исходной заявки уже внутри фабрики.
/// </param>
/// <param name="AddEntity">Добавление сущности в тот же <c>DbContext</c>.</param>
/// <param name="LoadOtherClaimCore">Загрузка соседней заявки тем же <c>DbContext</c>.</param>
internal abstract record ClaimCreationContext(
    Character Character,
    CharacterInfo CharacterInfo,
    ProjectInfo ProjectInfo,
    DateTime Now,
    ICurrentUserAccessor CurrentUser,
    User Initiator,
    UserInfo? Player,
    Action<object> AddEntity,
    Func<ClaimIdentification, Task<Claim>> LoadOtherClaimCore,
    FieldSaveHelper FieldSaveHelper)
    : CharacterOperationContext(ProjectInfo, Now, CurrentUser, FieldSaveHelper)
{
    /// <summary>
    /// Явный выход за границу агрегата: трекаемая заявка того же проекта. Нужен выходу на вторую
    /// роль — он не только создаёт новую заявку, но и мутирует старую, причём в том же сохранении.
    /// </summary>
    public Task<Claim> LoadOtherClaim(ClaimIdentification claimId) => LoadOtherClaimCore(claimId);

    /// <summary>
    /// Единственная точка построения <see cref="Claim"/>: до ADR014 эта конструкция была скопирована
    /// в <c>AddClaimFromUser</c>, <c>AddClaimFromMaster</c> и <c>MoveToSecondRole</c>.
    /// </summary>
    /// <remarks>
    /// Заявку в <c>DbContext</c> добавляет сервис — после того, как фабрика её вернёт.
    /// </remarks>
    /// <param name="claimStatus">Стартовый статус: подана игроком или предложена мастером.</param>
    /// <param name="playerAllowedSensitiveData">
    /// Разрешил ли <b>игрок</b> доступ к чувствительным данным. Мастер такого разрешения от имени
    /// игрока дать не может.
    /// </param>
    public Claim NewClaim(ClaimStatus claimStatus, bool playerAllowedSensitiveData)
        => NewClaimCore(
            claimStatus,
            playerAllowedSensitiveData,
            (Player ?? throw new InvalidOperationException(
                "Операция не назвала игрока заранее — тогда его надо передать в NewClaim явно"))
                .UserId.Value,
            player: null);

    /// <summary>
    /// То же, но игрок назван явно. Нужно операциям, которые узнают его только внутри фабрики:
    /// вторая роль оформляется на игрока исходной заявки, а не на того, кто выполняет операцию.
    /// </summary>
    /// <param name="claimStatus">Стартовый статус заявки.</param>
    /// <param name="playerAllowedSensitiveData">
    /// Разрешил ли <b>игрок</b> доступ к чувствительным данным.
    /// </param>
    /// <param name="player">
    /// Игрок, на которого оформляется заявка, — именно сущностью. Навигацию <c>Claim.Player</c>
    /// читает <c>FieldSaveHelper</c> у утверждённой заявки (имя персонажа по имени игрока), поэтому
    /// одного только идентификатора здесь недостаточно.
    /// </param>
    public Claim NewClaim(ClaimStatus claimStatus, bool playerAllowedSensitiveData, User player)
        => NewClaimCore(claimStatus, playerAllowedSensitiveData, player.UserId, player);

    private Claim NewClaimCore(
        ClaimStatus claimStatus,
        bool playerAllowedSensitiveData,
        int playerUserId,
        User? player)
    {
        // Обязан вернуть именно сущность User: она же кладётся в навигацию ResponsibleMasterUser.
#pragma warning disable CS0618 // Type or member is obsolete
        var responsibleMaster = Character.GetResponsibleMaster();
#pragma warning restore CS0618

        var claim = new Claim
        {
            CharacterId = Character.CharacterId,
            Character = Character,
            ProjectId = Character.ProjectId,
            // Не украшение: FieldSaveHelper.MarkUsed читает project.ProjectFields, и без этой
            // связки сохранение полей падает с NullReferenceException.
            Project = Character.Project,
            PlayerUserId = playerUserId,
            PlayerAcceptedDate = Now,
            CreateDate = Now,
            ClaimStatus = claimStatus,
            ResponsibleMasterUserId = responsibleMaster.UserId,
            ResponsibleMasterUser = responsibleMaster,
            LastUpdateDateTime = Now,
            PlayerAllowedSenstiveData = playerAllowedSensitiveData,
            CommentDiscussion = new CommentDiscussion
            {
                CommentDiscussionId = -1,
                ProjectId = Character.ProjectId,
            },
        };

        if (player is not null)
        {
            // Навигация ставится, только когда игрок известен сущностью: у утверждённой заявки её
            // читает FieldSaveHelper, а полагаться на relationship fixup EF было бы ненадёжно.
            claim.Player = player;
        }

        return claim;
    }

    /// <inheritdoc cref="CharacterOperationContext.SaveFieldsCore"/>
    /// <param name="claim">Заявка, через которую сохраняются поля.</param>
    /// <param name="fieldsToSet">Значения полей, заполненные при подаче.</param>
    public IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveFields(
        Claim claim,
        FieldLayerContainer fieldsToSet)
        => SaveFieldsCore(claim, fieldsToSet);

    /// <summary>Заказанные комментарии в порядке заказа.</summary>
    internal List<DeferredComment> DeferredComments { get; } = [];

    /// <summary>
    /// Заказывает комментарий к создаваемой заявке. Сам комментарий появится <b>после</b> первого
    /// сохранения — до него у дискуссии ещё нет настоящего идентификатора.
    /// </summary>
    public DeferredComment AddComment(
        string commentText,
        CommentExtraAction? extraAction,
        ClaimOperationType operationType)
        => AddComment(claim: null, commentText, extraAction, operationType);

    /// <summary>
    /// То же для соседней заявки, которую мутирует та же операция (старая заявка при выходе на
    /// вторую роль). Её дискуссия уже существует, но комментарий всё равно заказывается, а не
    /// создаётся на месте: порядок рассылки — часть контракта, и держит его одна очередь.
    /// </summary>
    public DeferredComment AddComment(
        Claim? claim,
        string commentText,
        CommentExtraAction? extraAction,
        ClaimOperationType operationType)
    {
        var deferred = new DeferredComment(commentText, extraAction, operationType, claim);
        DeferredComments.Add(deferred);
        return deferred;
    }
}

/// <summary>
/// Контекст создания заявки с типизированными аргументами операции (<see cref="Request"/>).
/// </summary>
internal sealed record ClaimCreationContext<TArgs>(
    Character Character,
    CharacterInfo CharacterInfo,
    ProjectInfo ProjectInfo,
    DateTime Now,
    ICurrentUserAccessor CurrentUser,
    User Initiator,
    UserInfo? Player,
    Action<object> AddEntity,
    Func<ClaimIdentification, Task<Claim>> LoadOtherClaimCore,
    FieldSaveHelper FieldSaveHelper,
    TArgs Request)
    : ClaimCreationContext(Character, CharacterInfo, ProjectInfo, Now, CurrentUser, Initiator, Player,
        AddEntity, LoadOtherClaimCore, FieldSaveHelper);
