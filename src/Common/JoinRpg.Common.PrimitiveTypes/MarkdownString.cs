namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>
/// Markdown-разметка как бизнес-значение. Для хранения в БД используется
/// <c>JoinRpg.DataModel.MarkdownDbValue</c> (между ними есть неявные конверсии).
/// </summary>
// Markdown длинный по природе (описание персонажа, сюжет, комментарий), поэтому
// ограничение длины снято явно: дефолт в 999 символов сюда попал случайно.
[TypedStringValue(MinLength = 0, MaxLength = int.MaxValue, Trim = false)]
public sealed partial record MarkdownString(string Value);
