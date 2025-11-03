using System.ComponentModel.DataAnnotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.CheckIn;

public class CheckInIndexViewModel(Project project, IReadOnlyCollection<ClaimWithPlayer> claims) : IProjectIdAware
{
    public int ProjectId { get; } = project.ProjectId;

    public int ClaimId { get; set; }

    public IReadOnlyCollection<CheckInListItemViewModel> Claims { get; } = claims.Select(claim => new CheckInListItemViewModel(claim)).ToList();
}

public class CheckInListItemViewModel(ClaimWithPlayer claim)
{
    public string OtherNicks { get; } = claim.ExtraNicknames ?? "";

    public string NickName { get; } = claim.Player.DisplayName.DisplayName;

    public string Fullname { get; } = claim.Player.DisplayName.FullName ?? "";

    public string CharacterName { get; } = claim.CharacterName;

    public int ClaimId { get; } = claim.ClaimId.ClaimId;
}

public class CheckInSetupModel : IValidatableObject
{
    [Display(Name = "Включить модуль регистрации", Description = "Начнет отображать игрокам информацию о готовности заявок к регистрации")]
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
                yield return new ValidationResult("Включите сначала модуль регистрации", new[] { nameof(CheckInProgress) });
            }
            if (AllowSecondRoles)
            {
                yield return new ValidationResult("Включите сначала модуль регистрации", new[] { nameof(AllowSecondRoles) });
            }
        }
    }
}
