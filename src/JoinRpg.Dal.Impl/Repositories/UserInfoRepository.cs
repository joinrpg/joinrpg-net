using System.Data.Entity;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Subscribe;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Dal.Impl.Repositories
{
    [UsedImplicitly]
    internal class UserInfoRepository : IUserRepository, IUserSubscribeRepository
    {
        private readonly MyDbContext _ctx;

        public UserInfoRepository(MyDbContext ctx) => _ctx = ctx;

        public Task<User> GetById(int id) => _ctx.UserSet.FindAsync(id);
        public Task<User> WithProfile(int userId)
        {
            return _ctx.Set<User>()
              .Include(u => u.Auth)
              .Include(u => u.Allrpg)
              .Include(u => u.Extra)
              .Include(u => u.Avatars)
              .Include(u => u.SelectedAvatar)
              .SingleOrDefaultAsync(u => u.UserId == userId);
        }

        public Task<User> GetWithSubscribe(int currentUserId)
            => _ctx
                .Set<User>()
                .Include(u => u.Subscriptions)
                .SingleOrDefaultAsync(u => u.UserId == currentUserId);

        public async Task<(User User, UserSubscriptionDto[] UserSubscriptions)>
            LoadSubscriptionsForProject(int userId, int projectId)
        {
            var user = await WithProfile(userId);
            var subscribe = await _ctx.Set<UserSubscription>()
                .Where(x => x.ProjectId == projectId && x.UserId == userId)
                .Select(SubscriptionDtoBuilder())
                .ToArrayAsync();
            return (user, subscribe);
        }

        private Expression<Func<UserSubscription, UserSubscriptionDto>> SubscriptionDtoBuilder()
        {
            return x =>
                    new UserSubscriptionDto()
                    {
                        UserSubscriptionId = x.UserSubscriptionId,
                        ProjectId = x.ProjectId,
                        CharacterGroupId = x.CharacterGroupId,
                        CharacterGroupName = x.CharacterGroup.CharacterGroupName,
                        CharacterId = x.CharacterId,
                        CharacterNames = x.Character.CharacterName,
                        ClaimId = x.ClaimId,
                        ClaimName = (x.Claim.Character != null ? x.Claim.Character.CharacterName : null)
                            ?? (x.Claim.Group != null ? x.Claim.Group.CharacterGroupName : null),
                        Options = new SubscriptionDto
                        {
                            AccommodationChange = x.AccommodationChange,
                            ClaimStatusChange = x.ClaimStatusChange,
                            Comments = x.Comments,
                            FieldChange = x.FieldChange,
                            MoneyOperation = x.MoneyOperation,
                        }
                    };
        }

        public async Task<UserSubscriptionDto> LoadSubscriptionById(int projectId, int subscriptionId)
        {
            var subscribe = await _ctx.Set<UserSubscription>()
                .Where(x => x.ProjectId == projectId && x.UserSubscriptionId == subscriptionId)
                .Select(SubscriptionDtoBuilder())
                .FirstOrDefaultAsync();
            return subscribe;
        }

        public Task<User> GetByEmail(string email)
        {
            return _ctx.Set<User>()
              .Include(u => u.Auth)
              .Include(u => u.Allrpg)
              .Include(u => u.Extra)
              .SingleOrDefaultAsync(u => u.Email == email);
        }

        Task<UserAvatar> IUserRepository.LoadAvatar(AvatarIdentification userAvatarId)
            => _ctx.Set<User>()
                .SelectMany(user => user.Avatars)
                .SingleOrDefaultAsync(a => a.UserAvatarId == userAvatarId.Value);
    }
}
