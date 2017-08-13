using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.CheckIn
{
  public class CheckInIndexViewModel : IProjectIdAware
  {
    public CheckInIndexViewModel(Project project, IReadOnlyCollection<ClaimWithPlayer> claims)
    {
      ProjectId = project.ProjectId;
      Claims = claims.Select(claim => new CheckInListItemViewModel(claim)).ToList();
    }

    public int ProjectId { get; }

    public int ClaimId { get; set; }

    public IReadOnlyCollection<CheckInListItemViewModel> Claims { get; }
  }

  public class CheckInListItemViewModel
  {
    

    public CheckInListItemViewModel(ClaimWithPlayer claim)
    {
      ClaimId = claim.ClaimId;
      CharacterName = claim.CharacterName;
      Fullname = claim.Player.FullName;
      NickName = claim.Player.DisplayName;
      OtherNicks = claim.Player.Extra?.Nicknames ?? "";
    }

    public string OtherNicks { get;  }

    public string NickName { get; }

    public string Fullname { get; }

    public string CharacterName { get; }

    public int ClaimId { get; }
  }

  public class CheckInSetupModel : IValidatableObject
  {
    [Display(Name="Включить модуль регистрации", Description = "Начнет отображать игрокам информацию о готовности заявок к регистрации")]
    public bool EnableCheckInModule { get; set; }
    [Display(Name = "Регистрация в процессе", Description = "Заезд и регистрация игроков идет. Включить кнопку регистрации.")]
    public bool CheckInProgress { get; set; }
    [Display(Name = "Разрешить выпуск вторыми ролями", Description = "Включить соответствующую кнопку.")]
    public bool AllowSecondRoles { get; set; }

    public int ProjectId { get; set; }

    public CheckInSetupModel(Project project)
    {
      ProjectId = project.ProjectId;
      EnableCheckInModule = project.Details.EnableCheckInModule;
      CheckInProgress = project.Details.CheckInProgress;
      AllowSecondRoles = project.Details.AllowSecondRoles;
    }

    public CheckInSetupModel() { }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      if (!EnableCheckInModule)
      {
        if (CheckInProgress)
        {
          yield return new ValidationResult("Включите сначала модуль регистрации", new []{nameof(CheckInProgress)});
        }
        if (AllowSecondRoles)
        {
          yield return new ValidationResult("Включите сначала модуль регистрации", new[] { nameof(AllowSecondRoles) });
        }
      }
    }
  }
}