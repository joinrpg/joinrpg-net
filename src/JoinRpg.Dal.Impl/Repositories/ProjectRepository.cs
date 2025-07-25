using System.Collections.Immutable;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Linq.Expressions;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

internal class ProjectRepository(MyDbContext ctx) : GameRepositoryImplBase(ctx), IProjectRepository
{
    private Expression<Func<Project, ProjectWithClaimCount>> GetProjectWithClaimCountBuilder(
        int? userId)
    {
        var activeClaimPredicate = ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active);
        var activeOrOnHoldClaimPredicate = ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active);
        var myClaim = userId == null ? claim => false : ClaimPredicates.GetForUser(userId.Value);
        return project => new ProjectWithClaimCount()
        {
            ProjectId = project.ProjectId,
            Active = project.Active,
            PublishPlot = project.Details.PublishPlot,
            ProjectName = project.ProjectName,
            IsAcceptingClaims = project.IsAcceptingClaims,
            ActiveClaimsCount = project.Claims.Count(claim => activeClaimPredicate.Invoke(claim)),
            HasMyClaims = project.Claims.Where(claim => activeOrOnHoldClaimPredicate.Invoke(claim)).Any(claim => myClaim.Invoke(claim)),
            HasMasterAccess = project.ProjectAcls.Any(acl => acl.UserId == userId),
        };
    }

    public async Task<IReadOnlyCollection<ProjectWithClaimCount>>
        GetActiveProjectsWithClaimCount(int? userId) => await GetProjectWithClaimCounts(ProjectPredicates.Active(), userId);

    private async Task<IReadOnlyCollection<ProjectWithClaimCount>> GetProjectWithClaimCounts(
        Expression<Func<Project, bool>> condition,
        int? userId)
    {
        var builder = GetProjectWithClaimCountBuilder(userId);
        return await AllProjects
            .AsExpandable()
            .Where(condition)
            .Select(builder).ToListAsync();
    }

    public async Task<IReadOnlyCollection<ProjectWithClaimCount>>
        GetArchivedProjectsWithClaimCount(int? userId)
        => await GetProjectWithClaimCounts(ProjectPredicates.Status(ProjectLifecycleStatus.Archived), userId);

    public async Task<IReadOnlyCollection<ProjectWithClaimCount>> GetAllProjectsWithClaimCount(
        int? userId)
        => await GetProjectWithClaimCounts(project => true, userId);
    private IQueryable<Project> AllProjects => Ctx.ProjectsSet.Include(p => p.ProjectAcls);

    public Task<Project> GetProjectAsync(int project) => AllProjects.SingleOrDefaultAsync(p => p.ProjectId == project);

    public Task<Project> GetProjectWithDetailsAsync(int project)
      => AllProjects
        .Include(p => p.Details)
        .Include(p => p.ProjectAcls.Select(a => a.User))
        .SingleOrDefaultAsync(p => p.ProjectId == project);

    public Task<Project?> GetProjectWithFieldsAsync(int project) => ProjectLoaderCommon.GetProjectWithFieldsAsync(Ctx, project, skipCache: false);

    public Task<CharacterGroup?> GetGroupAsync(int projectId, int characterGroupId) => GetGroupAsync(new(new ProjectIdentification(projectId), characterGroupId));

    public async Task<CharacterGroup?> GetGroupAsync(CharacterGroupIdentification characterGroupId)
    {
        return
          await Ctx.Set<CharacterGroup>()
            .Include(cg => cg.Project)
            .SingleOrDefaultAsync(cg => cg.CharacterGroupId == characterGroupId.CharacterGroupId && cg.ProjectId == characterGroupId.ProjectId);
    }

    public async Task<CharacterGroup?> LoadGroupWithTreeAsync(int projectId, int? characterGroupId)
    {
        await LoadProjectCharactersAndGroups(projectId);
        await LoadMasters(projectId);
        await LoadProjectClaims(projectId);
        await LoadProjectFields(projectId);

        var project = await Ctx.ProjectsSet
          .Include(p => p.Details)
          .SingleOrDefaultAsync(p => p.ProjectId == projectId);

        if (characterGroupId != null)
        {
            return project.CharacterGroups.SingleOrDefault(
              cg => cg.CharacterGroupId == characterGroupId);
        }
        else
        {
            return project.RootGroup;
        }
    }

    public async Task<CharacterGroupHeaderDto[]> LoadDirectChildGroupHeaders(CharacterGroupIdentification characterGroupId)
    {
        var query =
            from characterGroup in Ctx.Set<CharacterGroup>()
            where characterGroup.ProjectId == characterGroupId.ProjectId && characterGroup.ParentGroupsImpl.ListIds.Contains(characterGroupId.CharacterGroupId.ToString())
            where !characterGroup.IsSpecial && characterGroup.IsActive
            select characterGroup;

        var list = await query.ToListAsync();
        return [.. list.Where(g => g.ParentCharacterGroupIds.Contains(characterGroupId.CharacterGroupId))
            .Select(g => new CharacterGroupHeaderDto(new CharacterGroupIdentification(new ProjectIdentification(g.ProjectId), g.CharacterGroupId), g.CharacterGroupName, g.IsActive, g.IsPublic))];
    }

    public async Task<CharacterGroup> LoadGroupWithChildsAsync(int projectId, int characterGroupId)
    {
        await LoadProjectCharactersAndGroups(projectId);
        return
          await Ctx.Set<CharacterGroup>()
            .SingleOrDefaultAsync(cg => cg.CharacterGroupId == characterGroupId && cg.ProjectId == projectId);
    }

    public async Task<IList<CharacterGroup>> LoadGroups(int projectId, IReadOnlyCollection<int> groupIds) => await Ctx.Set<CharacterGroup>().Where(cg => cg.ProjectId == projectId && groupIds.Contains(cg.CharacterGroupId)).ToListAsync();

    public async Task<IList<CharacterGroup>> LoadGroups(IReadOnlyCollection<CharacterGroupIdentification> groupIds)
    {
        if (groupIds.Count == 0)
        {
            return [];
        }
        var projectId = groupIds.First().ProjectId;
        foreach (var g in groupIds)
        {
            if (g.ProjectId != projectId)
            {
                throw new ArgumentException("Нельзя смешивать разные проекты в запросе!", nameof(groupIds));
            }
        }
        return await LoadGroups(projectId, [.. groupIds.Select(x => x.CharacterGroupId)]);
    }

    public Task<ProjectField> GetProjectField(ProjectFieldIdentification id) => GetProjectField(id.ProjectId, id.ProjectFieldId);
    public Task<ProjectField> GetProjectField(int projectId, int projectCharacterFieldId)
    {
        return Ctx.Set<ProjectField>()
          .Include(f => f.Project)
          .Include(f => f.DropdownValues)
          .SingleOrDefaultAsync(f => f.ProjectFieldId == projectCharacterFieldId && f.ProjectId == projectId);
    }

    public Task<ProjectFieldDropdownValue> GetFieldValue(ProjectFieldIdentification id, int variantId) => GetFieldValue(id.ProjectId, id.ProjectFieldId, variantId);
    public async Task<ProjectFieldDropdownValue> GetFieldValue(int projectId, int projectFieldId, int projectCharacterFieldDropdownValueId)
    {
        return await Ctx.Set<ProjectFieldDropdownValue>()
          .Include(f => f.Project)
          .Include(f => f.ProjectField)
          .SingleOrDefaultAsync(
            f =>
              f.ProjectId == projectId &&
              f.ProjectFieldId == projectFieldId &&
              f.ProjectFieldDropdownValueId == projectCharacterFieldDropdownValueId);
    }

    public Task<Project> GetProjectWithFinances(int projectid)
      => Ctx.ProjectsSet.Include(f => f.Claims)
        .Include(f => f.FinanceOperations)
        .Include(p => p.FinanceOperations.Select(fo => fo.Comment.Author))
        .SingleOrDefaultAsync(p => p.ProjectId == projectid);

    public Task<Project> GetProjectForFinanceSetup(int projectid)
      => Ctx.ProjectsSet.Include(f => f.PaymentTypes)
        .Include(p => p.Details)
        .Include(p => p.ProjectAcls)
        .Include(p => p.ProjectFeeSettings)
        .Include(p => p.FinanceOperations)
        .SingleOrDefaultAsync(p => p.ProjectId == projectid);

    public async Task<CharacterGroup> LoadGroupWithTreeSlimAsync(int projectId)
    {
        var project1 = await Ctx.ProjectsSet
          .Include(p => p.CharacterGroups)
          .Include(p => p.Characters)
          .Include(p => p.Details)
          .Where(p => p.ProjectId == projectId)
          .SingleOrDefaultAsync(p => p.ProjectId == projectId);

        return project1.RootGroup;
    }

    public async Task<IReadOnlyCollection<ProjectWithUpdateDateDto>> GetStaleProjects(
        DateTime inActiveSince)
    {
        var allQuery =
            from beforeFilter in GetProjectWithLastUpdateQuery()
            where beforeFilter.LastUpdated < inActiveSince
            orderby beforeFilter.LastUpdated ascending
            select beforeFilter;

        return await allQuery.ToListAsync();
    }

    private IQueryable<ProjectWithUpdateDateDto> GetProjectWithLastUpdateQuery()
    {
        var commentQuery = GetLastUpdateQuery<Comment>(comment => comment.LastEditTime);
        var characterQuery = GetLastUpdateQuery<Character>(character => character.UpdatedAt);
        var characterGroupQuery = GetLastUpdateQuery<CharacterGroup>(group => group.UpdatedAt);
        var plotQuery = GetLastUpdateQuery<PlotFolder>(pf => pf.ModifiedDateTime);
        var plotElementQuery = GetLastUpdateQuery<PlotElement>(pe => pe.ModifiedDateTime);
        var claimQuery = GetLastUpdateQuery<Claim>(pfs => pfs.LastUpdateDateTime);

        return
            from updated in
                commentQuery
                    .Union(characterQuery)
                    .Union(characterGroupQuery)
                    .Union(plotQuery)
                    .Union(plotElementQuery)
                    .Union(claimQuery)
            group updated by new { updated.ProjectId, updated.ProjectName }
            into gr
            select new ProjectWithUpdateDateDto()
            {
                ProjectId = gr.Key.ProjectId,
                ProjectName = gr.Key.ProjectName,
                LastUpdated = gr.Max(g => g.LastUpdated),
            };
    }

    private IQueryable<ProjectWithUpdateDateDto> GetLastUpdateQuery<T>(
        Expression<Func<T, DateTime>> lastUpdateExpression) where T : class, IProjectEntity
    {
        return from entity in Ctx.Set<T>().AsExpandable()
               where entity.Project.Active
               group new { entity } by entity.Project
            into gr
               select new ProjectWithUpdateDateDto
               {
                   ProjectId = gr.Key.ProjectId,
                   ProjectName = gr.Key.ProjectName,
                   LastUpdated = gr.Max(g => lastUpdateExpression.Invoke(g.entity)),
               };
    }

    public async Task<ICollection<Character>> GetCharacterByGroups(int projectId, int[] characterGroupIds)
    {
        await LoadProjectFields(projectId);
        await LoadProjectCharactersAndGroups(projectId);
        await LoadMasters(projectId);
        await LoadProjectClaims(projectId);

        var result =
          await Ctx.Set<Character>().Where(
            character => character.ProjectId == projectId &&
              characterGroupIds.Any(id => SqlFunctions.CharIndex(id.ToString(), character.ParentGroupsImpl.ListIds) > 0)).ToListAsync();
        return result.Where(ch => ch.ParentCharacterGroupIds.Intersect(characterGroupIds).Any()).ToList();
    }



    async Task<ProjectHeaderDto[]> IProjectRepository.GetProjectsBySpecification(UserIdentification userIdentification, ProjectListSpecification projectListSpecification)
    {
        var filterPredicate = ProjectPredicates.BySpecification(userIdentification, projectListSpecification);
        var masterPredicate = ProjectPredicates.MasterAccess(userIdentification);
        var claimPredicate = ProjectPredicates.HasActiveClaim(userIdentification);
        var query = from project in AllProjects.AsExpandable()
                    where filterPredicate.Compile()(project)
                    select new
                    {
                        project.ProjectId,
                        project.ProjectName,
                        IAmMaster = masterPredicate.Compile()(project),
                        HasActiveClaims = claimPredicate.Compile()(project),
                    };

        var result = await query.ToListAsync();

        return result.Select(x => new ProjectHeaderDto(new(x.ProjectId), x.ProjectName, x.IAmMaster, x.HasActiveClaims)).ToArray();
    }
}

