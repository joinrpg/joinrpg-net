using System.ComponentModel.DataAnnotations;
using JoinRpg.Web.ProjectCommon.Claims;

namespace JoinRpg.Web.Models;

public class MassMailViewModel
{
    public int ProjectId { get; set; }
    public bool ToMyClaimsOnlyWarning { get; set; }
    public required CompressedIntList ClaimIds { get; set; }
    [Display(Name = "Адресаты")]
    public IEnumerable<ClaimLinkViewModel> Claims { get; set; } = [];

    [Display(Name = "Тема рассылки"), Required]
    public required string Subject { get; set; }
    [Display(Name = "Текст"), Required, UIHint("MarkdownString")]
    public required string Body { get; set; }
    [Display(Name = "Также включить всех мастеров")]
    public bool AlsoMailToMasters { get; set; }

    public string ProjectName { get; set; }
}
