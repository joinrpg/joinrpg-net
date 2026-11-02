namespace JoinRpg.Web.ProjectCommon;

/// <summary>
/// Как <see cref="CharacterGroupSelector"/> обращается с корневой группой и пустым пунктом «все».
/// </summary>
public enum CharacterGroupSelectorMode
{
    /// <summary>
    /// По умолчанию: корень — легальный отдельный выбор, пустого пункта нет
    /// (форма правил, выбор группы в редакторе).
    /// </summary>
    ShowRoot,

    /// <summary>
    /// Пустой пункт «<see cref="CharacterGroupSelector.EmptyText"/>» плюс корень как отдельный выбор:
    /// они формально различны (сетке ролей корень включает спецгруппы, пусто — нет).
    /// </summary>
    ShowEmptyAndRoot,

    /// <summary>
    /// Пустой пункт есть, корня в списке нет: выбор корня означал бы то же, что «без фильтра»,
    /// поэтому корень ≡ пусто (фильтры списков). При <c>Multiple</c> — только скрытие корня,
    /// пустого пункта нет (контракт <c>TypedSelector</c>: там пусто — это ничего не отмечено).
    /// </summary>
    RootMeansEmpty,
}
