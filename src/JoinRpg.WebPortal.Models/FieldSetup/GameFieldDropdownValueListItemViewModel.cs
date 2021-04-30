using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Markdown;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models.FieldSetup
{
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

        public GameFieldDropdownValueListItemViewModel(ProjectFieldDropdownValue value)
        {
            Label = value.Label;
            Description = value.Description.ToPlainText().ToString();
            IsActive = value.IsActive;
            Price = value.Price;
            ProjectId = value.ProjectId;
            ProjectFieldId = value.ProjectFieldId;
            ValueId = value.ProjectFieldDropdownValueId;
            CharacterGroupId = value.CharacterGroup?.CharacterGroupId;
            MasterRestricted = !value.PlayerSelectable && value.ProjectField.CanPlayerEdit;
        }

        #region Implementation of IMovableListItem

        public bool First { get; set; }
        public bool Last { get; set; }
        int IMovableListItem.ItemId => ValueId;

        #endregion
    }
}
