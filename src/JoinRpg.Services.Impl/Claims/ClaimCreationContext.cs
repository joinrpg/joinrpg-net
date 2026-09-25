using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Impl.Characters;

namespace JoinRpg.Services.Impl.Claims;

/// <summary>
/// Контекст создания заявки (ADR014). Персонаж уже существует и трекается, а заявки ещё нет —
/// её строит фабрика операции через <see cref="NewClaim"/>.
/// </summary>
/// <param name="Character">Трекаемая EF-сущность персонажа, на которого подаётся заявка.</param>
/// <param name="CharacterInfo">Доменный снимок персонажа; по нему считаются правила подачи.</param>
/// <param name="Initiator">
/// Текущий пользователь как EF-сущность — только для легаси-канала писем.
/// </param>
/// <param name="Player">
/// Игрок, на которого оформляется заявка. При <see cref="ClaimOperation.AddByMaster"/> это
/// <b>не</b> тот, кто выполняет операцию.
/// </param>
/// <param name="AddEntity">Добавление сущности в тот же <c>DbContext</c>.</param>
internal abstract record ClaimCreationContext(
    Character Character,
    CharacterInfo CharacterInfo,
    ProjectInfo ProjectInfo,
    DateTime Now,
    ICurrentUserAccessor CurrentUser,
    User Initiator,
    UserInfo Player,
    Action<object> AddEntity,
    FieldSaveHelper FieldSaveHelper)
    : CharacterOperationContext(ProjectInfo, Now, CurrentUser, FieldSaveHelper)
{
    /// <summary>
    /// Единственная точка построения <see cref="Claim"/>: до ADR014 эта конструкция была скопирована
    /// в <c>AddClaimFromUser</c>, <c>AddClaimFromMaster</c> и <c>MoveToSecondRole</c>.
    /// </summary>
    /// <remarks>
    /// Заявку в <c>DbContext</c> добавляет сервис — после того, как фабрика её вернёт. Всё, что
    /// зависит от конкретной операции, фабрика проставляет уже на возвращённой заявке; так,
    /// <c>PlayerAllowedSenstiveData</c> остаётся <c>false</c>, пока игрок — и только игрок — не
    /// разрешит доступ: мастер такого разрешения от его имени дать не может.
    /// </remarks>
    /// <param name="claimStatus">Стартовый статус: подана игроком или предложена мастером.</param>
    /// <param name="responsibleMaster">
    /// Мастер, ответственный за новую заявку. Выбирает его операция: способы создания заявки тут
    /// вправе расходиться, поэтому общего значения по умолчанию нет. Обычное правило проекта —
    /// <see cref="ResponsibleMasterByProjectRules"/>.
    /// </param>
    public Claim NewClaim(ClaimStatus claimStatus, User responsibleMaster)
        => new Claim
        {
            CharacterId = Character.CharacterId,
            Character = Character,
            ProjectId = Character.ProjectId,
            // Не украшение: FieldSaveHelper.MarkUsed читает project.ProjectFields, и без этой
            // связки сохранение полей падает с NullReferenceException.
            Project = Character.Project,
            PlayerUserId = Player.UserId.Value,
            PlayerAcceptedDate = Now,
            CreateDate = Now,
            ClaimStatus = claimStatus,
            ResponsibleMasterUserId = responsibleMaster.UserId,
            ResponsibleMasterUser = responsibleMaster,
            LastUpdateDateTime = Now,
            CommentDiscussion = new CommentDiscussion
            {
                CommentDiscussionId = -1,
                ProjectId = Character.ProjectId,
            },
        };

    /// <summary>
    /// Ответственный мастер по обычному правилу проекта: мастер роли, а если его нет — мастер
    /// ближайшей группы. Отдельный метод, а не значение по умолчанию у <see cref="NewClaim"/>:
    /// выбор остаётся за операцией.
    /// </summary>
    /// <remarks>
    /// Возвращает именно EF-сущность <see cref="User"/> — она кладётся в навигацию
    /// <c>Claim.ResponsibleMasterUser</c>, и доменного снимка тут недостаточно.
    /// </remarks>
    public User ResponsibleMasterByProjectRules()
#pragma warning disable CS0618 // Правило живёт в legacy-расширении над EF-графом
        => Character.GetResponsibleMaster();
#pragma warning restore CS0618

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
    public void AddComment(
        string commentText,
        CommentExtraAction? extraAction,
        ClaimOperationType operationType)
        => DeferredComments.Add(new DeferredComment(commentText, extraAction, operationType));
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
    UserInfo Player,
    Action<object> AddEntity,
    FieldSaveHelper FieldSaveHelper,
    TArgs Request)
    : ClaimCreationContext(Character, CharacterInfo, ProjectInfo, Now, CurrentUser, Initiator, Player,
        AddEntity, FieldSaveHelper);
