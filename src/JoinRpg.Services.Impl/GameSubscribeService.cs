using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Subscribe;

namespace JoinRpg.Services.Impl
{
    internal class GameSubscribeService : DbServiceImplBase, IGameSubscribeService
    {
        public GameSubscribeService(IUnitOfWork unitOfWork, ICurrentUserAccessor currentUserAccessor)
            : base(unitOfWork, currentUserAccessor)
        {
        }

        public async Task RemoveSubscribe(RemoveSubscribeRequest request)
        {
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

        public async Task UpdateSubscribeForGroup(SubscribeForGroupRequest request)
        {
            _ = (await ProjectRepository.GetGroupAsync(request.ProjectId, request.CharacterGroupId))
                .RequestMasterAccess(CurrentUserId)
                .EnsureActive();

            var user = await UserRepository.GetWithSubscribe(CurrentUserId);
            var direct =
                user.Subscriptions.SingleOrDefault(s => s.CharacterGroupId == request.CharacterGroupId);

            if (request.AnySet())
            {
                if (direct == null)
                {
                    direct = new UserSubscription()
                    {
                        UserId = CurrentUserId,
                        CharacterGroupId = request.CharacterGroupId,
                        ProjectId = request.ProjectId,
                    };
                    _ = user.Subscriptions.Add(direct);
                }

                _ = direct.AssignFrom(request);
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
    }
}
