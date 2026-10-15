using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Data.Interfaces.Characters;

/// <summary>
/// Репозиторий для изменения агрегата персонажа (ADR014). Гарантирует согласованность трекаемых
/// EF-сущностей (<see cref="Character"/>, <see cref="Claim"/>) и доменных снимков
/// (<see cref="ProjectInfo"/>, <see cref="CharacterInfo"/>).
/// </summary>
/// <remarks>
/// <para>
/// Берётся <b>только</b> из <c>IUnitOfWork</c>, а не из DI: <c>MyDbContext</c> зарегистрирован
/// транзиентом, поэтому DI-экземпляр получил бы другой <c>DbContext</c> — мутация трекалась бы
/// в одном, а <c>SaveChanges</c> шёл бы в другом.
/// </para>
/// <para>
/// Инициатора операции репозиторий не знает и не спрашивает: единственным потребителем была
/// EF-сущность <c>User</c> для легаси-канала писем, а он удалён. Кто совершает операцию, знает слой
/// сервисов — через <c>ICurrentUserAccessor</c>.
/// </para>
/// </remarks>
public interface ICharacterAggregateWriteRepository
{
    /// <summary>
    /// Загружает агрегат персонажа: трекаемую сущность вместе с согласованным доменным снимком.
    /// </summary>
    /// <param name="characterId">Персонаж, который будет изменён.</param>
    /// <exception cref="JoinRpgEntityNotFoundException">Персонаж не найден.</exception>
    Task<ICharacterAggregateUpdateHandle> LoadCharacterForUpdate(CharacterIdentification characterId);

    /// <summary>
    /// Загружает агрегат персонажа, которому принадлежит заявка, и саму заявку.
    /// </summary>
    /// <param name="claimId">Заявка, которая будет изменена.</param>
    /// <exception cref="JoinRpgEntityNotFoundException">Заявка или её персонаж не найдены.</exception>
    Task<IClaimUpdateHandle> LoadClaimForUpdate(ClaimIdentification claimId);

    /// <summary>
    /// Вставляет «оболочку» заявки с заданным идентификатором — для восстановления потерянной
    /// заявки по уведомлениям (DRP, #5481): на её прежний id ссылаются уведомления, письма и выгрузки,
    /// а EF6 значение IDENTITY-колонки не вставляет. Оболочка — заявка в статусе «предложена» со своим
    /// обсуждением и без полей; наполняет её дальше обычная операция над заявкой.
    /// </summary>
    /// <remarks>
    /// Выполняется сразу, своей транзакцией, мимо <c>SaveChanges</c>. Значение ниже текущего счётчика
    /// IDENTITY его не двигает, поэтому параллельно создаваемым заявкам не мешает.
    /// </remarks>
    /// <returns><c>false</c>, если заявка с таким идентификатором уже есть (в любом проекте).</returns>
    Task<bool> InsertRestoredClaimShell(RestoredClaimShell shell);

    /// <summary>
    /// Занят ли идентификатор заявки в <b>любом</b> проекте — идентификаторы заявок общие на всю базу.
    /// </summary>
    Task<bool> IsClaimIdTaken(int claimId);

    /// <summary>
    /// Есть ли заявка в своём проекте — лёгкая проверка без загрузки агрегата, чтобы отказать до
    /// первой записи мимо <c>SaveChanges</c>.
    /// </summary>
    Task<bool> ClaimExists(ClaimIdentification claimId);

    /// <summary>
    /// Вставляет «оболочку» персонажа с заданным идентификатором — для восстановления потерянного
    /// персонажа из выгрузки мастера (DRP, #5529). Оболочка — то, что дало бы доменное создание
    /// персонажа без полей: активный скрытый (<c>CharacterVisibility.Private</c>) персонаж игрока
    /// (<c>CharacterType.Player</c>) в корневой группе, без заявок, с пустым набором полей; порядок
    /// персонажей в корневой группе не меняется. Наполняет её дальше обычная операция над персонажем.
    /// </summary>
    /// <remarks>
    /// Выполняется сразу, своей транзакцией, мимо <c>SaveChanges</c>. Вставляется только id ниже
    /// текущего счётчика IDENTITY — такая вставка его не двигает.
    /// </remarks>
    Task<RestoredShellInsertResult> InsertRestoredCharacterShell(RestoredCharacterShell shell);

