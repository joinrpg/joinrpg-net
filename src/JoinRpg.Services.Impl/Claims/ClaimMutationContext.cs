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
    Action<object> AddEntity,
    Action<object> RemoveEntity,
    Func<CharacterIdentification, Task<(Character Entity, CharacterInfo Info)>> LoadOtherCharacterCore,
    FieldSaveHelper FieldSaveHelper,
    CommentHelper CommentHelper)
    : CharacterMutationContext(Character, CharacterInfo, ProjectInfo, Now, CurrentUser, AddEntity, RemoveEntity, FieldSaveHelper)
{
    /// <summary>
    /// Явный выход за границу агрегата: другой персонаж того же проекта — трекаемая сущность вместе
    /// со своим доменным снимком. Операции над двумя персонажами (перенос, восстановление, вторая
    /// роль) пересекают границу только так, а не скрытой ленивой навигацией (ADR014).
    /// </summary>
    public Task<(Character Entity, CharacterInfo Info)> LoadOtherCharacter(CharacterIdentification characterId)
        => LoadOtherCharacterCore(characterId);

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
            this.MarkChanged(Character);
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
    Action<object> AddEntity,
    Action<object> RemoveEntity,
    Func<CharacterIdentification, Task<(Character Entity, CharacterInfo Info)>> LoadOtherCharacterCore,
    FieldSaveHelper FieldSaveHelper,
    CommentHelper CommentHelper,
    TArgs Request)
    : ClaimMutationContext(Claim, ClaimInfo, Character, CharacterInfo, ProjectInfo, Now, CurrentUser,
        Initiator, AddEntity, RemoveEntity, LoadOtherCharacterCore, FieldSaveHelper, CommentHelper);
