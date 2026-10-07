namespace JoinRpg.WebPortal.Managers.ProjectMasterTools;

internal static class MasterProfileMapping
{
    /// <summary>Пустое поле «О себе» в форме — отсутствие описания, а не пустой markdown.</summary>
    public static MarkdownString? ToOptionalMarkdown(this string description)
        => string.IsNullOrWhiteSpace(description) ? null : new MarkdownString(description);
}