    /// <summary>
    /// Вставляет восстановленный комментарий к заявке — для восстановления переписки по уведомлениям
    /// (DRP, #5492). Строки — те же, что пишет доменное добавление комментария, но с датой и автором из
    /// уведомления и, если он известен, с прежним идентификатором: на него ссылаются уведомления и
    /// письма. Заявку (её отметки о последних комментариях, дату обновления) не трогает — это дело
    /// операции над заявкой.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Выполняется сразу, своей транзакцией, мимо <c>SaveChanges</c>. Прежний идентификатор вставляется
    /// только ниже текущего счётчика IDENTITY — такая вставка его не двигает. Без идентификатора повтор
    /// узнаётся по автору, времени (с точностью колонки <c>datetime</c>) и тексту в обсуждении заявки.
    /// </para>
    /// </remarks>
    /// <exception cref="JoinRpgEntityNotFoundException">Заявки нет в указанном проекте; ничего не вставлено.</exception>
    Task<RestoredCommentInsertResult> InsertRestoredClaimComment(RestoredClaimCommentRow comment);

    /// <summary>
    /// Вставляет восстановленную ручную финансовую операцию (взнос или возврат) — для восстановления
    /// по уведомлениям (DRP, #5561). Строки — те же, что пишет доменный приём взноса (<c>AcceptFee</c>):
    /// комментарий, его текст и финансовая операция с тем же идентификатором, но с датами и автором из
    /// уведомления и, если он известен, с прежним идентификатором. Заявку (взнос, отметки, дату
    /// обновления) не трогает — это дело операции над заявкой.
    /// </summary>
    /// <remarks>
    /// Выполняется сразу, одной своей транзакцией, мимо <c>SaveChanges</c>. Прежний идентификатор
    /// вставляется только ниже текущего счётчика IDENTITY комментариев — такая вставка его не двигает.
    /// Без идентификатора повтор узнаётся по операции в обсуждении заявки: тот же автор, тот же вид
    /// операции и сумма, время — в пределах минуты.
    /// </remarks>
    /// <returns>
    /// Исход и идентификатор: вставленной или уже бывшей операции, а при конфликте — запрошенный.
    /// </returns>
    /// <exception cref="JoinRpgEntityNotFoundException">Заявки нет в указанном проекте; ничего не вставлено.</exception>
    Task<RestoredFinanceOperationInsert> InsertRestoredFinanceOperation(RestoredFinanceOperationRow operation);
}

/// <summary>
/// Строка восстановленного комментария к заявке.
/// </summary>
/// <param name="CommentId">Прежний идентификатор либо <c>null</c> — тогда комментарий получит новый.</param>
/// <param name="CreatedAt">Время создания (UTC); им же ставится время правки.</param>
/// <param name="ParentCommentId">
/// Родительский комментарий того же обсуждения — у решения по финансовой операции это сама операция.
/// </param>
public record RestoredClaimCommentRow(
    ClaimIdentification ClaimId,
    int? CommentId,
    UserIdentification AuthorId,
    bool IsCommentByPlayer,
    bool IsVisibleToPlayer,
    CommentExtraAction? ExtraAction,
    DateTime CreatedAt,
    string Text,
    int? ParentCommentId = null);

/// <summary>
/// Строка восстановленной ручной финансовой операции. Комментарий операции видим игроку, его действие —
/// по виду операции (<c>PaidFee</c>/<c>RefundFee</c>), как у доменного приёма взноса.
/// </summary>
/// <param name="OperationId">
/// Прежний идентификатор (он же идентификатор комментария) либо <c>null</c> — тогда операция получит новый.
/// </param>
/// <param name="OperationType">Только <c>Submit</c> или <c>Refund</c>.</param>
/// <param name="MoneyAmount">Со знаком: у возврата — отрицательная.</param>
/// <param name="CreatedAt">Время создания (UTC) — комментария и операции.</param>
/// <param name="ChangedAt">Время последнего изменения операции (UTC) — решения по ней или создания.</param>
/// <param name="OperationDate">Дата операции — полночь без часового пояса, как её пишет форма приёма взноса.</param>
public record RestoredFinanceOperationRow(
    ClaimIdentification ClaimId,
    int? OperationId,
    UserIdentification AuthorId,
    bool IsCommentByPlayer,
    FinanceOperationType OperationType,
    int MoneyAmount,
    PaymentTypeIdentification PaymentTypeId,
    FinanceOperationState State,
    DateTime CreatedAt,
    DateTime ChangedAt,
    DateTime OperationDate,
    string Text);

