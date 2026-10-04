using System.ComponentModel.DataAnnotations;
using JoinRpg.Web.ProjectCommon.Masters;

namespace JoinRpg.Web.ProjectMasterTools.Acl;

/// <summary>
/// Форма добавления мастера (ADR019, §4): профиль и права. Рендерится статически и постит обычную форму
/// в MVC-экшен (имена полей — как у AddAclViewModel).
/// </summary>
public class AddMasterFormViewModel
{
    /// <summary>Куда постить форму.</summary>
    public required string Endpoint { get; init; }

    /// <summary>Профиль добавляемого — ссылка «вернуться» рядом с кнопкой.</summary>
    public required string UserProfileUrl { get; init; }

    public required int UserId { get; init; }

    [Display(Name = "Роль в проекте", Description = "Например, «Главный мастер» или «Мастер по боёвке». Видна игрокам на странице мастеров.")]
    public required string Role { get; init; }

    [Display(Name = "О себе", Description = "За что отвечает мастер и по каким вопросам ему писать. Видно игрокам на странице мастеров. Можно использовать Markdown.")]
    public required string Description { get; init; }

    [Display(Name = "Показывать игрокам", Description = "Непубличного мастера видят только мастера проекта. На права и уведомления это не влияет.")]
    public required bool IsPublic { get; init; }

    public required IReadOnlyCollection<PermissionBadgeViewModel> Permissions { get; init; }

    /// <summary>Ошибки предыдущей попытки — показываются над формой.</summary>
    public required IReadOnlyCollection<string> Errors { get; init; }
}
