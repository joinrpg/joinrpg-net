using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Helpers.Validation;

namespace JoinRpg.Web.Models
{
  public abstract class GameObjectViewModelBase  : IProjectIdAware
  {
    public int ProjectId { get; set; }
    
    [ReadOnly(true)]
    public string ProjectName { get; set; }

    [CannotBeEmpty, DisplayName("Является частью групп")]
    public List<string> ParentCharacterGroupIds { get; set; } = new List<string>();

    [Display(Name = "Публично?", Description = "Публичные группы показываются в сетке ролей и на карточках персонажей.")]
    public bool IsPublic { get; set; } = true;

    [Display(Name = "Описание", Description = "Если группа публична, будет доступно всем."), UIHint("MarkdownString")]
    public string Description { get; set; }
  }
}