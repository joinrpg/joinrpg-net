namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>
/// Markdown-разметка как бизнес-значение. Для хранения в БД используется
/// <c>JoinRpg.DataModel.MarkdownDbValue</c> (между ними есть неявные конверсии).
/// </summary>
[TypedStringValue(MinLength = 0, Trim = false)]
public sealed partial record MarkdownString(string Value);
