using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Helpers.Validation;

namespace JoinRpg.Web.Models.CharacterGroups;

public abstract class CharacterGroupViewModelBase : IProjectIdAware
{
    public int ProjectId { get; set; }

    [ReadOnly(true)]
    public string ProjectName { get; set; }

    [Display(Name = "Публично?",
        Description =
            "Публичные сущности показываются в сетке ролей, их описание и карточки доступны всем.")]
    public bool IsPublic { get; set; } = true;

    [CannotBeEmpty, DisplayName("Является частью групп")]
    public List<string> ParentCharacterGroupIds { get; set; } = new List<string>();

    [DisplayName("Название группы"), Required]
    public string Name { get; set; }

    [Display(Name = "Описание", Description = "Для публичных сущностей будет доступно всем."),
     // ReSharper disable once Mvc.TemplateNotResolved
     UIHint("MarkdownString")]
    public string Description { get; set; }
}
