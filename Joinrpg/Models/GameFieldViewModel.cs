using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models
{
  public class GameFieldViewModelBase : IValidatableObject, IProjectIdAware
  {
    public int ProjectId { get; set; }

    [Display(Name="Название поля"), Required]
    public string Name { get; set; }

    [Display(Name = "Публичное (видно всем)")]
    public bool IsPublic { get; set; }

    [Display(Name = "Видно игроку")]
    public bool CanPlayerView { get; set; }

    [Display(Name = "Игрок может менять")]
    public bool CanPlayerEdit { get; set; }

    [Display(Name = "Описание")]
    public MarkdownViewModel Description { get; set; }

    [Display(Name = "Обязательное?")]
    public MandatoryStatusViewType MandatoryStatus { get; set; }

    [Display(Name = "Показывать только для групп", Description = "Если оставить пустым, будет показываться всегда")]
    public ICollection<string> ShowForGroups { get; set; } = new List<string>();

    [Display(Name = "Доступно NPC", Description = "Доступно ли для персонажей-NPC")]
    public bool ValidForNpc { get; set; }

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
    public int ProjectFieldId { get; set; }

    public bool HasValueList { get; }

    public GameFieldEditViewModel(ProjectField field)
    {
      CanPlayerView = field.CanPlayerView;
      CanPlayerEdit = field.CanPlayerEdit;
      Description = new MarkdownViewModel(field.Description);
      ProjectFieldId = field.ProjectFieldId;
      IsPublic = field.IsPublic;
      Name = field.FieldName;
      ProjectId = field.ProjectId;
      IsActive = field.IsActive;
      HasValueList = field.HasValueList();
      DropdownValues = field.GetOrderedValues().Select(fv => new GameFieldDropdownValueListItemViewModel(fv)).MarkFirstAndLast();
      FieldViewType = (ProjectFieldViewType) field.FieldType;
      FieldBoundTo = (FieldBoundToViewModel) field.FieldBoundTo;
      MandatoryStatus = (MandatoryStatusViewType) field.MandatoryStatus;
      ShowForGroups = field.GroupsAvailableFor.Select(c => c.CharacterGroupId).PrefixAsGroups().ToList();
    }

    public GameFieldEditViewModel()
    { }

    [ReadOnly(true)]
    public IEnumerable<GameFieldDropdownValueListItemViewModel> DropdownValues { get; }

    [Display(Name = "Тип поля"), ReadOnly(true)]
    public ProjectFieldViewType FieldViewType { get; }

    [Display(Name = "Привязано к"), ReadOnly(true)]
    public FieldBoundToViewModel FieldBoundTo { get; }

    [ReadOnly(true)]
    public bool IsActive { get; }

    public bool First { get; set; }
    public bool Last { get; set; }
    int IMovableListItem.ItemId => ProjectFieldId;
  }


  public enum ProjectFieldViewType
  {
    [Display(Name="Строка", Order =1), UsedImplicitly]
    String,
    [Display(Name = "Текст", Order = 2), UsedImplicitly]
    Text,
    [Display(Name = "Выбор", Order=3 ), UsedImplicitly]
    Dropdown,
    [Display(Name = "Чекбокс", Order = 5), UsedImplicitly]
    Checkbox,
    [Display(Name = "Мультивыбор", Order = 4), UsedImplicitly]
    MultiSelect,
    [Display(Name = "Заголовок", Order = 6), UsedImplicitly]
    Header
  }

  public enum FieldBoundToViewModel
  {
    [Display(Name="персонажу"), UsedImplicitly]
    Character,
    [Display(Name = "заявке"), UsedImplicitly]
    Claim,
  }

  public enum MandatoryStatusViewType
  {
    [Display(Name = "Опциональное"), UsedImplicitly] Optional,

    [Display(Name = "Рекомендованное",
      Description =
        "При незаполненном поле будет выдаваться предупреждение, а заявка или персонаж — помечаться как проблемные"),
     UsedImplicitly] Recommended,
    [Display(Name = "Обязательное", Description =
        "Сохранение с незаполенным полем будет невозможно."), 
      UsedImplicitly] Required
  }

  public class GameFieldCreateViewModel : GameFieldViewModelBase
  {
    [Display(Name="Тип поля")]
    public ProjectFieldViewType FieldViewType { get; set; }

    [Display(Name = "Привязано к")]
    public FieldBoundToViewModel FieldBoundTo { get; set; }
  }

  public class GameFieldListViewModel
  {
    public int ProjectId { get; set; }
    public IEnumerable<GameFieldEditViewModel> Items { get; set; }

    public bool CanEditFields { get; set; }
  }

  public abstract class GameFieldDropdownValueViewModelBase
  {
    [Display(Name="Значение"), Required]
    public string Label { get; set; }

    [Display(Name = "Описание")]
    public MarkdownViewModel Description { get; set; }

    public int ProjectId { get; set; }
    public int ProjectFieldId { get; set; }
  }

  public class GameFieldDropdownValueListItemViewModel : GameFieldDropdownValueViewModelBase, IMovableListItem
  {
    public bool IsActive { get; }

    public int ProjectFieldDropdownValueId { get;  }

    public GameFieldDropdownValueListItemViewModel(ProjectFieldDropdownValue value)
    {
      Label = value.Label;
      Description = new MarkdownViewModel(value.Description);
      IsActive = value.IsActive;
      ProjectId = value.ProjectId;
      ProjectFieldId = value.ProjectFieldId;
      ProjectFieldDropdownValueId = value.ProjectFieldDropdownValueId;
    }

    #region Implementation of IMovableListItem
    public bool First { get; set; }
    public bool Last { get; set; }
    int IMovableListItem.ItemId => ProjectFieldDropdownValueId;
    #endregion
  }

  public class GameFieldDropdownValueEditViewModel : GameFieldDropdownValueViewModelBase
  {
    public bool IsActive
    { get; set; }

    public int ProjectFieldDropdownValueId
    { get; set; }

    public GameFieldDropdownValueEditViewModel(ProjectFieldDropdownValue value)
    {
      Label = value.Label;
      Description = new MarkdownViewModel(value.Description);
      IsActive = value.IsActive;
      ProjectId = value.ProjectId;
      ProjectFieldId = value.ProjectFieldId;
      ProjectFieldDropdownValueId = value.ProjectFieldDropdownValueId;
    }

    public GameFieldDropdownValueEditViewModel() { }//For binding

  }

  public class GameFieldDropdownValueCreateViewModel : GameFieldDropdownValueViewModelBase
  {
    public GameFieldDropdownValueCreateViewModel(ProjectField field)
    {
      ProjectId = field.ProjectId;
      ProjectFieldId = field.ProjectFieldId;
      Label = $"Вариант {field.DropdownValues.Count + 1}";
    }

    public GameFieldDropdownValueCreateViewModel() { }//For binding
  }

  public static class GameFieldViewModelsExtensions
  {
    public static IEnumerable<GameFieldEditViewModel> ToViewModels(this IEnumerable<ProjectField> gameFields)
    {
      return gameFields.Select(pf => new GameFieldEditViewModel(pf)).MarkFirstAndLast();
    }
  }
}
