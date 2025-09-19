using System.Data.Entity;
using System.Linq.Expressions;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Subscribe;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Users;

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
    public async Task<UserInfo?> GetUserInfo(UserIdentification userId)
    {
        var userQuery =
            from user in ctx.Set<User>()
            where user.UserId == userId.Value
            select new
            {
                user.UserId,
                user.PrefferedName,
                user.FatherName,
                user.SurName,
                user.BornName,
                user.Email,
                user.ExternalLogins,
                user.Extra!.Telegram,
            };

        var result = await userQuery.SingleOrDefaultAsync();
        if (result is null)
        {
            return null;
        }

        var telegramId = TelegramId.FromOptional(result.ExternalLogins.SingleOrDefault(x => x.Provider == UserExternalLogin.TelegramProvider)?.Key, new PrefferedName(result.Telegram));

        return new UserInfo(new UserIdentification(result.UserId), new UserDisplayName(
            new UserFullName(
                PrefferedName.FromOptional(result.PrefferedName),
            BornName.FromOptional(result.BornName),
            SurName.FromOptional(result.SurName),
            FatherName.FromOptional(result.FatherName)), new Email(result.Email)), new UserSocialNetworks(telegramId));
    }
}
