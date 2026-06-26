using JoinRpg.Markdown;

namespace JoinRpg.Web.Models.FieldSetup;

/// <summary>
/// View class for displaying dropdown value in field's editor
/// </summary>
public class GameFieldDropdownValueListItemViewModel : IMovableListItem
{
    [Display(Name = "Значение"), Required]
    public string Label { get; set; }

    [Display(Name = "Описание")]
    public string Description { get; }

    [Display(Name = "Цена")]
    public int Price { get; }

    public int ProjectId { get; }
    public int ProjectFieldId { get; }
    public bool IsActive { get; }

    public bool MasterRestricted { get; }

    public int? CharacterGroupId { get; }

    public int ValueId { get; }

    public GameFieldDropdownValueListItemViewModel(ProjectFieldVariant variant, bool canPlayerEdit)
    {
        Label = variant.Label;
        Description = variant.Description.ToPlainTextWithoutHtmlEscape();
        IsActive = variant.IsActive;
        Price = variant.Price;
        ProjectId = variant.Id.FieldId.ProjectId;
        ProjectFieldId = variant.Id.FieldId.ProjectFieldId;
        ValueId = variant.Id.ProjectFieldVariantId;
        CharacterGroupId = variant.CharacterGroupId?.CharacterGroupId;
        MasterRestricted = !variant.IsPlayerSelectable && canPlayerEdit;
    }

    #region Implementation of IMovableListItem

    public bool First { get; set; }
    public bool Last { get; set; }
    int IMovableListItem.ItemId => ValueId;

    #endregion
}
