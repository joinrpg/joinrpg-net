namespace JoinRpg.Services.Interfaces.Projects;

/// <summary>
/// Приводит публичность групп проекта в соответствие правилу «у публичной группы есть публичный
/// путь наверх» (issue #4878).
/// </summary>
/// <remarks>
/// Нужен разовой починке данных (<c>HidePublicGroupsUnderPrivateJob</c>). Отдельным интерфейсом,
/// а не методом <see cref="ICharacterGroupService"/>: это не операция мастера над группой, а
/// обслуживание данных, и вызывать её из UI незачем.
/// </remarks>
[Obsolete("Разовая починка данных под #4878, удалить вместе с ней — см. #4974")]
public interface IPublicGroupVisibilityFixer
{
    /// <summary>
    /// Снимает публичность с групп проекта, у которых нет публичного пути наверх, и возвращает
    /// их названия. Если чинить нечего — пустой список.
    /// </summary>
    Task<IReadOnlyCollection<string>> HideGroupsWithoutPublicPath(ProjectIdentification projectId);
}
