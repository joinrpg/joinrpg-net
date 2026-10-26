using JoinRpg.Web.ProjectCommon.Projects;

namespace JoinRpg.Web.Models;

public static class ProjectLinkViewModelBuilder
{
    public static IEnumerable<ProjectLinkViewModel> ToLinkViewModels(this IEnumerable<ProjectPersonalizedInfo> projects)
        => projects.OrderByDisplayPriority().Select(p => new ProjectLinkViewModel(p.ProjectId, p.ProjectName));
}

public class EditProjectViewModel
{
    public int ProjectId { get; set; }

    [ReadOnly(true)]
    public string OriginalName { get; set; }

    public bool Active { get; set; }

    /// <summary>Заблокирован на время восстановления данных (ADR023).</summary>
    public bool Blocked { get; set; }
}

public class UnblockProjectViewModel : IValidatableObject
{
    /// <summary>Фраза, которую надо ввести, чтобы подтвердить разблокировку.</summary>
    public const string ConfirmationPhrase = "все восстановлено";

    public int ProjectId { get; set; }

    [ReadOnly(true)]
    public string OriginalName { get; set; } = "";

    [ReadOnly(true)]
    public bool IsBlocked { get; set; }

    [Display(Name = "Подтверждение", Description = "Введите «" + ConfirmationPhrase + "», чтобы подтвердить, что все нужные данные восстановлены.")]
    public string? Confirmation { get; set; }

    /// <summary>
    /// Терпимое сравнение с <see cref="ConfirmationPhrase"/>: без пробелов по краям, без учёта регистра, «ё» = «е».
    /// </summary>
    public static bool IsConfirmationPhraseValid(string? confirmation)
    {
        if (confirmation is null)
        {
            return false;
        }
        var normalized = confirmation.Trim().ToLowerInvariant().Replace('ё', 'е');
        return normalized == ConfirmationPhrase;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsConfirmationPhraseValid(Confirmation))
        {
            yield return new ValidationResult(
                "Введите «" + ConfirmationPhrase + "», чтобы подтвердить разблокировку",
                [nameof(Confirmation)]);
        }
    }
}

public class CloseProjectViewModel
{
    public int ProjectId { get; set; }

    [ReadOnly(true)]
    public string OriginalName { get; set; }

    [Display(Name = "Опубликовать сюжет всем",
        Description =
            "Cюжет игры будет раскрыт всем для всеобщего просмотра и послужит обмену опытом среди мастеров.")]
    public bool PublishPlot { get; set; }

    public bool IsMaster { get; set; }
}

