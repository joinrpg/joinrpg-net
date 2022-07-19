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

    [Display(Name = "Лимит заявок", Description = "Не включает уже прописанных в сетке ролей"), Range(0, 100)]
    public int DirectSlots { get; set; }

    [Display(Name = "Заявки в группу",
        Description = "Разрешены ли персонажи, кроме прописанных в сетке ролей АКА «И еще три стражника». Рекомендуется выбрать вариант «Заявки вне прописанных мастерами персонажей запрещены», а вместо остальных вариантов использовать персонажи типа «Слот». Это позволит заранее прописать все нужные поля.")]
    public DirectClaimSettings HaveDirectSlots { get; set; }

    [Display(Name = "Описание", Description = "Для публичных сущностей будет доступно всем."),
     // ReSharper disable once Mvc.TemplateNotResolved
     UIHint("MarkdownString")]
    public string Description { get; set; }

    public bool HaveDirectSlotsForSave() => HaveDirectSlots != DirectClaimSettings.NoDirectClaims;

    public int DirectSlotsForSave() => HaveDirectSlots == DirectClaimSettings.DirectClaimsUnlimited ? -1 : DirectSlots;
}
