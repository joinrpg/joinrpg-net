using JoinRpg.DataModel;
using JoinRpg.Domain.Problems;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain;

public class ClaimCheckInValidator

{
    private readonly Claim claim;
    private readonly IProblemValidator<Claim> claimValidator;
    private readonly ProjectInfo projectInfo;

    public ClaimCheckInValidator(Claim claim, IProblemValidator<Claim> claimValidator, ProjectInfo projectInfo)
    {
        this.claim = claim ?? throw new ArgumentNullException(nameof(claim));
        this.claimValidator = claimValidator;
        this.projectInfo = projectInfo;
    }

    public int FeeDue => claim.ClaimFeeDue();

    public bool NotCheckedInAlready => claim.CheckInDate == null &&
                                       claim.ClaimStatus != Claim.Status.CheckedIn;

    public bool IsApproved => claim.ClaimStatus == Claim.Status.Approved;

    public IReadOnlyCollection<FieldRelatedProblem> NotFilledFields => claimValidator.ValidateFieldsOnly(claim, projectInfo).ToList();

    public bool CanCheckInInPrinciple => NotCheckedInAlready && IsApproved &&
                                         !NotFilledFields.Any();

    public bool CanCheckInNow => CanCheckInInPrinciple && FeeDue <= 0;
}
