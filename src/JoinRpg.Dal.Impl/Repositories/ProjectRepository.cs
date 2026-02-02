using System.Collections.Immutable;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

internal class ProjectRepository(MyDbContext ctx) : GameRepositoryImplBase(ctx), IProjectRepository
{
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
        (var projectId, var ids) = EnsureSingleProject(groupIds);
        return await LoadGroups(projectId, ids);
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

    public async Task<ICollection<Character>> GetCharacterByGroups(IReadOnlyCollection<CharacterGroupIdentification> characterGroupIdentifications)
    {
        if (characterGroupIdentifications.Count == 0)
        {
            return [];
        }
        var projectId = characterGroupIdentifications.EnsureSameProject().First().ProjectId;
        await LoadProjectFields(projectId); //TODO Remove
        await LoadProjectCharactersAndGroups(projectId);
        await LoadMasters(projectId); //TODO Remove
        await LoadProjectClaims(projectId);

        var result =
          await Ctx.Set<Character>().Where(CharacterPredicates.ByGroupImprecise(characterGroupIdentifications)).ToListAsync();
        return result.Where(CharacterPredicates.ByGroupPrecise(characterGroupIdentifications)).ToList();
    }

    async Task<ProjectPersonalizedInfo[]> IProjectRepository.GetPersonalizedProjectsBySpecification(UserIdentification? userId, ProjectListSpecification projectListSpecification)
    {
        var filterPredicate = ProjectPredicates.BySpecification(userId, projectListSpecification);
        return await GetProjectPersonalizedListInternal(userId, filterPredicate);
    }

    async Task<ProjectShortInfo[]> IProjectRepository.GetProjectsBySpecification(ProjectListSpecification projectListSpecification)
    {
        var filterPredicate = ProjectPredicates.BySpecification(userInfoId: null, projectListSpecification);
        return await GetProjectListInternal(filterPredicate);
    }

    Task<ProjectPersonalizedInfo[]> IProjectRepository.GetProjectsByIds(UserIdentification? userId, ProjectIdentification[] ids)
    {
        var idArray = ids.Select(id => id.Value).ToArray();
        return GetProjectPersonalizedListInternal(userId, project => idArray.Contains(project.ProjectId));
    }
    private async Task<ProjectShortInfo[]> GetProjectListInternal(Expression<Func<Project, bool>> filterPredicate)
    {
        var activeClaimPredicate = ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active);

        var query = from project in AllProjects.AsExpandable()
                    join update in GetProjectWithLastUpdateQuery() on project.ProjectId equals update.ProjectId
                    where filterPredicate.Compile()(project)
                    select new
                    {
                        project.ProjectId,
                        project.ProjectName,
                        project.IsAcceptingClaims,
                        project.Details.PublishPlot,
                        project.Active,
                        update.LastUpdated,
                        ActiveClaimsCount = project.Claims.Count(claim => activeClaimPredicate.Invoke(claim)),
                        project.KogdaIgraGames,
                        project.Details.DisableKogdaIgraMapping,
                    };

        var result = await query.ToListAsync();

        return [.. result.Select(x => new ProjectShortInfo(
            new(x.ProjectId),
            ProjectLoaderCommon.CreateStatus(x.Active, x.IsAcceptingClaims),
            x.PublishPlot,
            new(x.ProjectName),
            x.ActiveClaimsCount,
            DateOnly.FromDateTime(x.LastUpdated),
            KiLinks:
                (x.DisableKogdaIgraMapping, x.KogdaIgraGames.Count) switch{
                    (true, _) => [],
                    (false, 0 ) => null,
                    (false, _) => [..x.KogdaIgraGames.Select(KogdaIgraRepository.TryConvert).WhereNotNull()],
                }
            ))];
    }

    private async Task<ProjectPersonalizedInfo[]> GetProjectPersonalizedListInternal(UserIdentification? userId, Expression<Func<Project, bool>> filterPredicate)
    {
        var masterPredicate = userId is null ? project => false : ProjectPredicates.MasterAccess(userId);
        var claimPredicate = userId is null ? project => false : ProjectPredicates.HasActiveClaim(userId);

        var activeClaimPredicate = ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active);

        var query = from project in AllProjects.AsExpandable()
                    where filterPredicate.Compile()(project)
                    select new
                    {
                        project.ProjectId,
                        project.ProjectName,
                        project.IsAcceptingClaims,
                        project.Details.PublishPlot,
                        project.Active,
                        IAmMaster = masterPredicate.Compile()(project),
                        HasMyClaims = claimPredicate.Compile()(project),
                        ActiveClaimsCount = project.Claims.Count(claim => activeClaimPredicate.Invoke(claim)),
                        LastKogdaIgraId = (int?)project.KogdaIgraGames.OrderByDescending(x => x.KogdaIgraGameId).FirstOrDefault()!.KogdaIgraGameId
                    };

        var result = await query.ToListAsync();

        return [.. result.Select(x => new ProjectPersonalizedInfo(
            new(x.ProjectId),
            ProjectLoaderCommon.CreateStatus(x.Active, x.IsAcceptingClaims),
            x.PublishPlot,
            new(x.ProjectName),
            x.ActiveClaimsCount,
            x.HasMyClaims,
            x.IAmMaster,
            KogdaIgraIdentification.FromOptional(x.LastKogdaIgraId)
            ))];
    }

    async Task<CharacterGroupHeaderDto[]> IProjectRepository.GetGroupHeaders(IReadOnlyCollection<CharacterGroupIdentification> characterGroupIds)
    {
        if (characterGroupIds.Count == 0)
        {
            return [];
        }
        (var projectId, var ids) = EnsureSingleProject(characterGroupIds);
        var list =
            await Ctx.Set<CharacterGroup>().Where(cg => cg.ProjectId == projectId && ids.Contains(cg.CharacterGroupId))
            .Select(
                x => new
                {
                    x.CharacterGroupId,
                    x.CharacterGroupName,
                    x.IsPublic,
                    x.IsActive,
                }
                )
            .ToListAsync();

        return [..
            list.Select(x => new CharacterGroupHeaderDto(new CharacterGroupIdentification(projectId, x.CharacterGroupId), x.CharacterGroupName, x.IsActive, x.IsPublic))
            ];

    }

    private static (ProjectIdentification, IReadOnlyCollection<int>) EnsureSingleProject(IReadOnlyCollection<CharacterGroupIdentification> groupIds)
    {
        var projectId = groupIds.First().ProjectId;
        List<int> ids = new List<int>(groupIds.Count);
        foreach (var g in groupIds)
        {
            if (g.ProjectId != projectId)
            {
                throw new ArgumentException("Нельзя смешивать разные проекты в запросе!", nameof(groupIds));
            }
            ids.Add(g.CharacterGroupId);
        }

        return (projectId, ids);
    }
}
