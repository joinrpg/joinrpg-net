using JoinRpg.Data.Interfaces.Subscribe;
using JoinRpg.DataModel.Users;
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
        LoadSubscriptionsForProject(UserIdentification userId, ProjectIdentification projectId)
    {
        var user = await WithProfile(userId);
        UserSubscriptionDto[] subscribe = await GetSubscriptionsByPredicate(x => x.ProjectId == projectId && x.UserId == userId);
        return (user, subscribe);
    }

    public async Task<UserSubscriptionDto> LoadSubscriptionById(ProjectIdentification projectId, int subscriptionId)
    {
        var subscribe = await ctx.Set<UserSubscription>()
            .Where(x => x.ProjectId == projectId && x.UserSubscriptionId == subscriptionId)
            .Select(SubscriptionDtoBuilder())
            .FirstOrDefaultAsync();
        return subscribe;
    }

    Task<UserAvatar> IUserRepository.LoadAvatar(AvatarIdentification userAvatarId)
        => ctx.Set<User>()
            .SelectMany(user => user.Avatars)
            .SingleOrDefaultAsync(a => a.UserAvatarId == userAvatarId.Value);
    public async Task<UserInfo?> GetUserInfo(UserIdentification userId) => (await GetUserInfosByPredicate(user => user.UserId == userId.Value)).SingleOrDefault();

    public async Task<IReadOnlyCollection<UserInfoHeader>> GetUserInfoHeaders(IReadOnlyCollection<UserIdentification> userIds)
    {
        var ids = userIds.Select(x => x.Value).ToList();
        return await GetUserInfoHeadersByPredicate(user => ids.Contains(user.UserId));
    }

    private async Task<IReadOnlyCollection<UserInfoHeader>> GetUserInfoHeadersByPredicate(Expression<Func<User, bool>> predicate)
    {
        var builder = UserInfoHeaderDtoBuilder();
        var userQuery =
            from user in ctx.Set<User>().AsExpandable()
            where predicate.Invoke(user)
            select builder.Invoke(user);

        var list = await userQuery.ToListAsync();

        return [.. list.Select(x => x.ToUserInfoHeader())];
    }

    async Task<IReadOnlyCollection<UserInfo>> IUserRepository.GetUserInfos(IReadOnlyCollection<UserIdentification> userIds)
    {
        var ids = userIds.Select(x => x.Value).ToList();
        return await GetUserInfosByPredicate(user => ids.Contains(user.UserId));
    }

    public async Task<IReadOnlyCollection<UserInfo>> GetUserInfosByPredicate(Expression<Func<User, bool>> predicate)
    {
        var activeclaimsPredicate = ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active);
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
                user.ExternalLogins,
                user.Extra.Telegram,
                Claims = user.Claims.Where(claim => activeclaimsPredicate.Invoke(claim)).Select(claim => new { claim.ClaimId, claim.ProjectId }),
                Projects = user.ProjectAcls.Select(acl => new { acl.ProjectId, acl.Project.Active }),
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


        var results = await userQuery.ToListAsync();

        return [.. results.Select(result => {
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
        })];
    }

    public async Task<IReadOnlyCollection<UserInfoHeader>> GetAdminUserInfoHeaders() => await GetUserInfoHeadersByPredicate(user => user.Auth.IsAdmin);
    public async Task<IReadOnlyCollection<UserSubscribe>> GetDirect(ClaimIdentification claimId)
    {
        var builder = UserInfoHeaderDtoBuilder();
        var options = SubscriptionOptionsBuilder();
        var query =
            from x in ctx.Set<UserSubscription>().AsExpandable()
            where x.ProjectId == claimId.ProjectId && x.ClaimId == claimId.ClaimId
            select
            new
            {
                Options = options.Invoke(x),
                User = builder.Invoke(x.User),
            };
        var result = await query.ToArrayAsync();
        return [.. result.Select(x => new UserSubscribe(x.User.ToUserInfoHeader(), x.Options))];
    }
    public async Task<IReadOnlyCollection<UserSubscribe>> GetForCharAndGroups(IReadOnlyCollection<CharacterGroupIdentification> characterGroupIdentifications, CharacterIdentification characterId)
    {
        var builder = UserInfoHeaderDtoBuilder();
        var options = SubscriptionOptionsBuilder();
        var groupIds = characterGroupIdentifications.EnsureSameProject().Select(x => x.CharacterGroupId).ToList();
        var query =
            from x in ctx.Set<UserSubscription>().AsExpandable()
            where x.ProjectId == characterId.ProjectId && (groupIds.Contains(x.CharacterGroupId!.Value) || x.CharacterId == characterId.Id)
            select
            new
            {
                Options = options.Invoke(x),
                User = builder.Invoke(x.User),
            };
        var result = await query.ToArrayAsync();
        return [.. result.Select(x => new UserSubscribe(x.User.ToUserInfoHeader(), x.Options))];
    }

    private async Task<UserSubscriptionDto[]> GetSubscriptionsByPredicate(Expression<Func<UserSubscription, bool>> predicate)
    {
        return await ctx.Set<UserSubscription>().AsExpandable()
            .Where(predicate)
            .Select(SubscriptionDtoBuilder())
            .ToArrayAsync();
    }

    private static Expression<Func<UserSubscription, UserSubscriptionDto>> SubscriptionDtoBuilder()
    {
        var inner = SubscriptionOptionsBuilder();
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
                    Options = inner.Invoke(x),
                };
    }

    private static Expression<Func<UserSubscription, SubscriptionOptions>> SubscriptionOptionsBuilder()
    {
        return x =>
                 new SubscriptionOptions
                 {
                     AccommodationChange = x.AccommodationChange,
                     ClaimStatusChange = x.ClaimStatusChange,
                     Comments = x.Comments,
                     FieldChange = x.FieldChange,
                     MoneyOperation = x.MoneyOperation,
                     AccommodationInvitesChange = x.AccommodationChange, // В базе мы пока на это не умеем подписываться
                 };
    }

    private static Expression<Func<User, UserInfoHeaderDto>> UserInfoHeaderDtoBuilder()
    {
        return user => new UserInfoHeaderDto
        {
            UserId = user.UserId,
            PrefferedName1 = user.PrefferedName,
            FatherName1 = user.FatherName,
            SurName1 = user.SurName,
            BornName1 = user.BornName,
            Email = user.Email,
        };
    }

    private class UserInfoHeaderDto
    {
        public required int UserId { get; internal set; }
        public required string? PrefferedName1 { get; internal set; }
        public required string? FatherName1 { get; internal set; }
        public required string? SurName1 { get; internal set; }
        public required string? BornName1 { get; internal set; }
        public required string Email { get; internal set; }

        internal UserInfoHeader ToUserInfoHeader()
        {
            return new UserInfoHeader(
                            new UserIdentification(UserId),
                            new UserDisplayName(
                                new UserFullName(
                                PrefferedName.FromOptional(PrefferedName1),
                            BornName.FromOptional(BornName1),
                            SurName.FromOptional(SurName1),
                            FatherName.FromOptional(FatherName1)),
                                new Email(Email))
                            );
        }
    }
}
