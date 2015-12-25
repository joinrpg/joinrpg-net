using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models
{
  public class GameFieldViewModelBase : IValidatableObject
  {
    public int ProjectId { get; set; }

    [Display(Name="Название поля")]
    public string Name { get; set; }

    [Display(Name = "Публичное (видно всем)")]
    public bool IsPublic { get; set; }

    [Display(Name = "Видно игроку")]
    public bool CanPlayerView { get; set; }

    [Display(Name = "Игрок может менять")]
    public bool CanPlayerEdit { get; set; }

    [Display(Name = "Описание")]
    public string FieldHint { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      if (IsPublic && !CanPlayerView)
      {
        yield return new ValidationResult("Нельзя скрыть публичное поле от игрока");
      }
    }
  }

  public class GameFieldEditViewModel : GameFieldViewModelBase
  {
    public int ProjectCharacterFieldId { get; set; }

    public bool IsActive { get; set; }

    public bool HasValueList { get; }

    public GameFieldEditViewModel(ProjectCharacterField field)
    {
      CanPlayerView = field.CanPlayerView;
      CanPlayerEdit = field.CanPlayerEdit;
      FieldHint = field.FieldHint;
      ProjectCharacterFieldId = field.ProjectCharacterFieldId;
      IsPublic = field.IsPublic;
      Name = field.FieldName;
      ProjectId = field.ProjectId;
      IsActive = field.IsActive;
      HasValueList = field.HasValueList();
      DropdownValues = field.DropdownValues.Select(fv => new GameFieldDropdownValueListItemViewModel(fv));
    }

    public GameFieldEditViewModel()
    { }

    [ReadOnly(true)]
    public IEnumerable<GameFieldDropdownValueListItemViewModel> DropdownValues { get; set; }
  }


  public enum CharacterFieldType
  {
    [Display(Name="Строка")]
    String,
    [Display(Name = "Текст")]
    Text,
    [Display(Name = "Выбор")]
    Dropdown,
    [Display(Name = "Чекбокс")]
    Checkbox,
    [Display(Name = "Мультивыбор")]
    MultiSelect
  }

  public class GameFieldCreateViewModel : GameFieldViewModelBase
  {
    [Display(Name="Тип поля")]
    public CharacterFieldType FieldType { get; set; }
  }

  public class GameFieldListViewModel
  {
    public int ProjectId { get; set; }
    public IEnumerable<GameFieldEditViewModel> Items { get; set; }
  }

  public abstract class GameFieldDropdownValueViewModelBase
  {
    [Display(Name="Значение"), Required]
    public string Label { get; set; }

    [Display(Name = "Описание"),DataType(DataType.MultilineText)]
    public string Description { get; set; }

    public int ProjectId { get; set; }
    public int ProjectCharacterFieldId { get; set; }
  }

  public class GameFieldDropdownValueListItemViewModel : GameFieldDropdownValueViewModelBase
  {
    public bool IsActive { get; set; }

    public int ProjectCharacterFieldDropdownValueId { get; set; }

    public GameFieldDropdownValueListItemViewModel(ProjectCharacterFieldDropdownValue value)
    {
      Label = value.Label;
      Description = value.Description;
      IsActive = value.IsActive;
      ProjectId = value.ProjectId;
      ProjectCharacterFieldId = value.ProjectCharacterFieldId;
      ProjectCharacterFieldDropdownValueId = value.ProjectCharacterFieldDropdownValueId;
    }
  }

  public class GameFieldDropdownValueEditViewModel : GameFieldDropdownValueViewModelBase
  {
    public bool IsActive
    { get; set; }

    public int ProjectCharacterFieldDropdownValueId
    { get; set; }

    public GameFieldDropdownValueEditViewModel(ProjectCharacterFieldDropdownValue value)
    {
      Label = value.Label;
      Description = value.Description;
      IsActive = value.IsActive;
      ProjectId = value.ProjectId;
      ProjectCharacterFieldId = value.ProjectCharacterFieldId;
      ProjectCharacterFieldDropdownValueId = value.ProjectCharacterFieldDropdownValueId;
    }

    public GameFieldDropdownValueEditViewModel() { }//For binding

  }

  public class GameFieldDropdownValueCreateViewModel : GameFieldDropdownValueViewModelBase
  {
    public GameFieldDropdownValueCreateViewModel(ProjectCharacterField field)
    {
      ProjectId = field.ProjectId;
      ProjectCharacterFieldId = field.ProjectCharacterFieldId;
      Label = $"Вариант {(field.DropdownValues.Count + 1)}";
    }

    public GameFieldDropdownValueCreateViewModel() { }//For binding
  }
}