/// <summary>Исход вставки восстановленной финансовой операции.</summary>
/// <param name="OperationId">
/// Идентификатор вставленной или уже бывшей операции; при конфликте — запрошенный.
/// </param>
public record RestoredFinanceOperationInsert(RestoredCommentInsertResult Result, int OperationId);

/// <summary>
/// Минимум, без которого строки персонажа нет.
/// </summary>
/// <param name="RootGroupId">Корневая группа проекта — единственная группа оболочки.</param>
/// <param name="CharacterName">Имя персонажа (как его записало бы сохранение полей).</param>
/// <param name="Description">
/// Описание: <c>null</c> — не задано (в проекте нет поля описания), пустая строка — пустое описание,
/// как его пишет сохранение полей при настроенном поле описания.
/// </param>
/// <param name="CreatedAt">Время создания и изменения (UTC) — восстановленный персонаж поднимает дату обновления проекта.</param>
public record RestoredCharacterShell(
    CharacterIdentification CharacterId,
    CharacterGroupIdentification RootGroupId,
    string CharacterName,
    string? Description,
    UserIdentification CreatedBy,
    DateTime CreatedAt);

/// <summary>
/// Минимум, без которого строки заявки нет: персонаж, игрок, ответственный мастер и дата подачи.
/// </summary>
public record RestoredClaimShell(
    ClaimIdentification ClaimId,
    CharacterIdentification CharacterId,
    UserIdentification PlayerId,
    UserIdentification ResponsibleMasterId,
    DateTime CreatedAt);

/// <summary>
/// Согласованная тройка: трекаемый <see cref="Character"/>, <see cref="ProjectInfo"/> и
/// <see cref="CharacterInfo"/> (ADR014).
/// </summary>
/// <remarks>
/// Хэндл не отдаёт <c>DbSet&lt;T&gt;</c> наружу — только <see cref="Add"/> и <see cref="Remove"/>.
/// Это и не даёт вызывающему уйти в другой <c>DbContext</c>, и позволяет подделать хэндл
/// в юнит-тестах, не поднимая EF.
/// </remarks>
/// <summary>
/// Что операции разрешено делать с БД внутри мутации: добавить и удалить сущность, догрузить
/// соседей агрегата. Всё — через тот же <c>DbContext</c>, что и последующий <c>SaveChanges</c>.
/// </summary>
/// <remarks>
/// Единый интерфейс вместо делегата на каждую догрузку: делегаты пришлось бы протаскивать
/// параметрами через оба контекста — и абстрактный, и типизированный, — а их число растёт с каждой
/// мигрированной операцией. Доступ при этом не расширяется: произвольного репозитория здесь нет,
/// только именованные догрузки, и подделать интерфейс в тестах не сложнее делегатов.
/// </remarks>
public interface IAggregateMutationScope
{
    /// <summary>
    /// Добавляет новую сущность в тот же <c>DbContext</c>, через который потом идёт
    /// <c>SaveChanges</c>.
    /// </summary>
    void Add(object entity);

    /// <summary>
    /// Окончательно удаляет сущность из того же <c>DbContext</c>, через который потом идёт
    /// <c>SaveChanges</c>.
    /// </summary>
    void Remove(object entity);

    /// <summary>
    /// Явный выход за границу агрегата: трекаемая заявка другого персонажа того же проекта.
    /// </summary>
    /// <exception cref="JoinRpgEntityNotFoundException">Заявка не найдена.</exception>
    Task<Claim> LoadOtherClaim(ClaimIdentification claimId);

    /// <summary>
    /// Явный выход за границу агрегата: другой персонаж того же проекта — трекаемая сущность
    /// и его доменный снимок, разделяющий <c>ProjectInfo</c> хэндла.
    /// </summary>
    /// <exception cref="JoinRpgEntityNotFoundException">Персонаж не найден.</exception>
    Task<(Character Entity, CharacterInfo Info)> LoadOtherCharacter(CharacterIdentification characterId);

    /// <summary>
    /// Сюжеты, привязанные напрямую к персонажу, — трекаемые тем же <c>DbContext</c>. Нужны
    /// созданию персонажа из слота: новый персонаж наследует привязки слота, а мутировать
    /// <c>PlotElement.TargetCharacters</c> можно только в том контексте, через который идёт
    /// <c>SaveChanges</c> (ADR014).
    /// </summary>
    Task<IReadOnlyCollection<PlotElement>> LoadDirectPlotsForCharacter(CharacterIdentification characterId);

