using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectMasterTools.Acl;

/// <summary>
/// Профиль мастера на странице мастеров (ADR019, §4): роль, описание, публичность.
/// Свой профиль мастер правит сам, чужой — с правом выдавать доступ.
/// </summary>
public interface IMasterProfileClient
{
    Task<MasterProfileViewModel> GetProfile(ProjectIdentification projectId, UserIdentification userId);
    Task SaveProfile(MasterProfileViewModel model);
}

public class MasterProfileViewModel
{
    public required ProjectIdentification ProjectId { get; set; }
    public required UserIdentification UserId { get; set; }

    [Display(Name = "Роль в проекте", Description = "Например, «Главный мастер» или «Мастер по боёвке». Видна игрокам на странице мастеров.")]
    [Required(ErrorMessage = "Укажите роль мастера в проекте")]
    [MaxLength(100)]
    public required string Role { get; set; }

    [Display(Name = "О себе", Description = "За что отвечает мастер и по каким вопросам ему писать. Видно игрокам на странице мастеров. Можно использовать Markdown.")]
    public string? Description { get; set; }

    [Display(Name = "Показывать игрокам", Description = "Непубличного мастера видят только мастера проекта. На права и уведомления это не влияет.")]
    public bool IsPublic { get; set; }
}
