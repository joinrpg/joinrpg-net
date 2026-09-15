using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

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
/// Инициатор операции передаётся параметром, а не берётся из <c>ICurrentUserAccessor</c>:
/// слой доступа к данным не должен зависеть от текущего пользователя.
/// </para>
/// </remarks>
public interface ICharacterAggregateWriteRepository
{
    /// <summary>
    /// Загружает агрегат персонажа: трекаемую сущность вместе с согласованным доменным снимком.
    /// </summary>
    /// <param name="characterId">Персонаж, который будет изменён.</param>
    /// <param name="initiatorId">Пользователь, от имени которого идёт операция.</param>
    /// <exception cref="JoinRpgEntityNotFoundException">Персонаж не найден.</exception>
    Task<ICharacterAggregateUpdateHandle> LoadCharacterForUpdate(
        CharacterIdentification characterId,
        UserIdentification initiatorId);

    /// <summary>
    /// Загружает агрегат персонажа, которому принадлежит заявка, и саму заявку.
    /// </summary>
    /// <param name="claimId">Заявка, которая будет изменена.</param>
    /// <param name="initiatorId">Пользователь, от имени которого идёт операция.</param>
    /// <exception cref="JoinRpgEntityNotFoundException">Заявка или её персонаж не найдены.</exception>
    Task<IClaimUpdateHandle> LoadClaimForUpdate(
        ClaimIdentification claimId,
        UserIdentification initiatorId);
}

/// <summary>
/// Согласованная четвёрка: трекаемый <see cref="Character"/>, <see cref="ProjectInfo"/>,
/// <see cref="CharacterInfo"/> и <see cref="User"/>-инициатор (ADR014).
/// </summary>
/// <remarks>
/// Хэндл не отдаёт <c>DbSet&lt;T&gt;</c> наружу — только <see cref="Add"/> и <see cref="Remove"/>.
/// Это и не даёт вызывающему уйти в другой <c>DbContext</c>, и позволяет подделать хэндл
/// в юнит-тестах, не поднимая EF.
/// </remarks>
public interface ICharacterAggregateUpdateHandle
{
    /// <summary>Трекаемая EF-сущность проекта. Согласована с <see cref="ProjectInfo"/>.</summary>
    Project Project { get; }

    /// <summary>
    /// Снимок метаданных проекта. Тот самый экземпляр, на который ссылается
    /// <see cref="CharacterInfo"/> — это проверяется в конструкторе агрегата (ADR013).
    /// Обновляется только вызовом <see cref="RefreshProjectInfo"/>.
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

    /// <summary>
    /// Текущий пользователь как EF-сущность. Существует только ради легаси-канала писем:
    /// <c>EmailModelBase.Initiator</c> требует именно сущность <see cref="User"/>.
    /// </summary>
    [Obsolete("Нужен только легаси-письмам (EmailModelBase.Initiator). Новый код должен обходиться "
        + "UserIdentification/UserInfoHeader; свойство уйдёт вместе с легаси-каналом писем, см. ADR014")]
    User Initiator { get; }

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
    /// Перечитывает проект из БД (тем же <c>DbContext</c>, значит — в той же транзакции)
    /// и пересобирает из него <see cref="ProjectInfo"/>. Нужен операциям, которые меняют
    /// метаданные проекта, — сегодня это только <c>ProjectField.WasEverUsed</c>.
    /// </summary>
    Task<ProjectInfo> RefreshProjectInfo();

    /// <summary>
    /// Явный выход за границу агрегата: трекаемая заявка другого персонажа того же проекта.
    /// </summary>
    /// <exception cref="JoinRpgEntityNotFoundException">Заявка не найдена.</exception>
    Task<Claim> LoadOtherClaim(ClaimIdentification claimId);

    /// <summary>
    /// Явный выход за границу агрегата: другой персонаж того же проекта — трекаемая сущность
    /// и его доменный снимок, разделяющий <see cref="ProjectInfo"/> этого хэндла.
    /// </summary>
    /// <exception cref="JoinRpgEntityNotFoundException">Персонаж не найден.</exception>
    Task<(Character Entity, CharacterInfo Info)> LoadOtherCharacter(CharacterIdentification characterId);
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
    /// Доменный снимок заявки строго <b>ДО</b> изменения. Это тот же экземпляр, что лежит
    /// в <see cref="ICharacterAggregateUpdateHandle.CharacterInfo"/>.<c>Claims</c>.
    /// </summary>
    CharacterClaimInfo ClaimInfo { get; }
}
