using System.Runtime.CompilerServices;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters.Claims;
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
    /// <remarks>
    /// Отметку аудита персонажа (<c>UpdatedAt</c>/<c>UpdatedBy</c>) ставит сам сервис — операция
    /// здесь по определению меняет персонажа. У операций над заявкой такой автоматики нет и быть не
    /// может: комментарий или смена ответственного персонажа не трогают, поэтому там остаётся
    /// явный <c>ctx.MarkCharacterChangedIfApproved()</c> с его правилом.
    /// </remarks>
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
    /// Изменяет заявку асинхронной мутацией.
    /// </summary>
    /// <remarks>
    /// Асинхронная лямбда разрешена намеренно — в отличие от ADR009: единого снимка, из которого
    /// можно было бы всё поднять заранее, у заявки нет. Произвольный ввод-вывод при этом ограничен
    /// тем, что репозитории в контекст не пробрасываются — доступны только именованные
    /// <c>ctx.LoadOtherCharacter</c> / <c>ctx.LoadOtherClaim</c>, идущие через тот же
    /// <c>DbContext</c>.
    /// </remarks>
    /// <inheritdoc cref="ChangeClaim{TArgs}" path="/param"/>
    /// <typeparam name="TArgs">Тип аргументов операции; логируется вместе с именем операции.</typeparam>
    Task ChangeClaimAsync<TArgs>(
        ClaimIdentification claimId,
        ClaimAccessRequirement accessRequirement,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<ClaimMutationContext<TArgs>, Task> action,
        [CallerMemberName] string operationName = "");

    /// <summary>
    /// Изменяет заявку асинхронной мутацией и возвращает её результат.
    /// </summary>
    /// <inheritdoc cref="ChangeClaimAsync{TArgs}" path="/param"/>
    /// <typeparam name="TArgs">Тип аргументов операции; логируется вместе с именем операции.</typeparam>
    /// <typeparam name="TResult">Тип результата, возвращаемого <paramref name="action"/>.</typeparam>
    Task<TResult> ChangeClaimAsync<TArgs, TResult>(
        ClaimIdentification claimId,
        ClaimAccessRequirement accessRequirement,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<ClaimMutationContext<TArgs>, Task<TResult>> action,
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

    /// <summary>
    /// Создаёт заявку на персонажа: проверяет права и правила подачи, отдаёт построение заявки
    /// <paramref name="factory"/>, сохраняет <b>дважды</b> и рассылает уведомления.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Два сохранения — не небрежность, а зависимость по идентификатору:
    /// <c>CommentHelper.CreateClaimCommentWithNotification</c> кладёт комментарий в
    /// <c>claim.CommentDiscussion</c>, а до первого сохранения у дискуссии
    /// <c>CommentDiscussionId == -1</c>. Поэтому фабрика комментарий не создаёт, а
    /// <b>заказывает</b> через <c>ctx.AddComment(...)</c>; сервис материализует заказы между двумя
    /// сохранениями. До ADR014 эта связка была скопирована в оба метода создания заявки.
    /// </para>
    /// <para>
    /// Атомарности здесь нет — её нет и сегодня (см. «что сознательно не чиним» в ADR014).
    /// </para>
    /// </remarks>
    /// <param name="characterId">Персонаж, на которого подаётся заявка.</param>
    /// <param name="playerId">
    /// Игрок, на которого оформляется заявка. При <see cref="ClaimOperation.AddByMaster"/> это
    /// <b>не</b> текущий пользователь.
    /// </param>
    /// <param name="operation">
    /// <see cref="ClaimOperation.AddByPlayer"/> или <see cref="ClaimOperation.AddByMaster"/>: от
    /// этого зависят и проверка прав, и то, какие причины запрета мастер вправе обойти.
    /// </param>
    /// <param name="activeRequirement">Допустима ли операция над неактивным (архивным) проектом.</param>
    /// <param name="arguments">Аргументы операции; передаются в <paramref name="factory"/> и логируются.</param>
    /// <param name="factory">Строит заявку. Добавляет её в <c>DbContext</c> сам сервис.</param>
    /// <param name="operationName">Имя операции для лога; по умолчанию — имя вызывающего метода.</param>
    /// <returns>
    /// Созданная заявка. Идентификатор генерируется базой, поэтому читать его надо уже после
    /// <c>await</c>.
    /// </returns>
    Task<Claim> CreateClaim<TArgs>(
        CharacterIdentification characterId,
        UserIdentification playerId,
        ClaimOperation operation,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<ClaimCreationContext<TArgs>, Claim> factory,
        [CallerMemberName] string operationName = "");

    /// <summary>
    /// То же, но фабрика асинхронная и игрок может быть неизвестен заранее.
    /// </summary>
    /// <remarks>
    /// Обе поблажки нужны одной операции — выходу на вторую роль. Она не только создаёт заявку, но
    /// и мутирует исходную: та приезжает через <c>ctx.LoadOtherClaim</c> (это ввод-вывод, отсюда
    /// асинхронность), и только из неё известен игрок, на которого оформляется вторая роль.
    /// </remarks>
    /// <param name="characterId">Персонаж, на которого подаётся заявка.</param>
    /// <param name="playerId">
    /// Игрок, на которого оформляется заявка, либо <c>null</c>, если операция узнаёт его только
    /// внутри фабрики. <c>null</c> допустим <b>только</b> для операций без add-claim-валидации
    /// (<see cref="ClaimOperationExtensions.ValidatesClaimTarget"/>): правила подачи считаются для
    /// игрока, и без него считать их нечем.
    /// </param>
    /// <param name="operation">Операция: от неё зависят и проверка прав, и набор правил.</param>
    /// <param name="activeRequirement">Допустима ли операция над неактивным (архивным) проектом.</param>
    /// <param name="arguments">Аргументы операции; передаются в <paramref name="factory"/> и логируются.</param>
    /// <param name="factory">Строит заявку. Добавляет её в <c>DbContext</c> сам сервис.</param>
    /// <param name="operationName">Имя операции для лога; по умолчанию — имя вызывающего метода.</param>
    /// <typeparam name="TArgs">Тип аргументов операции; логируется вместе с именем операции.</typeparam>
    Task<Claim> CreateClaimAsync<TArgs>(
        CharacterIdentification characterId,
        UserIdentification? playerId,
        ClaimOperation operation,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<ClaimCreationContext<TArgs>, Task<Claim>> factory,
        [CallerMemberName] string operationName = "");
}