    /// <summary>
    /// Приглашения к совместному проживанию, в которых участвует заявка, — и отправленные ею, и
    /// полученные. Трекаются тем же <c>DbContext</c>, поэтому их отклонение попадёт в то же
    /// единственное сохранение, что и остальная мутация (ADR014).
    /// </summary>
    Task<IReadOnlyCollection<AccommodationInvite>> LoadInvitesForClaim(ClaimIdentification claimId);

    /// <summary>
    /// Группа проживающих по её идентификатору — строка без связей, либо <c>null</c>, если такой
    /// группы в этом проекте нет.
    /// </summary>
    /// <remarks>
    /// Решения по ней не принимаются (ADR022 §4): приглашение берёт отсюда только существование и
    /// тип группы, которой нет в плане приглашающего, а решает по плану её типа.
    /// </remarks>
    Task<AccommodationRequest?> LoadAccommodationGroup(AccommodationRequestIdentification groupId);

    /// <summary>
    /// План поселения категории, из которой селится тип проживания <paramref name="typeId"/>, —
    /// доменный снимок групп и комнат, по которому операция принимает решения (ADR022 §4).
    /// </summary>
    /// <remarks>
    /// План строится на <c>ProjectInfo</c> хэндла, а не на экземпляре из
    /// <c>IProjectMetadataRepository</c>: конструктор плана требует, чтобы типы проживания были
    /// экземплярами из того же <c>ProjectInfo</c> (ADR013). План не трекается — это снимок
    /// строго ДО изменения, мутация после него его не обновляет.
    /// </remarks>
    /// <exception cref="AccommodationTypeNotFoundException">Типа нет в этом проекте.</exception>
    Task<RoomCategoryPlan> LoadRoomCategoryPlan(AccommodationTypeIdentification typeId);

    /// <summary>
    /// Приглашение к совместному проживанию по идентификатору — трекаемое, вместе с заявками обеих
    /// сторон.
    /// </summary>
    /// <remarks>
    /// Фильтр по проекту здесь существенный: идентификатор приглашения приходит параметром
    /// операции, а проект в нём только объявленный.
    /// </remarks>
    /// <exception cref="JoinRpgEntityNotFoundException">Приглашения в этом проекте нет.</exception>
    Task<AccommodationInvite> LoadInvite(AccommodationInviteIdentification inviteId);
}

public interface ICharacterAggregateUpdateHandle : IAggregateMutationScope
{
    /// <summary>Трекаемая EF-сущность проекта. Согласована с <see cref="ProjectInfo"/>.</summary>
    Project Project { get; }

    /// <summary>
    /// Снимок метаданных проекта. Тот самый экземпляр, на который ссылается
    /// <see cref="CharacterInfo"/> — это проверяется в конструкторе агрегата (ADR013).
    /// Операции над агрегатом метаданных проекта не меняют, поэтому снимок не обновляется.
    /// </summary>
    ProjectInfo ProjectInfo { get; }

    /// <summary>Трекаемая EF-сущность персонажа; именно её нужно мутировать.</summary>
    Character Character { get; }

    /// <summary>
    /// Доменный снимок персонажа строго <b>ДО</b> изменения. После мутации
    /// <see cref="Character"/> он <b>не обновляется</b> — это осознанно: у
    /// <see cref="CharacterInfo"/> нет кеша (ADR013), значит нечему и устаревать, а снимок ПОСЛЕ
    /// строится доменными методами (<c>ForNewCharacter</c>/<c>WithXxx</c>), а не перечитыванием БД.
    /// </summary>
    CharacterInfo CharacterInfo { get; }
}

/// <summary>
/// Хэндл агрегата персонажа, дополнительно называющий конкретную заявку (ADR014): мутация заявки
/// есть мутация character-агрегата.
/// </summary>
public interface IClaimUpdateHandle : ICharacterAggregateUpdateHandle
{
    /// <summary>Трекаемая EF-сущность заявки; именно её нужно мутировать.</summary>
    Claim Claim { get; }

    /// <summary>
    /// Доменный снимок заявки в составе персонажа строго <b>ДО</b> изменения (ADR021). Персонаж в
    /// нём — тот же экземпляр, что <see cref="ICharacterAggregateUpdateHandle.CharacterInfo"/>, а
    /// заявка — тот же экземпляр, что лежит в его <c>Claims</c>.
    /// </summary>
    ClaimInCharacter ClaimSnapshot { get; }

    /// <summary>Доменный снимок самой заявки — сокращение для <c>ClaimSnapshot.Claim</c>.</summary>
    CharacterClaimInfo CharacterClaimInfo => ClaimSnapshot.Claim;
}
