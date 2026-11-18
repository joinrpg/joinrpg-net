namespace JoinRpg.Web.ProjectCommon;

public record CharacterGroupDto(
    CharacterGroupIdentification CharacterGroupId,
    string Name,
    string[] FullPath,
    bool IsPublic,
    bool IsSpecial,

    /// <summary>
    /// Корневая группа проекта. Пустой <see cref="FullPath"/> маркером корня быть не может: у прямых
    /// детей корня путь тоже пуст (генератор срезает имя корня из пути родителя).
    /// </summary>
    bool IsRoot = false)
{
}
