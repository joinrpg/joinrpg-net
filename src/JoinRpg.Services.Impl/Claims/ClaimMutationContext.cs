using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DataModel;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Impl.Characters;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl.Claims;

/// <summary>
/// Контекст изменения заявки. Мутация заявки — это мутация агрегата персонажа, дополнительно
/// называющая конкретную заявку (ADR014), поэтому контекст наследует
/// <see cref="CharacterMutationContext"/> и даёт доступ и к персонажу, и к его снимку.
/// </summary>
/// <param name="Claim">Трекаемая EF-сущность заявки; её и нужно мутировать.</param>
/// <param name="ClaimInfo">
/// Доменный снимок заявки строго ДО изменения — тот же экземпляр, что лежит в
/// <see cref="CharacterMutationContext.CharacterInfo"/>.<c>Claims</c>.
/// </param>
internal abstract record ClaimMutationContext(
    Claim Claim,
    CharacterClaimInfo ClaimInfo,
    Character Character,
    CharacterInfo CharacterInfo,
    ProjectInfo ProjectInfo,
    DateTime Now,
    ICurrentUserAccessor CurrentUser,
    User Initiator,
    IAggregateMutationScope Scope,
    FieldSaveHelper FieldSaveHelper,
    CommentHelper CommentHelper)
    : CharacterMutationContext(Character, CharacterInfo, ProjectInfo, Now, CurrentUser, Scope, FieldSaveHelper)
{
    /// <summary>
    /// Сюжеты, привязанные напрямую к персонажу, трекаемые тем же <c>DbContext</c>. Нужны созданию
    /// персонажа из слота.
    /// </summary>
    public Task<IReadOnlyCollection<PlotElement>> LoadDirectPlotsForCharacter(CharacterIdentification characterId)
        => Scope.LoadDirectPlotsForCharacter(characterId);

    /// <summary>
    /// Тип поселения проекта. Именованный загрузчик, а не <c>ProjectInfo</c>: типы поселения в снимок
    /// метаданных не входят.
    /// </summary>
    /// <exception cref="JoinRpgEntityNotFoundException">Тип поселения не найден в этом проекте.</exception>
    public Task<ProjectAccommodationType> LoadAccommodationType(AccommodationTypeIdentification accommodationTypeId)
        => Scope.LoadAccommodationType(accommodationTypeId);

    /// <summary>
    /// Приглашения к совместному проживанию, в которых участвует эта заявка, — трекаемые тем же
    /// <c>DbContext</c>, поэтому их изменение уедет в то же единственное сохранение.
    /// </summary>
    public Task<IReadOnlyCollection<AccommodationInvite>> LoadInvitesForClaim()
        => Scope.LoadInvitesForClaim(ClaimInfo.ClaimId);

    /// <summary>
    /// Сохраняет поля <b>через заявку</b>, а не через персонажа хэндла. Разница не косметическая:
    /// стратегия сохранения выбирается по <c>Claim.IsApproved</c>, а сам персонаж берётся из
    /// заявки — при утверждении заявки на слот она к этому моменту уже переехала на только что
    /// созданного персонажа, которого в хэндле нет.
    /// </summary>
    public new IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveFields(FieldLayerContainer fieldsToSet)
        => SaveFieldsCore(Claim, fieldsToSet);

    /// <summary>
    /// Явный выход за границу агрегата: другой персонаж того же проекта — трекаемая сущность вместе
    /// со своим доменным снимком. Операции над двумя персонажами (перенос, восстановление, вторая
    /// роль) пересекают границу только так, а не скрытой ленивой навигацией (ADR014).
    /// </summary>
    public Task<(Character Entity, CharacterInfo Info)> LoadOtherCharacter(CharacterIdentification characterId)
        => Scope.LoadOtherCharacter(characterId);

    /// <summary>
    /// Комментарии, созданные операцией, в порядке создания. Уведомления по ним сервис отправит
    /// после сохранения — см. <see cref="PendingComment"/>.
    /// </summary>
    internal List<PendingComment> PendingComments { get; } = [];

    /// <summary>
    /// Письма второго, легаси-канала (<c>LeaveRoomEmail</c> и подобные). Отправляются после всех
    /// уведомлений — как это происходило до миграции.
    /// </summary>
    internal List<Func<IEmailService, Task>> LegacyEmails { get; } = [];

    /// <summary>
    /// Добавляет комментарий к этой заявке и ставит уведомление по нему в очередь.
    /// </summary>
    public PendingComment AddComment(
        string commentText,
        CommentExtraAction? extraAction,
        ClaimOperationType operationType)
        => AddComment(Claim, commentText, extraAction, operationType);

    /// <summary>
    /// То же для другой заявки того же игрока — нужно автоотклонению при
    /// <c>StrictlyOneCharacter</c>.
    /// </summary>
    public PendingComment AddComment(
        Claim claim,
        string commentText,
        CommentExtraAction? extraAction,
        ClaimOperationType operationType)
    {
        var (comment, notification) = CommentHelper.CreateClaimCommentWithNotification(
            commentText, claim, ProjectInfo, extraAction, operationType, Now);

        var pending = new PendingComment(comment, notification);
        PendingComments.Add(pending);

        return pending;
    }

    /// <summary>
    /// Запрещает операции, датированные будущим.
    /// </summary>
    public void CheckOperationDate(DateTime operationDate)
        => OperationDateValidation.CheckOperationDate(operationDate, Now);

    /// <summary>
    /// Ставит письмо легаси-канала в очередь. Отправится после сохранения и после уведомлений.
    /// Передаётся отправителем, а не самим письмом: у <c>IEmailService</c> нет перегрузки по
    /// базовому типу, только по конкретным.
    /// </summary>
    public void AddLegacyEmail(Func<IEmailService, Task> send) => LegacyEmails.Add(send);

    /// <summary>
    /// Помечает персонажа изменённым, если заявка утверждена. Перенос
    /// <c>ClaimServiceImpl.MarkCharacterChangedIfApproved</c>.
    /// </summary>
    public void MarkCharacterChangedIfApproved()
    {
        if (Claim.ClaimStatus == ClaimStatus.Approved)
        {
            // Персонаж берётся из заявки, а не из хэндла: при утверждении заявки на слот заявка
            // к этому моменту уже переехала на только что созданного персонажа.
            this.MarkChanged(Claim.Character ?? Character);
        }
    }
}

/// <summary>
/// Контекст изменения заявки с типизированными аргументами операции (<see cref="Request"/>).
/// </summary>
internal sealed record ClaimMutationContext<TArgs>(
    Claim Claim,
    CharacterClaimInfo ClaimInfo,
    Character Character,
    CharacterInfo CharacterInfo,
    ProjectInfo ProjectInfo,
    DateTime Now,
    ICurrentUserAccessor CurrentUser,
    User Initiator,
    IAggregateMutationScope Scope,
    FieldSaveHelper FieldSaveHelper,
    CommentHelper CommentHelper,
    TArgs Request)
    : ClaimMutationContext(Claim, ClaimInfo, Character, CharacterInfo, ProjectInfo, Now, CurrentUser,
        Initiator, Scope, FieldSaveHelper, CommentHelper);
