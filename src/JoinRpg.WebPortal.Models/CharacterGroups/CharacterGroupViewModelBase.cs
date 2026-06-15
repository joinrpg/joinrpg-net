using System.ComponentModel;

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

    [DisplayName("Название группы"), Required]
    public string Name { get; set; }

    [Display(Name = "Описание", Description = "Для публичных сущностей будет доступно всем."),

     UIHint("MarkdownString")]
    public string Description { get; set; }
}
