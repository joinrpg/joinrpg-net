using System.Data.Entity;
using System.Linq.Expressions;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Subscribe;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.Users;
using LinqKit;

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
          .Include(u => u.ExternalLogins)
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
        var activeclaimsPredicate = ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active);
        var activeProjectsPredicate = ProjectPredicates.MasterAccess(userId);
        var userQuery =
            from user in ctx.Set<User>().AsExpandable()
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
                user.Extra.Telegram,
                Claims = user.Claims.Where(claim => activeclaimsPredicate.Invoke(claim)).Select(claim => new { claim.ClaimId, claim.ProjectId }),
                Projects = user.ProjectAcls.Where(acl => activeProjectsPredicate.Invoke(acl.Project)).Select(acl => new { acl.ProjectId, acl.Project.Active }),
                user.Auth.IsAdmin,
                user.Extra!.Livejournal,
                AllRpgInfoId = user.Allrpg!.Sid,
                user.Extra.Vk,
                user.Extra.SocialNetworksAccess,
                user.SelectedAvatarId,
                user.VerifiedProfileFlag,
                user.Extra.PhoneNumber,
                user.Auth.EmailConfirmed,
            };

        var result = await userQuery.SingleOrDefaultAsync();
        if (result is null)
        {
            return null;
        }

        var telegramId = TelegramId.FromOptional(result.ExternalLogins.SingleOrDefault(x => x.Provider == UserExternalLogin.TelegramProvider)?.Key, new PrefferedName(result.Telegram));

        var userFullName =
            new UserFullName(
                PrefferedName.FromOptional(result.PrefferedName),
            BornName.FromOptional(result.BornName),
            SurName.FromOptional(result.SurName),
            FatherName.FromOptional(result.FatherName));
        return new UserInfo(
            new UserIdentification(result.UserId),
            new UserSocialNetworks(telegramId, result.Livejournal, result.AllRpgInfoId, result.Vk, result.SocialNetworksAccess),
            result.Claims.Select(c => new ClaimIdentification(c.ProjectId, c.ClaimId)).ToList(),
            result.Projects.Where(p => p.Active).Select(p => new ProjectIdentification(p.ProjectId)).ToList(),
            result.Projects.Select(p => new ProjectIdentification(p.ProjectId)).ToList(),
            result.IsAdmin,
            AvatarIdentification.FromOptional(result.SelectedAvatarId),
            new Email(result.Email),
            result.EmailConfirmed,
            userFullName,
            result.VerifiedProfileFlag,
            result.PhoneNumber
            );
    }

    public async Task<IReadOnlyCollection<UserInfoHeader>> GetUserInfoHeaders(IReadOnlyCollection<UserIdentification> userIds)
    {
        var ids = userIds.Select(x => x.Value).ToList();
        Func<User, bool> predicate = user => ids.Contains(user.UserId);
        return await GetUserInfoHeadersByPredicate(predicate);
    }

    private async Task<IReadOnlyCollection<UserInfoHeader>> GetUserInfoHeadersByPredicate(Func<User, bool> predicate)
    {
        var userQuery =
            from user in ctx.Set<User>().AsExpandable()
            where predicate.Invoke(user)
            select new
            {
                user.UserId,
                user.PrefferedName,
                user.FatherName,
                user.SurName,
                user.BornName,
                user.Email,
            };

        var list = await userQuery.ToListAsync();

        return list.Select(result =>
            new UserInfoHeader(
                new UserIdentification(result.UserId),
                new UserDisplayName(
                    new UserFullName(
                    PrefferedName.FromOptional(result.PrefferedName),
                BornName.FromOptional(result.BornName),
                SurName.FromOptional(result.SurName),
                FatherName.FromOptional(result.FatherName)),
                    new Email(result.Email))
                )
        ).ToList();
    }
}
