using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Models.CommonTypes;

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
    public MarkdownViewModel FieldHint { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      if (IsPublic && !CanPlayerView)
      {
        yield return new ValidationResult("Нельзя скрыть публичное поле от игрока.",
          new[] {nameof(CanPlayerView), nameof(IsPublic)});
      }
      if (CanPlayerEdit && !CanPlayerView)
      {
        yield return
          new ValidationResult("Нельзя скрыть поле от игрока и одновременно разрешить редактирование поля.",
            new[] {nameof(CanPlayerView), nameof(CanPlayerEdit)});
      }
    }
  }

  public class GameFieldEditViewModel : GameFieldViewModelBase, IMovableListItem
  {
    public int ProjectCharacterFieldId { get; set; }

    public bool IsActive { get; set; }

    public bool HasValueList { get; }

    public GameFieldEditViewModel(ProjectCharacterField field)
    {
      CanPlayerView = field.CanPlayerView;
      CanPlayerEdit = field.CanPlayerEdit;
      FieldHint = new MarkdownViewModel(field.FieldHint);
      ProjectCharacterFieldId = field.ProjectCharacterFieldId;
      IsPublic = field.IsPublic;
      Name = field.FieldName;
      ProjectId = field.ProjectId;
      IsActive = field.IsActive;
      HasValueList = field.HasValueList();
      DropdownValues = field.GetOrderedValues().Select(fv => new GameFieldDropdownValueListItemViewModel(fv));
    }

    public GameFieldEditViewModel()
    { }

    [ReadOnly(true)]
    public IEnumerable<GameFieldDropdownValueListItemViewModel> DropdownValues { get; set; }

    public bool First { get; set; }
    public bool Last { get; set; }
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

    [Display(Name = "Описание")]
    public MarkdownViewModel Description { get; set; }

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
      Description = new MarkdownViewModel(value.Description);
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
      Description = new MarkdownViewModel(value.Description);
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

  public static class GameFieldViewModelsExtensions
  {
    public static IEnumerable<GameFieldEditViewModel> ToViewModels(this IEnumerable<ProjectCharacterField> projectCharacterFields)
    {
      return projectCharacterFields.Select(pf => new GameFieldEditViewModel(pf)).ToList().MarkFirstAndLast();
    }
  }
}
