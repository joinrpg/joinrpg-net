namespace JoinRpg.DomainTypes.ProjectMetadata;

public static class CharacterNameFieldCandidates
{
    private static readonly string[] NamePrefixes = ["Имя", "Название мероприятия"];

    /// <summary>
    /// Поля, похожие на имя персонажа, когда поле-имя в проекте не выбрано и имя берётся из имени игрока.
    /// </summary>
    /// <remarks>
    /// Так бывает после восстановления проекта: поля восстановили руками, а выбрать поле-имя в настройках
    /// забыли — и все персонажи получают имена игроков. Кандидаты — те поля, которые можно выбрать
    /// полем-именем в настройках: активные строковые поля персонажа.
    /// </remarks>
    public static IReadOnlyList<ProjectFieldInfo> GetUnboundCharacterNameFieldCandidates(this ProjectInfo projectInfo)
    {
        if (projectInfo.CharacterNameField is not null)
        {
            return [];
        }

        return [.. projectInfo.SortedActiveFields.Where(f =>
            f.Type == ProjectFieldType.String
            && f.BoundTo == FieldBoundTo.Character
            && NamePrefixes.Any(prefix => f.Name.Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))];
    }
}
