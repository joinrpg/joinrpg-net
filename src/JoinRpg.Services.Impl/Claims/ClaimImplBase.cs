using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl.Claims;

internal abstract class ClaimImplBase(IUnitOfWork unitOfWork,
    IEmailService emailService,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository,
    CommentHelper commentHelper
    ) : DbServiceImplBase(unitOfWork, currentUserAccessor)
{
    protected CommentHelper CommentHelper { get; } = commentHelper;
    protected IProjectMetadataRepository ProjectMetadataRepository { get; } = projectMetadataRepository;

    protected IEmailService EmailService { get; } = emailService;

    protected void CheckOperationDate(DateTime operationDate) => CheckOperationDate(operationDate, Now);

    /// <summary>
    /// Версия с явным временем операции: мигрированные методы берут его из контекста, а не из поля
    /// сервиса, зафиксированного в конструкторе (ADR014).
    /// </summary>
    internal static void CheckOperationDate(DateTime operationDate, DateTime now)
    {
        if (operationDate > now.AddDays(1)
        ) //TODO[UTC]: if everyone properly uses UTC, we don't have to do +1
        {
            throw new CannotPerformOperationInFuture();
        }
    }

    [Obsolete("Используй ICharacterPropsService.ChangeClaim, см. ADR014")]
    protected Task<(Claim, ProjectInfo)> LoadClaimAsMaster(IClaimOperationRequest request, Permission permission = Permission.None, ExtraAccessReason reason = ExtraAccessReason.None)
        => LoadClaimAsMaster(new ClaimIdentification(request.ProjectIdentification, request.ClaimId), permission, reason);


    [Obsolete("Используй ICharacterPropsService.ChangeClaim, см. ADR014")]
    protected async Task<(Claim, ProjectInfo)> LoadClaimAsMaster(ClaimIdentification claimId, Permission permission = Permission.None, ExtraAccessReason reason = ExtraAccessReason.None)
    {
        var claim = await ClaimsRepository.GetClaim(claimId);
        var projectInfo = await ProjectMetadataRepository.GetProjectMetadata(claimId.ProjectId);

        return (claim.RequestAccess(CurrentUserId, permission, reason), projectInfo);
    }

    [Obsolete("Используй ICharacterPropsService.ChangeClaim, см. ADR014")]
    protected async Task<(Claim, ProjectInfo)> LoadClaimAsPlayer(ClaimIdentification claimId)
    {
        var claim = await ClaimsRepository.GetClaim(claimId);
        var projectInfo = await ProjectMetadataRepository.GetProjectMetadata(claimId.ProjectId);

        if (claim?.PlayerUserId != CurrentUserId)
        {
            throw new PlayerOnlyException(claimId, CurrentUserId);
        }

        return (claim, projectInfo);
    }

    protected async Task<TEmail> CreateClaimEmail<TEmail>(
        Claim claim,
        string commentText,
        Func<UserSubscription, bool> subscribePredicate,
        CommentExtraAction? commentExtraAction,
        bool mastersOnly = false,
        IEnumerable<User?>? extraRecipients = null)
        where TEmail : ClaimEmailModel, new()
    {
        var initiator = await GetCurrentUser();
        if (claim == null)
        {
            throw new ArgumentNullException(nameof(claim));
        }

        if (commentText == null)
        {
            throw new ArgumentNullException(nameof(commentText));
        }

        var subscriptions =
            claim.GetSubscriptions(subscribePredicate, extraRecipients ?? Enumerable.Empty<User>(),
                mastersOnly).ToList();
        return new TEmail()
        {
            Claim = claim,
            ProjectName = claim.Project.ProjectName,
            Initiator = initiator,
            InitiatorType = initiator.UserId == claim.PlayerUserId ? ParcipantType.Player : ParcipantType.Master,
            Recipients = subscriptions,
            Text = new MarkdownDbValue(commentText),
            CommentExtraAction = commentExtraAction,
        };
    }
}
