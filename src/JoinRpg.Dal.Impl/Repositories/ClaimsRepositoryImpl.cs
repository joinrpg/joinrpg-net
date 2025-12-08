using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.Users;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

internal class ClaimsRepositoryImpl(MyDbContext ctx) : GameRepositoryImplBase(ctx), IClaimsRepository
{
    public Task<IReadOnlyCollection<Claim>> GetClaims(int projectId, ClaimStatusSpec status) => GetClaimsImpl(projectId, status, claim => true);

    public async Task<IReadOnlyCollection<Claim>> GetClaimsForMoneyTransfersListAsync(int projectId, ClaimStatusSpec claimStatusSpec)
    {
        return await Ctx
            .ClaimSet
            .AsNoTracking()
            .Include(c => c.Player)
            .Where(ClaimPredicates.GetClaimStatusPredicate(claimStatusSpec))
            .Where(c => c.ProjectId == projectId)
            .ToArrayAsync();
    }

    private async Task<IReadOnlyCollection<Claim>> GetClaimsImpl(int projectId, ClaimStatusSpec status, Expression<Func<Claim, bool>> predicate)
    {
        await LoadProjectFields(projectId);
        await LoadProjectGroups(projectId);
        await LoadMasters(projectId);

        var predicateBuilder = PredicateBuilder.New<Claim>()
            .And(claim => claim.ProjectId == projectId)
            .And(predicate)
            .And(ClaimPredicates.GetClaimStatusPredicate(status));

        return await GetClaimsImpl(predicateBuilder);
    }

    private async Task<IReadOnlyCollection<Claim>> GetClaimsImpl(Expression<Func<Claim, bool>> predicate)
    {
        return await Ctx
          .ClaimSet
          .Include(c => c.AccommodationRequest)
          .Include(c => c.Player.Extra)
          .Include(c => c.FinanceOperations)
          .Include(c => c.Character)
          .Where(predicate)
          .ToListAsync();
    }


    public Task<IReadOnlyCollection<Claim>> GetClaimsForMaster(int projectId, int userId, ClaimStatusSpec status) => GetClaimsImpl(projectId, status, ClaimPredicates.GetResponsible(userId));

    public Task<Claim?> GetClaim(int projectId, int? claimId) => GetClaimImpl(e => e.ClaimId == claimId && e.ProjectId == projectId);

    public Task<Claim?> GetClaim(ClaimIdentification claimId) => GetClaim(projectId: claimId.ProjectId, claimId: claimId.ClaimId);

    public async Task<IReadOnlyCollection<ClaimCountByMaster>> GetClaimsCountByMasters(int projectId, ClaimStatusSpec claimStatusSpec)
    {
        return await Ctx.Set<Claim>().Where(claim => claim.ProjectId == projectId)
          .Where(ClaimPredicates.GetClaimStatusPredicate(claimStatusSpec)).GroupBy(claim => claim.ResponsibleMasterUserId)
          .Select(grouping => new ClaimCountByMaster() { ClaimCount = grouping.Count(), MasterId = grouping.Key })
          .ToListAsync();
    }

    public async Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(int projectId, ClaimStatusSpec claimStatusSpec)
    {
        return await GetHeadersByPredicate(new(projectId), ClaimPredicates.GetClaimStatusPredicate(claimStatusSpec));
    }

    public async Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(ProjectIdentification projectId, ClaimStatusSpec claimStatusSpec)
    {
        return await GetHeadersByPredicate(projectId, ClaimPredicates.GetClaimStatusPredicate(claimStatusSpec));
    }

    async Task<IReadOnlyCollection<ClaimWithPlayer>> IClaimsRepository.GetClaimHeadersWithPlayer(IReadOnlyCollection<ClaimIdentification> claimIds)
    {
        if (claimIds.Count == 0)
        {
            return [];
        }
        var projectId = claimIds.First().ProjectId;
        var idList = claimIds.EnsureSameProject().Select(c => c.ClaimId).ToArray();
        return await GetHeadersByPredicate(projectId, claim => idList.Contains(claim.ClaimId));
    }

