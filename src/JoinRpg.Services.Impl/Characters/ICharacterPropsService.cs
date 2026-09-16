using System.Runtime.CompilerServices;
using JoinRpg.DataModel;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl.Characters;

/// <summary>
/// Базовая единица изменения агрегата персонажа (ADR014). Централизует загрузку трекаемых
/// сущностей вместе с согласованными доменными снимками, проверку прав, проверку активности
/// проекта, сохранение и логирование операции с её аргументами.
/// </summary>
/// <remarks>
/// <para>
/// Корень агрегата — персонаж, а не заявка: почти каждая операция над заявкой мутирует персонажа,
/// а перенос и вторая роль — сразу двух. Поэтому и <c>CharacterServiceImpl</c>, и будущие
/// claim-операции обслуживает один сервис.
/// </para>
/// <para>
/// Admin-bypass здесь <b>отсутствует</b>, в отличие от <see cref="IProjectPropsService"/>. Так
/// устроены claim- и character-пути сегодня (<c>Claim.RequestAccess</c> сводится к чистой проверке
/// ACL), и перенос прав идёт один в один. Добавить bypass означало бы выдать администраторам доступ
/// ко всем заявкам всех проектов.
/// </para>
/// </remarks>
internal interface ICharacterPropsService
{
    /// <summary>
    /// Изменяет персонажа.
    /// </summary>
    /// <typeparam name="TArgs">Тип аргументов операции; логируется вместе с именем операции.</typeparam>
    /// <param name="characterId">Персонаж, который будет изменён.</param>
    /// <param name="requiredPermission">Право, которым должен обладать текущий пользователь.</param>
    /// <param name="activeRequirement">Допустима ли операция над неактивным (архивным) проектом.</param>
    /// <param name="arguments">Аргументы операции; передаются в <paramref name="action"/> и логируются.</param>
    /// <param name="action">Мутация. Аргумент — контекст со снимками ДО изменения.</param>
    /// <param name="operationName">Имя операции для лога; по умолчанию — имя вызывающего метода.</param>
    Task ChangeCharacter<TArgs>(
        CharacterIdentification characterId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Action<CharacterMutationContext<TArgs>> action,
        [CallerMemberName] string operationName = "");

    /// <summary>
    /// Изменяет персонажа и возвращает результат мутации.
    /// </summary>
    /// <inheritdoc cref="ChangeCharacter{TArgs}" path="/param"/>
    /// <typeparam name="TArgs">Тип аргументов операции; логируется вместе с именем операции.</typeparam>
    /// <typeparam name="TResult">Тип результата, возвращаемого <paramref name="action"/>.</typeparam>
    Task<TResult> ChangeCharacter<TArgs, TResult>(
        CharacterIdentification characterId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<CharacterMutationContext<TArgs>, TResult> action,
        [CallerMemberName] string operationName = "");

    /// <summary>
    /// Изменяет заявку. Мутация заявки — это мутация агрегата персонажа, дополнительно называющая
    /// конкретную заявку, поэтому контекст даёт доступ и к персонажу, и к его снимку.
    /// </summary>
    /// <remarks>
    /// Комментарии, созданные внутри <paramref name="action"/>, и письма легаси-канала сервис
    /// отправляет сам — после <c>SaveChanges</c>, когда становится известен <c>CommentId</c>.
    /// </remarks>
    /// <param name="claimId">Заявка, которая будет изменена.</param>
    /// <param name="accessRequirement">Требуемый доступ к заявке.</param>
    /// <param name="activeRequirement">Допустима ли операция над неактивным (архивным) проектом.</param>
    /// <param name="arguments">Аргументы операции; передаются в <paramref name="action"/> и логируются.</param>
    /// <param name="action">Мутация. Аргумент — контекст со снимками ДО изменения.</param>
    /// <param name="operationName">Имя операции для лога; по умолчанию — имя вызывающего метода.</param>
    /// <typeparam name="TArgs">Тип аргументов операции; логируется вместе с именем операции.</typeparam>
    Task ChangeClaim<TArgs>(
        ClaimIdentification claimId,
        ClaimAccessRequirement accessRequirement,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Action<ClaimMutationContext<TArgs>> action,
        [CallerMemberName] string operationName = "");

    /// <summary>
    /// Изменяет заявку и возвращает результат мутации.
    /// </summary>
    /// <inheritdoc cref="ChangeClaim{TArgs}" path="/param"/>
    /// <typeparam name="TArgs">Тип аргументов операции; логируется вместе с именем операции.</typeparam>
    /// <typeparam name="TResult">Тип результата, возвращаемого <paramref name="action"/>.</typeparam>
    Task<TResult> ChangeClaim<TArgs, TResult>(
        ClaimIdentification claimId,
        ClaimAccessRequirement accessRequirement,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<ClaimMutationContext<TArgs>, TResult> action,
        [CallerMemberName] string operationName = "");

    /// <summary>
    /// Создаёт персонажа: <paramref name="factory"/> строит EF-сущность, сервис добавляет её в
    /// проект и сохраняет. Единственная точка создания <see cref="Character"/>.
    /// </summary>
    /// <remarks>
    /// Идентификатор генерируется базой при сохранении, поэтому читать его надо у возвращённой
    /// сущности — уже после <c>await</c>.
    /// </remarks>
    Task<Character> CreateCharacter<TArgs>(
        ProjectIdentification projectId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<CharacterCreationContext<TArgs>, Character> factory,
        [CallerMemberName] string operationName = "");
}
