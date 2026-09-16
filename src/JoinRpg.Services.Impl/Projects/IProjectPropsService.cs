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
    /// То же, что <see cref="ChangeProjectProperties{TArgs}(ProjectIdentification, Permission, ProjectActiveRequirement, TArgs, Action{ProjectMutationContext{TArgs}}, string)"/>,
    /// но мутация асинхронная.
    /// </summary>
    /// <remarks>
    /// Нужно операциям, которым по ходу мутации приходится догружать под-сущности проекта, не
    /// попадающие ни в <see cref="ProjectInfo"/>, ни в навигации <see cref="Project"/> — сегодня это
    /// поселение (<see cref="Data.Interfaces.IProjectAccommodationWriteAccess"/>). Догружать их
    /// заранее нельзя: они обязаны приехать из того же <c>DbContext</c>, что и хэндл, а он создаётся
    /// уже внутри операции. Произвольного ввода-вывода это не приглашает — в контексте доступны
    /// только именованные загрузчики того же хэндла (ср. ADR014 §6).
    /// </remarks>
    Task ChangeProjectPropertiesAsync<TArgs>(
        ProjectIdentification projectId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<ProjectMutationContext<TArgs>, Task> action,
        [CallerMemberName] string operationName = "");

    /// <summary>
    /// То же, что <see cref="ChangeProjectProperties{TArgs, TResult}(ProjectIdentification, Permission, ProjectActiveRequirement, TArgs, Func{ProjectMutationContext{TArgs}, TResult}, string)"/>,
    /// но мутация асинхронная.
    /// </summary>
    Task<TResult> ChangeProjectPropertiesAsync<TArgs, TResult>(
        ProjectIdentification projectId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<ProjectMutationContext<TArgs>, Task<TResult>> action,
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