    public Task<IReadOnlyCollection<Claim>> GetClaimsForRoomType(int projectId, ClaimStatusSpec claimStatusSpec, int? roomTypeId)
    {
        if (roomTypeId != null)
        {
            return GetClaimsImpl(projectId,
                claimStatusSpec,
                claim => claim.AccommodationRequest!.AccommodationTypeId == roomTypeId);
        }
        else
        {
            return GetClaimsImpl(projectId,
                claimStatusSpec,
                claim => claim.AccommodationRequest == null);
        }
    }

    private async Task<Claim?> GetClaimImpl(Expression<Func<Claim, bool>> predicate)
    {
        return
            await
          Ctx.ClaimSet.Include(c => c.Project)
            .Include(c => c.Project.ProjectAcls)
            .Include(c => c.Character)
            .Include(c => c.Player)
            .Include(c => c.Player.Claims)
            .SingleOrDefaultAsync(predicate);
    }


    public async Task<Claim> GetClaimWithDetails(int projectId, int claimId)
    {
        await LoadProjectFields(projectId);
        await LoadMasters(projectId);
        await LoadProjectGroups(projectId);
        await LoadProjectClaims(projectId);

        return
          await
            Ctx.ClaimSet.Include(c => c.CommentDiscussion.Comments.Select(com => com.Finance))
              .Include(c => c.CommentDiscussion.Comments.Select(com => com.Author))
              .Include(c => c.CommentDiscussion.Comments.Select(com => com.CommentText))
              .Include(c => c.AccommodationRequest)
              .Include(c => c.Character)
              .SingleOrDefaultAsync(e => e.ClaimId == claimId && e.ProjectId == projectId);
    }

    public Task<IReadOnlyCollection<Claim>> GetClaimsForGroups(int projectId, ClaimStatusSpec active, int[] characterGroupsIds)
    {
        return GetClaimsImpl(projectId, active, ClaimPredicates.GetInGroupPredicate(characterGroupsIds));
    }
    public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(int projectId, ClaimStatusSpec claimStatusSpec, int userId)
        => GetClaimsImpl(projectId, claimStatusSpec, ClaimPredicates.GetForUser(userId));

    public async Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimsHeadersForPlayer(ProjectIdentification projectId, ClaimStatusSpec claimStatusSpec, UserIdentification userId)
    {
        var query = Ctx.ClaimSet
          .Where(ClaimPredicates.GetClaimStatusPredicate(claimStatusSpec))
          .Where(ClaimPredicates.GetForUser(userId))
          .Where(c => c.ProjectId == projectId);
        return await MapToClaimWithPlayer(query);
    }

    private async Task<IReadOnlyCollection<ClaimWithPlayer>> GetHeadersByPredicate(ProjectIdentification projectId, Expression<Func<Claim, bool>> predicate)
    {
        var query = Ctx.ClaimSet
              .Where(predicate)
              .Where(c => c.ProjectId == projectId);
        return await MapToClaimWithPlayer(query);
    }

    private static async Task<IReadOnlyCollection<ClaimWithPlayer>> MapToClaimWithPlayer(IQueryable<Claim> query)
    {
        var result = await query
          .Select(claim => new
          {
              claim.ProjectId,
              claim.PlayerUserId,
              claim.Character.CharacterName,
              claim.Player.Extra!.Nicknames,
              claim.Player.PrefferedName,
              claim.Player.SurName,
              claim.Player.BornName,
              claim.Player.FatherName,
              claim.ClaimId,
              claim.Player.Email,
              claim.ResponsibleMasterUserId,
              claim.CharacterId
          })
          .ToListAsync();

        return [.. result.Select(
            c => new ClaimWithPlayer
            {
                CharacterName = c.CharacterName,
                ClaimId = new (c.ProjectId, c.ClaimId),
                ExtraNicknames = c.Nicknames,
                ResponsibleMasterUserId = new( c.ResponsibleMasterUserId),
                Player = new UserInfoHeader(new (c.PlayerUserId),
                                new UserDisplayName(
                                    new UserFullName(
                                        PrefferedName.FromOptional(c.PrefferedName),
                                        BornName.FromOptional(c.BornName),
                                        SurName.FromOptional(c.SurName),
                                        FatherName.FromOptional(c.FatherName)
                                        ),
                                    new Email(c.Email)
                                    )),
                CharacterId = new CharacterIdentification(c.ProjectId, c.CharacterId),
            }
            )];
    }

