using System.Data.Entity;
using System.Linq.Expressions;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Subscribe;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Dal.Impl.Repositories;

internal class UserInfoRepository(MyDbContext ctx) : IUserRepository, IUserSubscribeRepository
{
    public Task<User> GetById(int id) => ctx.UserSet.FindAsync(id) ?? throw new JoinRpgEntityNotFoundException(id, nameof(User));
    public Task<User> WithProfile(int userId)
    {
        return ctx.Set<User>()
          .Include(u => u.Auth)
          .Include(u => u.Allrpg)
          .Include(u => u.Extra)
          .Include(u => u.Avatars)
          .Include(u => u.SelectedAvatar)
          .SingleOrDefaultAsync(u => u.UserId == userId);
    }

    public Task<User> GetWithSubscribe(int currentUserId)
        => ctx
            .Set<User>()
            .Include(u => u.Subscriptions)
            .SingleOrDefaultAsync(u => u.UserId == currentUserId);

    public async Task<(User User, UserSubscriptionDto[] UserSubscriptions)>
        LoadSubscriptionsForProject(int userId, int projectId)
    {
        var user = await WithProfile(userId);
        var subscribe = await ctx.Set<UserSubscription>()
            .Where(x => x.ProjectId == projectId && x.UserId == userId)
            .Select(SubscriptionDtoBuilder())
            .ToArrayAsync();
        return (user, subscribe);
    }

    private static Expression<Func<UserSubscription, UserSubscriptionDto>> SubscriptionDtoBuilder()
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
                    ClaimName = x.Claim.Character.CharacterName,
                    Options = new SubscriptionOptions
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
        var subscribe = await ctx.Set<UserSubscription>()
            .Where(x => x.ProjectId == projectId && x.UserSubscriptionId == subscriptionId)
            .Select(SubscriptionDtoBuilder())
            .FirstOrDefaultAsync();
        return subscribe;
    }

    public async Task<User?> GetByEmail(string email)
    {
        return await ctx.Set<User>()
          .Include(u => u.Auth)
          .Include(u => u.Allrpg)
          .Include(u => u.Extra)
          .SingleOrDefaultAsync(u => u.Email == email);
    }

    Task<UserAvatar> IUserRepository.LoadAvatar(AvatarIdentification userAvatarId)
        => ctx.Set<User>()
            .SelectMany(user => user.Avatars)
            .SingleOrDefaultAsync(a => a.UserAvatarId == userAvatarId.Value);

    public async Task<(UserIdentification, AvatarIdentification)[]> GetLegacyAvatarsList()
    {
        var res = await ctx.Set<User>().SelectMany(user => user.Avatars)
            .Where(a => a.CachedUri != null && a.CachedUri.Contains("windows")).
            Select(a => new { a.UserId, a.UserAvatarId })
            .ToListAsync();

        return res.Select(a => (new UserIdentification(a.UserId), new AvatarIdentification(a.UserAvatarId))).ToArray();
    }
}
