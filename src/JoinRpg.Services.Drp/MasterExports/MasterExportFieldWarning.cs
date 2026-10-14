namespace JoinRpg.Services.Drp.MasterExports;

/// <summary>
/// В чём сопоставление выгрузки мастера с полями проекта не уверено — мастер решает сам.
/// </summary>
public enum MasterExportFieldWarning
{
    /// <summary>Колонка не похожа ни на одно поле проекта (обычно это служебная колонка выгрузки).</summary>
    ColumnNotMapped,

    /// <summary>Под заголовок подходит несколько полей проекта с одинаковым именем.</summary>
    ColumnAmbiguous,

    /// <summary>Заголовок совпал с именем поля только без учёта регистра.</summary>
    ColumnCaseMismatch,

    /// <summary>Такой заголовок в выгрузке встречается несколько раз — непонятно, какая колонка от поля.</summary>
    ColumnDuplicated,

    /// <summary>Поле выключено (удалено) в проекте.</summary>
    FieldInactive,

    /// <summary>Поле не может иметь значения (например, заголовок раздела анкеты).</summary>
    FieldCannotHaveValue,

    /// <summary>Ячейка обрезана при выгрузке — значение неполное.</summary>
    Truncated,

    /// <summary>Часть текста не совпала ни с одной подписью варианта — она пропущена.</summary>
    VariantNotFound,

    /// <summary>Текст раскладывается на подписи вариантов несколькими способами или подпись есть у нескольких вариантов.</summary>
    VariantAmbiguous,

    /// <summary>
    /// Выбран выключенный вариант. Доменная проверка не даёт добавить выключенный вариант
    /// в значение, где его ещё не было.
    /// </summary>
    VariantInactive,

    /// <summary>У выпадающего списка (одиночный выбор) распознано несколько вариантов.</summary>
    MultipleVariantsForSingleChoice,

    /// <summary>
    /// В значении поля-ссылки на пользователя не идентификаторы (они пропущены) или несколько разных
    /// пользователей в поле на одного (значение не восстанавливается).
    /// </summary>
    InvalidUserIds,

    /// <summary>Значение чекбокса не похоже ни на «☑️», ни на «☐».</summary>
    InvalidCheckboxValue,
}
