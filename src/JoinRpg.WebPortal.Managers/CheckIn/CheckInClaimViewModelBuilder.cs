using JoinRpg.Common.WebComponents;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Web.CheckIn;
using JoinRpg.Web.Claims;
using JoinRpg.Web.UserProfile;

namespace JoinRpg.WebPortal.Managers.CheckIn;

/// <summary>
/// Строит форму регистрации на стойке (<see cref="CheckInClaimViewModel"/>) по заявке.
/// </summary>
public class CheckInClaimViewModelBuilder(
    IClaimProblemValidator claimValidator,
    ICurrentUserAccessor currentUserAccessor,
    UserProfileDetailsBuilder cardBuilder)
{
    /// <param name="claimInfo">Заявка вместе с персонажем и профилем игрока (ADR021).</param>
    public CheckInClaimViewModel Build(ClaimInfo claimInfo)
    {
        ArgumentNullException.ThrowIfNull(claimInfo);

        var projectInfo = claimInfo.ProjectInfo;
        var claim = claimInfo.Claim;
        var validator = new ClaimCheckInValidator(claimInfo, claimValidator);
        var responsibleMaster = projectInfo.GetMasterById(claim.ResponsibleMasterId);
        var problems = validator.Problems.Select(p => new CheckInProblemViewModel(p)).ToList();
        var currentUserFullName = currentUserAccessor.DisplayName.FullName ?? "";

        return new CheckInClaimViewModel
        {
            ProjectId = claimInfo.ClaimId.ProjectId.Value,
            ClaimId = claimInfo.ClaimId.ClaimId,
            Master = new UserLinkViewModel(responsibleMaster.UserId, responsibleMaster.Name.DisplayName, ViewMode.Show),
            PlayerDetails = cardBuilder.Build(claimInfo.Player, projectInfo, currentUserAccessor.UserIdentificationOrDefault, currentUserAccessor.IsAdmin),
            ClaimStatus = (ClaimStatusView)claim.Status,
            CheckInTime = claim.CheckInDate,
            CheckedInAlready = !validator.NotCheckedInAlready,
            IsApproved = validator.IsApproved,
            CanCheckInInPrinciple = validator.CanCheckInInPrinciple,
            FeeDue = validator.FeeDue,
            CanAcceptFee = projectInfo.ProjectFinanceSettings.CanAcceptCash(currentUserAccessor.UserIdentification),
            Problems = problems,
            CurrentUserFullName = currentUserFullName,
            Override = projectInfo.HasMasterAccess(currentUserAccessor.UserIdentification, Permission.CanManageClaims)
                       && validator.RequiredOverride is { } requiredOverride
                ? new CheckInOverrideViewModel(
                    [.. problems.Select(p => p.Details is null ? p.Title : $"{p.Title} — {p.Details}")],
                    [.. requiredOverride.Problems.Select(key => key.ToString())],
                    requiredOverride.FeeDue,
                    currentUserFullName)
                : null,
        };
    }
}
