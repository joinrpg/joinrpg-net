using System.Data.Entity;
using System.Diagnostics;
using System.Linq.Expressions;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
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
        Debug.WriteLine($"{nameof(LoadProjectClaimsAndComments)} started");
        return await Ctx
          .ClaimSet
          .Include(c => c.AccommodationRequest)
          .Include(c => c.Player)
          .Include(c => c.FinanceOperations)
          .Include(c => c.Character)
          .Where(predicate)
          .ToListAsync();
    }

    public async Task<IEnumerable<Claim>> GetClaimsByIds(int projectid, IReadOnlyCollection<int> claimindexes)
    {
        await LoadProjectFields(projectid);
        return
          await Ctx.ClaimSet.Include(c => c.Project.ProjectAcls.Select(pa => pa.User))
            .Include(c => c.Player)
            .Where(c => claimindexes.Contains(c.ClaimId) && c.ProjectId == projectid)
            .ToListAsync();
    }

    public Task<IReadOnlyCollection<Claim>> GetClaimsForMaster(int projectId, int userId, ClaimStatusSpec status) => GetClaimsImpl(projectId, status, ClaimPredicates.GetResponsible(userId));

    public Task<Claim> GetClaim(int projectId, int? claimId) => GetClaimImpl(e => e.ClaimId == claimId && e.ProjectId == projectId);

    public Task<Claim> GetClaim(ClaimIdentification claimId) => GetClaim(projectId: claimId.ProjectId, claimId: claimId.ClaimId);

    public async Task<IReadOnlyCollection<ClaimCountByMaster>> GetClaimsCountByMasters(int projectId, ClaimStatusSpec claimStatusSpec)
    {
        return await Ctx.Set<Claim>().Where(claim => claim.ProjectId == projectId)
          .Where(ClaimPredicates.GetClaimStatusPredicate(claimStatusSpec)).GroupBy(claim => claim.ResponsibleMasterUserId)
          .Select(grouping => new ClaimCountByMaster() { ClaimCount = grouping.Count(), MasterId = grouping.Key })
          .ToListAsync();
    }

    public async Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(int projectId, ClaimStatusSpec claimStatusSpec)
    {
        var query = Ctx.ClaimSet
              .Where(ClaimPredicates.GetClaimStatusPredicate(claimStatusSpec))
              .Where(c => c.ProjectId == projectId);
        return await MapToClaimWithPlayer(query);
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

    private Task<Claim> GetClaimImpl(Expression<Func<Claim, bool>> predicate)
    {
        return
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

    public async Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimsHeadersForPlayer(int projectId, ClaimStatusSpec claimStatusSpec, int userId)
    {
        var query = Ctx.ClaimSet
          .Where(ClaimPredicates.GetClaimStatusPredicate(claimStatusSpec))
          .Where(ClaimPredicates.GetForUser(userId))
          .Where(c => c.ProjectId == projectId);
        return await MapToClaimWithPlayer(query);
    }

    private static async Task<IReadOnlyCollection<ClaimWithPlayer>> MapToClaimWithPlayer(IQueryable<Claim> query)
    {
        var result = await query
          .Select(claim => new
          {
              claim.ProjectId,
              claim.Player,
              claim.Character.CharacterName,
              claim.Player.Extra!.Nicknames,
              claim.ClaimId,
          })
          .ToListAsync();

        return [.. result.Select(
            c => new ClaimWithPlayer
            {
                CharacterName = c.CharacterName,
                ClaimId = c.ClaimId,
                ExtraNicknames = c.Nicknames,
                Player = c.Player,
                ProjectId = new (c.ProjectId)
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
        .And(claim => claim.PlayerUserId == userId.Value)
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
}
