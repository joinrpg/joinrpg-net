using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models
{
  public class MassMailViewModel
  {
    public int ProjectId { get; set; }
    public bool ToMyClaimsOnlyWarning { get; set; }
    public ICollection<int> ClaimIds { get; set; }
    [Display(Name="Адресаты")]
    public IEnumerable<ClaimShortListItemViewModel> Claims { get; set; }
    [Display(Name="Тема рассылки"), Required]
    public string Subject { get; set; }
    [Display(Name = "Текст"), Required]
    public MarkdownViewModel Body { get; set; }
    [Display(Name = "Также включить всех мастеров")]
    public bool AlsoMailToMasters { get; set; }

    public string ProjectName { get; set; }
  }
}
