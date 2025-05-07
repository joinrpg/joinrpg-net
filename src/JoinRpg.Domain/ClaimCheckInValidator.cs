using JoinRpg.DataModel;
using JoinRpg.Domain.Problems;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain;

public class ClaimCheckInValidator(Claim claim, IProblemValidator<Claim> claimValidator, ProjectInfo projectInfo)
{
    public int FeeDue => claim.ClaimFeeDue(projectInfo);

    public bool NotCheckedInAlready => claim.CheckInDate == null &&
                                       claim.ClaimStatus != Claim.Status.CheckedIn;

    public bool IsApproved => claim.ClaimStatus == Claim.Status.Approved;

    public IReadOnlyCollection<FieldRelatedProblem> FieldProblems { get; } = [.. claimValidator.ValidateFieldsOnly(claim, projectInfo)];

    public bool CanCheckInInPrinciple => NotCheckedInAlready && IsApproved && FieldProblems.Count == 0;

    public bool CanCheckInNow => CanCheckInInPrinciple && FeeDue <= 0;
}
