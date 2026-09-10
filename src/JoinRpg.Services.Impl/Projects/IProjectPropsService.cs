using System.Runtime.CompilerServices;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Impl.Projects;

/// <summary>
/// Допустимость операции над неактивным (архивным) проектом. Задаётся явно в сигнатуре —
/// как правило операция над неактивным проектом недопустима.
/// </summary>
internal enum ProjectActiveRequirement
{
    /// <summary>Операция допустима только над активным проектом (обычный случай).</summary>
    MustBeActive,

    /// <summary>Операция явно допустима и над неактивным проектом (например, публикация/закрытие).</summary>
    AllowInactive,
}

/// <summary>
/// Базовая единица изменения метаданных проекта (того, что попадает в <see cref="ProjectInfo"/>).
/// Централизует загрузку, проверку прав, проверку активности, сохранение и поддержание
/// согласованности <see cref="Project"/>/<see cref="ProjectInfo"/> и кэша, а также логирование
/// операции с её аргументами.
/// </summary>
internal interface IProjectPropsService
{
    /// <summary>
    /// Изменяет метаданные проекта.
    /// </summary>
    /// <typeparam name="TArgs">Тип аргументов операции; логируется вместе с именем операции.</typeparam>
    /// <param name="projectId">Проект, метаданные которого меняются.</param>
    /// <param name="requiredPermission">Право, которым должен обладать текущий пользователь (админ — в обход).</param>
    /// <param name="activeRequirement">Допустима ли операция над неактивным проектом.</param>
    /// <param name="arguments">Аргументы операции; передаются в <paramref name="action"/> и логируются.</param>
    /// <param name="action">Мутация EF-сущности проекта. Второй параметр — снимок метаданных ДО изменения.</param>
    /// <param name="operationName">Имя операции для лога; по умолчанию — имя вызывающего метода.</param>
    Task ChangeProjectProperties<TArgs>(
        ProjectIdentification projectId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Action<ProjectMutationContext<TArgs>> action,
        [CallerMemberName] string operationName = "");

    /// <summary>
    /// Изменяет метаданные проекта и возвращает результат мутации.
    /// </summary>
    /// <typeparam name="TArgs">Тип аргументов операции; логируется вместе с именем операции.</typeparam>
    /// <typeparam name="TResult">Тип результата, возвращаемого <paramref name="action"/>.</typeparam>
    /// <param name="projectId">Проект, метаданные которого меняются.</param>
    /// <param name="requiredPermission">Право, которым должен обладать текущий пользователь (админ — в обход).</param>
    /// <param name="activeRequirement">Допустима ли операция над неактивным проектом.</param>
    /// <param name="arguments">Аргументы операции; передаются в <paramref name="action"/> и логируются.</param>
    /// <param name="action">Мутация EF-сущности проекта. Второй параметр — снимок метаданных ДО изменения.</param>
    /// <param name="operationName">Имя операции для лога; по умолчанию — имя вызывающего метода.</param>
    Task<TResult> ChangeProjectProperties<TArgs, TResult>(
        ProjectIdentification projectId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<ProjectMutationContext<TArgs>, TResult> action,
        [CallerMemberName] string operationName = "");

    /// <summary>
    /// Изменяет метаданные проекта как побочный эффект операции, права на которую вызывающий
    /// проверил сам (сейчас — отметка <c>WasEverUsed</c> при сохранении полей заявки или персонажа).
    /// Мастерские права здесь НЕ проверяются: отметку ставит тот, кто заполнил поле, — в том числе
    /// игрок в своей заявке, а мастерских прав у него нет. Всё остальное — как в
    /// <see cref="ChangeProjectProperties{TArgs}"/>: своя <c>SaveChanges</c>, пересборка
    /// <see cref="ProjectInfo"/>, обновление кэша и логирование.
    /// </summary>
    /// <remarks>
    /// Обратная сторона собственной <c>SaveChanges</c>: репозитории работают с тем же
    /// <c>DbContext</c>, что и вызывающий сценарий, поэтому вызов досрочно сбрасывает в БД и его
    /// незакоммиченные изменения. Поэтому вызывать только тогда, когда менять действительно есть
    /// что — см. <c>FieldSaveHelper.MarkAsUsed</c>.
    /// </remarks>
    /// <typeparam name="TArgs">Тип аргументов операции; логируется вместе с именем операции.</typeparam>
    /// <param name="projectId">Проект, метаданные которого меняются.</param>
    /// <param name="activeRequirement">Допустима ли операция над неактивным проектом.</param>
    /// <param name="arguments">Аргументы операции; передаются в <paramref name="action"/> и логируются.</param>
    /// <param name="action">Мутация EF-сущности проекта. Второй параметр — снимок метаданных ДО изменения.</param>
    /// <param name="operationName">Имя операции для лога; по умолчанию — имя вызывающего метода.</param>
    Task ChangeProjectPropertiesAsSideEffect<TArgs>(
        ProjectIdentification projectId,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Action<ProjectMutationContext<TArgs>> action,
        [CallerMemberName] string operationName = "");

    /// <summary>
    /// Создаёт новый проект: <paramref name="factory"/> строит EF-сущность <see cref="Project"/>
    /// (используя <see cref="ProjectCreationContext{TArgs}"/> для аудита), сервис добавляет её в БД
    /// и сохраняет. Единственная точка создания <see cref="Project"/>.
    /// </summary>
    Task<Project> CreateProject<TArgs>(
        TArgs arguments,
        Func<ProjectCreationContext<TArgs>, Project> factory,
        [CallerMemberName] string operationName = "");
}
