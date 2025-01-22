using System.Collections.Immutable;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Linq.Expressions;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.PrimitiveTypes.ProjectMetadata.Payments;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

internal class ProjectRepository(MyDbContext ctx) : GameRepositoryImplBase(ctx), IProjectRepository, IProjectMetadataRepository
{
    private Expression<Func<Project, ProjectWithClaimCount>> GetProjectWithClaimCountBuilder(
        int? userId)
    {
        var activeClaimPredicate = ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active);
        var activeOrOnHoldClaimPredicate = ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active);
        var myClaim = userId == null ? claim => false : ClaimPredicates.GetMyClaim(userId.Value);
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
        GetActiveProjectsWithClaimCount(int? userId) => await GetProjectWithClaimCounts(project => project.Active, userId);

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
        => await GetProjectWithClaimCounts(project => !project.Active, userId);

    public async Task<IReadOnlyCollection<ProjectWithClaimCount>> GetAllProjectsWithClaimCount(
        int? userId)
        => await GetProjectWithClaimCounts(project => true, userId);

    private IQueryable<Project> ActiveProjects => AllProjects.Where(project => project.Active);
    private IQueryable<Project> AllProjects => Ctx.ProjectsSet.Include(p => p.ProjectAcls);

    public async Task<IEnumerable<Project>> GetMyActiveProjectsAsync(int userInfoId) => await
      ActiveProjects.Where(MyProjectPredicate(new(userInfoId))).ToListAsync();

    public async Task<IEnumerable<Project>> GetActiveProjectsWithSchedule()
        => await ActiveProjects.Where(project => project.Details.ScheduleEnabled)
            .ToListAsync();

    public Task<Project> GetProjectAsync(int project) => AllProjects.SingleOrDefaultAsync(p => p.ProjectId == project);

    public Task<Project> GetProjectWithDetailsAsync(int project)
      => AllProjects
        .Include(p => p.ProjectAcls.Select(a => a.User))
        .SingleOrDefaultAsync(p => p.ProjectId == project);

    public async Task<Project?> GetProjectWithFieldsAsync(int project)
      => await AllProjects
        .Include(p => p.Details)
        .Include(p => p.ProjectAcls.Select(a => a.User))
        .Include(p => p.ProjectFields.Select(f => f.DropdownValues))
        .SingleOrDefaultAsync(p => p.ProjectId == project);

    public async Task<CharacterGroup?> GetGroupAsync(int projectId, int characterGroupId)
    {
        return
          await Ctx.Set<CharacterGroup>()
            .Include(cg => cg.Project)
            .SingleOrDefaultAsync(cg => cg.CharacterGroupId == characterGroupId && cg.ProjectId == projectId);
    }

    public Task<CharacterGroup> GetRootGroupAsync(int projectId)
    {
        return
         Ctx.Set<CharacterGroup>()
           .Include(cg => cg.Project)
           .SingleOrDefaultAsync(cg => cg.IsRoot && cg.ProjectId == projectId);
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

    public async Task<CharacterGroup> LoadGroupWithChildsAsync(int projectId, int characterGroupId)
    {
        await LoadProjectCharactersAndGroups(projectId);
        return
          await Ctx.Set<CharacterGroup>()
            .SingleOrDefaultAsync(cg => cg.CharacterGroupId == characterGroupId && cg.ProjectId == projectId);
    }

    private static Expression<Func<Project, bool>> MyProjectPredicate(UserIdentification userInfoId)
    {
        return project => project.ProjectAcls.Any(projectAcl => projectAcl.UserId == userInfoId.Value);
    }

    public async Task<IReadOnlyCollection<Character>> LoadCharactersWithGroups(int projectId, IReadOnlyCollection<int> characterIds)
    {
        await LoadProjectGroups(projectId);
        await LoadProjectClaimsAndComments(projectId);
        await LoadMasters(projectId);
        await LoadProjectFields(projectId);

        return
          await Ctx.Set<Character>()
            .Where(e => characterIds.Contains(e.CharacterId) && e.ProjectId == projectId).ToListAsync();
    }

    public async Task<IList<CharacterGroup>> LoadGroups(int projectId, IReadOnlyCollection<int> groupIds) => await Ctx.Set<CharacterGroup>().Where(cg => cg.ProjectId == projectId && groupIds.Contains(cg.CharacterGroupId)).ToListAsync();

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

    public async Task<ICollection<Character>> GetCharacters(int projectId)
    {
        await LoadProjectFields(projectId);
        await LoadProjectCharactersAndGroups(projectId);
        await LoadMasters(projectId);
        await LoadProjectClaims(projectId);

        return (await Ctx.Set<Project>().SingleOrDefaultAsync(p => p.ProjectId == projectId)).Characters;
    }

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

    public async Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId)
    {
        var project = await GetProjectWithFieldsAsync(projectId.Value) ?? throw new InvalidOperationException($"Project with {projectId} not found");

        return CreateInfoFromProject(project, projectId);
    }

    // This is internal to allow usage in tests
    internal static ProjectInfo CreateInfoFromProject(Project project, ProjectIdentification projectId)
    {
        var fieldSettings = new ProjectFieldSettings(
            NameField: ProjectFieldIdentification.FromOptional(projectId, project.Details.CharacterNameField?.ProjectFieldId),
            DescriptionField: ProjectFieldIdentification.FromOptional(projectId, project.Details.CharacterDescription?.ProjectFieldId)
            );

        var financeSettings = new ProjectFinanceSettings(
            project.Details.PreferentialFeeEnabled,
            project.PaymentTypes.Select(pt => new PaymentTypeInfo(pt.TypeKind, pt.IsActive, pt.UserId)).ToArray());

        return new ProjectInfo(
            projectId,
            project.ProjectName,
            project.Details.FieldsOrdering,
            CreateFields(project, fieldSettings).ToList(),
            fieldSettings,
            financeSettings,
            project.Details.EnableAccommodation,
            CharacterIdentification.FromOptional(projectId, project.Details.DefaultTemplateCharacterId),
            allowToSetGroups: project.CharacterGroups.Any(x => x.IsActive && !x.IsRoot && !x.IsSpecial),
            rootCharacterGroupId: project.RootGroup.CharacterGroupId);

        IEnumerable<ProjectFieldInfo> CreateFields(Project project, ProjectFieldSettings fieldSettings)
        {
            foreach (var field in project.ProjectFields)
            {
                var fieldId = new ProjectFieldIdentification(projectId, field.ProjectFieldId);
                yield return new ProjectFieldInfo(
                    fieldId,
                    field.FieldName,
                    field.FieldType,
                    field.FieldBoundTo,
                    CreateVariants(field, fieldId).ToList(),
                    field.ValuesOrdering,
                    field.Price,
                    field.CanPlayerEdit,
                    field.ShowOnUnApprovedClaims,
                    field.MandatoryStatus,
                    field.ValidForNpc,
                    field.IsActive,
                    field.AvailableForCharacterGroupIds,
                    field.Description,
                    field.MasterDescription,
                    field.IncludeInPrint,
                    fieldSettings,
                        field.ProgrammaticValue,
                        CreateProjectFieldVisibility(field));
            }

            static ProjectFieldVisibility CreateProjectFieldVisibility(ProjectField field)
            {
                return field switch
                {
                    { IsPublic: true, CanPlayerView: true } => ProjectFieldVisibility.Public,
                    { IsPublic: false, CanPlayerView: true } => ProjectFieldVisibility.PlayerAndMaster,
                    { IsPublic: false, CanPlayerView: false } => ProjectFieldVisibility.MasterOnly,
                    { IsPublic: true, CanPlayerView: false } => throw new InvalidOperationException("Invalid combination of flagss"),
                };
            }
        }

        IEnumerable<ProjectFieldVariant> CreateVariants(ProjectField field, ProjectFieldIdentification fieldId)
        {
            if (field.FieldType == ProjectFieldType.ScheduleTimeSlotField)
            {
                foreach (var variant in field.DropdownValues)
                {
                    yield return new TimeSlotFieldVariant(
                            new(fieldId, variant.ProjectFieldDropdownValueId),
                            variant.Label,
                            variant.Price,
                            variant.PlayerSelectable,
                            variant.IsActive,
                            variant.CharacterGroup?.CharacterGroupId,
                            variant.Description,
                            variant.MasterDescription,
                            variant.ProgrammaticValue
                        );
                }
            }
            else
            {
                foreach (var variant in field.DropdownValues)
                {
                    yield return new ProjectFieldVariant(
                            new(fieldId, variant.ProjectFieldDropdownValueId),
                            variant.Label,
                            variant.Price,
                            variant.PlayerSelectable,
                            variant.IsActive,
                            variant.CharacterGroup?.CharacterGroupId,
                            variant.Description,
                            variant.MasterDescription,
                            variant.ProgrammaticValue
                        );
                }
            }
        }
    }

    async Task<ProjectMastersListInfo> IProjectMetadataRepository.GetMastersList(ProjectIdentification projectId)
    {
        var project = await GetProjectWithFieldsAsync(projectId.Value) ?? throw new InvalidOperationException($"Project with {projectId} not found");

        var masters = project.ProjectAcls.Select(acl => new ProjectMasterInfo(
            new UserIdentification(acl.User.UserId),
            acl.User.ExtractDisplayName(),
            new Email(acl.User.Email),
            acl.GetPermissions())
            );

        return new ProjectMastersListInfo(projectId, project.ProjectName, masters.ToArray());
    }

    async Task<ProjectHeaderDto[]> IProjectRepository.GetMyProjects(UserIdentification userIdentification)
    {
        var predicate = ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active);
        var query = from project in ActiveProjects.AsExpandable()
                    let master = project.ProjectAcls.Any(a => a.UserId == userIdentification.Value)
                    let claims = project.Claims.Where(c => c.PlayerUserId == userIdentification.Value).Any(predicate.Compile())
                    where master || claims
                    select new
                    {
                        project.ProjectId,
                        project.ProjectName,
                        IAmMaster = master,
                        HasActiveClaims = claims,
                    };

        var result = await query.ToListAsync();

        return result.Select(x => new ProjectHeaderDto(new(x.ProjectId), x.ProjectName, x.IAmMaster, x.HasActiveClaims)).ToArray();
    }
}

