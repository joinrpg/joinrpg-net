using JoinRpg.Domain.CharacterFields;

namespace JoinRpg.Services.Impl.Characters;

/// <summary>
/// Определяет, отметила ли операция сохранения полей что-то как использованное впервые.
/// </summary>
internal static class CharacterFieldMarking
{
    /// <summary>
    /// Есть ли среди изменившихся полей такие, что до операции ещё не были отмечены.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Отметка монотонна (<c>false → true</c>), поэтому достаточно посмотреть на снимок метаданных:
    /// если всё уже отмечено — а это подавляющее большинство сохранений — метаданные не изменились
    /// и пересобирать <c>ProjectInfo</c> незачем.
    /// </para>
    /// <para>
    /// Читать надо именно снимок: <c>FieldSaveHelper</c> мутирует EF-сущности, а не
    /// <c>ProjectInfo</c>, поэтому снимок к моменту вызова ещё описывает состояние ДО операции.
    /// </para>
    /// </remarks>
    public static bool MarksNewUsage(IReadOnlyCollection<FieldWithPreviousAndNewValue> changedFields)
    {
        foreach (var changed in changedFields)
        {
            var field = changed.New.Field;

            if (!field.WasEverUsed)
            {
                return true;
            }

            if (field.HasValueList && changed.New.GetDropdownValues().Any(variant => !variant.WasEverUsed))
            {
                return true;
            }
        }

        return false;
    }
}
