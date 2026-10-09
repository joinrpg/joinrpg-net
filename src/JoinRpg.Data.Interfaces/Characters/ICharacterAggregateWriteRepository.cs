using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

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
}

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
    /// Группа проживающих (<c>AccommodationRequest</c>) указанной заявки — строка без связей, либо
    /// <c>null</c>, если заявка ещё не выбрала тип проживания.
    /// </summary>
    /// <remarks>
    /// Решения о группах принимаются по снимкам — <c>CharacterClaimInfo</c> и плану поселения
    /// (ADR022 §4), — поэтому потребителей в сервисах больше нет; загрузчик удаляется следующим
    /// шагом ADR022.
    /// </remarks>
    Task<AccommodationRequest?> LoadAccommodationGroupForClaim(ClaimIdentification claimId);

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
