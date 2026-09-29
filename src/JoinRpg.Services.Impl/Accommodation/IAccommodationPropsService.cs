using System.Runtime.CompilerServices;
using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl.Accommodation;

/// <summary>
/// Базовая единица изменения агрегата поселения — плана категории комнат (ADR018).
/// Централизует загрузку трекаемых сущностей вместе с согласованным доменным снимком
/// <see cref="RoomCategoryPlan"/>, проверку прав, проверку активности проекта, сохранение
/// и логирование операции с её аргументами.
/// </summary>
/// <remarks>
/// <para>
/// Корень агрегата — пул комнат (категория), а не тип проживания, не комната и не проект:
/// инварианты вместимости и «группа селится в комнату своего пула» выражаются только на нём
/// (ADR018, §1). Входа по <c>ProjectIdentification</c> нет намеренно — операции над всем
/// проектом перебирают категории и зовут сервис на каждую.
/// </para>
/// <para>
/// Метаданные проекта план не меняет, поэтому ни <c>Refresh</c>, ни <c>PrimeCache</c> здесь нет —
/// в отличие от <see cref="IProjectPropsService"/>. Это и был главный пункт возражения ADR015
/// против отдельного корня (ADR018, §6).
/// </para>
/// </remarks>
internal interface IAccommodationPropsService
{
    /// <summary>
    /// Изменяет план категории комнат и возвращает результат мутации.
    /// </summary>
    /// <typeparam name="TArgs">Тип аргументов операции; логируется вместе с именем операции.</typeparam>
    /// <typeparam name="TResult">Тип результата, возвращаемого <paramref name="action"/>.</typeparam>
    /// <param name="categoryId">Категория комнат, план которой будет изменён.</param>
    /// <param name="requiredPermission">Право, которым должен обладать текущий пользователь.</param>
    /// <param name="activeRequirement">Допустима ли операция над неактивным (архивным) проектом.</param>
    /// <param name="tracking">
    /// Нужны ли операции трекаемые группы жильцов. Управление комнатами их не меняет и обходится
    /// двумя запросами; заселение и выселение просят <see cref="RoomCategoryPlanTracking.WithGroups"/>
    /// и платят за это третьим запросом (ADR018, §10).
    /// </param>
    /// <param name="arguments">Аргументы операции; передаются в <paramref name="action"/> и логируются.</param>
    /// <param name="action">Мутация. Аргумент — контекст со снимком ДО изменения.</param>
    /// <param name="operationName">Имя операции для лога; по умолчанию — имя вызывающего метода.</param>
    Task<TResult> ChangePlan<TArgs, TResult>(
        RoomCategoryIdentification categoryId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        RoomCategoryPlanTracking tracking,
        TArgs arguments,
        Func<RoomCategoryPlanMutationContext<TArgs>, TResult> action,
        [CallerMemberName] string operationName = "");

    /// <summary>
    /// Изменяет план категории комнат.
    /// </summary>
    /// <inheritdoc cref="ChangePlan{TArgs, TResult}" path="/param"/>
    /// <typeparam name="TArgs">Тип аргументов операции; логируется вместе с именем операции.</typeparam>
    Task ChangePlan<TArgs>(
        RoomCategoryIdentification categoryId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        RoomCategoryPlanTracking tracking,
        TArgs arguments,
        Action<RoomCategoryPlanMutationContext<TArgs>> action,
        [CallerMemberName] string operationName = "");

    /// <summary>
    /// То же, но корень агрегата назван через комнату: пул в идентификаторе комнаты не закодирован,
    /// поэтому план ищется по принадлежности комнаты категории (ADR018, §10).
    /// </summary>
    /// <param name="roomId">Комната, которая будет изменена.</param>
    /// <inheritdoc cref="ChangePlan{TArgs, TResult}" path="/param[not(@name='categoryId')]"/>
    /// <typeparam name="TArgs">Тип аргументов операции; логируется вместе с именем операции.</typeparam>
    Task ChangePlanForRoom<TArgs>(
        AccommodationRoomIdentification roomId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        RoomCategoryPlanTracking tracking,
        TArgs arguments,
        Action<RoomCategoryPlanMutationContext<TArgs>> action,
        [CallerMemberName] string operationName = "");

    /// <summary>
    /// То же, но корень агрегата назван через группу жильцов: пул в идентификаторе группы не
    /// закодирован, поэтому план ищется по принадлежности группы категории (ADR018, §10).
    /// Трекаемые группы здесь загружаются всегда: назвать корень через группу может только
    /// операция, которая её и двигает, — поэтому параметра <c>tracking</c> у метода нет.
    /// </summary>
    /// <param name="groupId">Группа жильцов, которая будет изменена.</param>
    /// <inheritdoc cref="ChangePlan{TArgs, TResult}" path="/param[not(@name='categoryId') and not(@name='tracking')]"/>
    /// <typeparam name="TArgs">Тип аргументов операции; логируется вместе с именем операции.</typeparam>
    Task ChangePlanForGroup<TArgs>(
        AccommodationRequestIdentification groupId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Action<RoomCategoryPlanMutationContext<TArgs>> action,
        [CallerMemberName] string operationName = "");
}
