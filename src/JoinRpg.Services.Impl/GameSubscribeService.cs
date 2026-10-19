using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces.Subscribe;

namespace JoinRpg.Services.Impl;

internal class GameSubscribeService : DbServiceImplBase, IGameSubscribeService
{
    private readonly IProjectMetadataRepository projectMetadataRepository;

    public GameSubscribeService(IUnitOfWork unitOfWork, ICurrentUserAccessor currentUserAccessor, IProjectMetadataRepository projectMetadataRepository)
        : base(unitOfWork, currentUserAccessor)
    {
        this.projectMetadataRepository = projectMetadataRepository;
    }

    public async Task RemoveSubscribe(RemoveSubscribeRequest request)
    {
        await EnsureProjectNotBlocked(new ProjectIdentification(request.ProjectId));
        var user = await UserRepository.GetWithSubscribe(CurrentUserId);

        var direct =
            user.Subscriptions.SingleOrDefault(s => s.UserSubscriptionId == request.UserSubscribtionId && s.ProjectId == request.ProjectId);

        if (direct is null)
        {
            throw new JoinRpgEntityNotFoundException(request.UserSubscribtionId, nameof(UserSubscription));
        }

        _ = UnitOfWork.GetDbSet<UserSubscription>().Remove(direct);

        await UnitOfWork.SaveChangesAsync();
    }

    public async Task SubscribeClaimToUser(ClaimIdentification claimId)
    {
        await EnsureProjectNotBlocked(claimId.ProjectId);
        var user = await UserRepository.GetWithSubscribe(CurrentUserId);
        _ = (await ClaimsRepository.GetClaim(claimId)).RequestAccess(CurrentUserId);
        _ = user.Subscriptions.Add(
            new UserSubscription() { ClaimId = claimId.ClaimId, ProjectId = claimId.ProjectId }.AssignFrom(
                SubscriptionOptions.CreateAllSet()));
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task UnsubscribeClaimToUser(ClaimIdentification claimId)
    {
        await EnsureProjectNotBlocked(claimId.ProjectId);
        var user = await UserRepository.GetWithSubscribe(CurrentUserId);
        _ = (await ClaimsRepository.GetClaim(claimId)).RequestAccess(CurrentUserId);
        var subscription = user.Subscriptions.FirstOrDefault(s =>
            s.ProjectId == claimId.ProjectId && s.UserId == CurrentUserId && s.ClaimId == claimId.ClaimId);
        if (subscription != null)
        {
            _ = UnitOfWork.GetDbSet<UserSubscription>().Remove(subscription);
            await UnitOfWork.SaveChangesAsync();
        }
    }

    public async Task UpdateSubscribeForGroup(SubscribeForGroupRequest request)
    {
        await EnsureProjectNotBlocked(request.CharacterGroupId.ProjectId);
        var characterGroup = await ProjectRepository.GetGroupAsync(request.CharacterGroupId)
            ?? throw new JoinRpgEntityNotFoundException(request.CharacterGroupId.CharacterGroupId, nameof(CharacterGroup));

        _ = characterGroup
            .RequestMasterAccess(CurrentUserId)
            .EnsureActive();

        if (CurrentUserId != request.MasterId)
        {
            throw new JoinRpgInvalidUserException("Changing another user not implemented yet");
        }

        var user = await UserRepository.GetWithSubscribe(CurrentUserId);
        var direct =
            user.Subscriptions.SingleOrDefault(s => s.CharacterGroupId == request.CharacterGroupId.CharacterGroupId);

        if (request.SubscriptionOptions.AnySet)
        {
            if (direct == null)
            {
                direct = new UserSubscription()
                {
                    UserId = CurrentUserId,
                    CharacterGroupId = request.CharacterGroupId.CharacterGroupId,
                    ProjectId = request.CharacterGroupId.ProjectId,
                };
                _ = user.Subscriptions.Add(direct);
            }

            _ = direct.AssignFrom(request.SubscriptionOptions);
        }
        else
        {
            if (direct != null)
            {
                _ = UnitOfWork.GetDbSet<UserSubscription>().Remove(direct);
            }
        }

        await UnitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Часть снятия мастера, которое разрешено и в заблокированном проекте (ADR023), поэтому блокировку не проверяет.
    /// </summary>
    public async Task RemoveAllSubscriptions(ProjectIdentification projectId, UserIdentification userId)
    {
        if (userId != currentUserAccessor.UserIdentification && !IsCurrentUserAdmin)
        {
            var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
            _ = projectInfo.RequestMasterAccess(currentUserAccessor, Permission.CanGrantRights);
        }

        _ = UnitOfWork.GetDbSet<UserSubscription>().RemoveRange(
            UnitOfWork.GetDbSet<UserSubscription>().Where(x => x.UserId == userId.Value && x.ProjectId == projectId.Value));
        await UnitOfWork.SaveChangesAsync();
    }

    /// <summary>Подписки в заблокированном проекте не меняются (ADR023). Архив здесь не проверялся и раньше.</summary>
    private async Task EnsureProjectNotBlocked(ProjectIdentification projectId)
        => _ = (await projectMetadataRepository.GetProjectMetadata(projectId)).EnsureNotBlocked();
}