    public Task<Dictionary<int, int>> GetUnreadDiscussionsForClaims(int projectId, ClaimStatusSpec claimStatusSpec, int userId, bool hasMasterAccess)
    {
        var claims = Ctx.Set<Claim>()
            .Where(claim => claim.ProjectId == projectId)
          .Where(ClaimPredicates.GetClaimStatusPredicate(claimStatusSpec));

        var query =
            from claim in claims
            let lastMyCommentId = claim.CommentDiscussion.Comments.Where(comment => comment.AuthorUserId == userId).Max(comment => (int?)comment.CommentId) ?? 0
            let lastWatermark = claim.CommentDiscussion.Watermarks.Where(watermark => watermark.UserId == userId).Max(watermark => (int?)watermark.CommentId) ?? 0
            let comments = claim.CommentDiscussion.Comments.Where(comment => comment.IsVisibleToPlayer || hasMasterAccess)
            let unreadComments = comments.Where(comment => comment.CommentId > lastMyCommentId && comment.CommentId > lastWatermark)
            select
            new
            {
                claim.CommentDiscussionId,
                UnreadCommentsCount = unreadComments.Count()
            };

        return query.ToDictionaryAsync(x => x.CommentDiscussionId, x => x.UnreadCommentsCount);
    }

    public async Task<IReadOnlyCollection<UpdatedClaimDto>> GetUpdatedClaimsSince(DateTimeOffset since)
    {
        var query =
            from claim in Ctx.Set<Claim>()
            where claim.CommentDiscussion.Comments.Any(comment => comment.LastEditTime > since.UtcDateTime)
            select new { claim.ClaimId, claim.ProjectId, claim.Project.ProjectName, claim.Character.CharacterName, claim.PlayerUserId };
        var result = await query.ToListAsync();
        return [.. result.Select(x =>
        new UpdatedClaimDto(new ClaimIdentification(x.ProjectId, x.ClaimId), new UserIdentification(x.PlayerUserId), new(x.ProjectName), x.CharacterName))];
    }

    public async Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(UserIdentification userId, ClaimStatusSpec status)
    {
        var predicateBuilder = PredicateBuilder.New<Claim>()
        .And(ClaimPredicates.GetForUser(userId))
        .And(ClaimPredicates.GetClaimStatusPredicate(status));

        return await Ctx
          .ClaimSet
          .Include(c => c.AccommodationRequest)
          .Include(c => c.Player)
          .Include(c => c.FinanceOperations)
          .Include(c => c.Character)
          .Include(c => c.Project)
          .Where(predicateBuilder)
          .ToListAsync();
    }

    public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(ProjectIdentification projectId, UserIdentification userId, ClaimStatusSpec status)
    {
        var predicateBuilder = PredicateBuilder.New<Claim>()
            .And(ClaimPredicates.GetForUser(userId))
            .And(ClaimPredicates.GetForProject(projectId))
            .And(ClaimPredicates.GetClaimStatusPredicate(status));

        return GetClaimsImpl(predicateBuilder);
    }

    public async Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(IReadOnlyCollection<CharacterGroupIdentification> characterGroupsIds, ClaimStatusSpec spec)
    {
        var query = Ctx.ClaimSet
            .AsExpandable()
            .Where(ClaimPredicates.GetInGroupPredicate(characterGroupsIds))
            .Where(ClaimPredicates.GetClaimStatusPredicate(spec));

        return await MapToClaimWithPlayer(query);
    }
}
