using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Helpers.Validation;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models
{
  public abstract class GameObjectViewModelBase  : IRootGroupAware
  {
    public int ProjectId { get; set; }
    
    [ReadOnly(true)]
    public string ProjectName { get; set; }

    [CannotBeEmpty, DisplayName("Является частью групп")]
    public List<int> ParentCharacterGroupIds { get; set; } = new List<int>();

    [Display(Name = "Публично?", Description = "Публичные группы показываются в сетке ролей и на карточках персонажей.")]
    public bool IsPublic { get; set; } = true;

    [Display(Name = "Описание", Description = "Если группа публична, будет доступно всем. Если нет — только членам группы.")]
    public MarkdownViewModel Description { get; set; }

    [ReadOnly(true)]
    public CharacterGroupListViewModel Data { get; set; }

    public abstract IEnumerable<CharacterGroupListItemViewModel> PossibleParents { get; }

    [ReadOnly(true)]
    public int RootGroupId { get; set; }
  }
}