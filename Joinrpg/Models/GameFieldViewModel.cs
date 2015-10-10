using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;

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
    }

    public GameFieldEditViewModel()
    { }
  }

  public class GameFieldCreateViewModel : GameFieldViewModelBase
  {
    public CharacterFieldType FieldType { get; set; }
  }

  public class GameFieldListViewModel
  {
    public int ProjectId { get; set; }
    public IEnumerable<GameFieldEditViewModel> Items { get; set; }
  }
}
