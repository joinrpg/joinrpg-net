using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Repositories
{
  [UsedImplicitly]
  public class ProjectRepository : GameRepositoryImplBase, IProjectRepository
  {
    public ProjectRepository(MyDbContext ctx) : base(ctx) 
    {
    }

    public async Task<IEnumerable<Project>> GetActiveProjectsWithClaimCount()
      => await ActiveProjects.Include(p=> p.Claims).ToListAsync();

    public async Task<IEnumerable<Project>> GetAllProjectsWithClaimCount() 
      => await AllProjects.Include(p=> p.Claims).ToListAsync();

    private IQueryable<Project> ActiveProjects => AllProjects.Where(project => project.Active);
    private IQueryable<Project> AllProjects => Ctx.ProjectsSet.Include(p => p.ProjectAcls); 

    public IEnumerable<Project> GetMyActiveProjects(int? userInfoId)
      => userInfoId == null ? Enumerable.Empty<Project>() :  ActiveProjects.Where(MyProjectPredicate(userInfoId));

    public Task<Project> GetProjectAsync(int project) => AllProjects.SingleOrDefaultAsync(p => p.ProjectId == project);

    public Task<Project> GetProjectWithDetailsAsync(int project)
      => AllProjects
        .Include(p => p.Details)
        .Include(p => p.ProjectAcls.Select(a => a.User))
        .SingleOrDefaultAsync(p => p.ProjectId == project);

    public Task<CharacterGroup> GetGroupAsync(int projectId, int characterGroupId)
    {
      return
        Ctx.Set<CharacterGroup>()
          .Include(cg => cg.Project)
          .SingleOrDefaultAsync(cg => cg.CharacterGroupId ==characterGroupId && cg.ProjectId == projectId);
    }

    public async Task<CharacterGroup> LoadGroupWithTreeAsync(int projectId, int characterGroupId)
    {
      await LoadProjectCharactersAndGroups(projectId);
      await LoadMasters(projectId);
      await LoadProjectClaims(projectId);

      var project = await Ctx.ProjectsSet
        .Include(p => p.Details)
        .SingleOrDefaultAsync(p => p.ProjectId == projectId);

      return project.CharacterGroups.SingleOrDefault(cg => cg.CharacterGroupId == characterGroupId);
    }

    public Task<CharacterGroup> LoadGroupWithChildsAsync(int projectId, int characterGroupId)
    {
      return
        Ctx.Set<CharacterGroup>()
          .Include(cg => cg.Project.Characters)
          .Include(cg => cg.ParentGroups)
          .SingleOrDefaultAsync(cg => cg.CharacterGroupId == characterGroupId && cg.ProjectId == projectId);
    }

    private static Expression<Func<Project, bool>> MyProjectPredicate(int? userInfoId)
    {
      if (userInfoId == null)
      {
        return project => false;
      }
      return project => project.ProjectAcls.Any(projectAcl => projectAcl.UserId == userInfoId);
    }


    public Task<Character> GetCharacterAsync(int projectId, int characterId)
    {
      return
        Ctx.Set<Character>()
          .Include(c => c.Project)
          .Include(c => c.Project.ProjectFields)
          .SingleOrDefaultAsync(e => e.CharacterId == characterId && e.ProjectId == projectId);
    }

    public async Task<Character> GetCharacterWithGroups(int projectId, int characterId)
    {
      await LoadProjectGroups(projectId);

      return
        await Ctx.Set<Character>()
          .Include(c => c.Groups)
          .Include(c => c.Project.ProjectFields.Select(pf => pf.DropdownValues))
          .SingleOrDefaultAsync(e => e.CharacterId == characterId && e.ProjectId == projectId);
    }
    public async Task<Character> GetCharacterWithDetails(int projectId, int characterId)
    {
      await LoadProjectCharactersAndGroups(projectId);
      await LoadProjectClaims(projectId);

      return
        await Ctx.Set<Character>()
          .Include(c => c.Groups)
          .Include(c => c.Project.ProjectFields.Select(pf => pf.DropdownValues))
          .SingleOrDefaultAsync(e => e.CharacterId == characterId && e.ProjectId == projectId);
    }


    public async Task<IReadOnlyCollection<Character>> LoadCharacters(int projectId, IReadOnlyCollection<int> characterIds)
    {
      return await Ctx.Set<Character>().Where(cg => cg.ProjectId == projectId && characterIds.Contains(cg.CharacterId)).ToListAsync();
    }

    public async Task<IReadOnlyCollection<Character>> LoadCharactersWithGroups(int projectId, IReadOnlyCollection<int> characterIds)
    {
      await LoadProjectGroups(projectId);

      return
        await Ctx.Set<Character>()
          .Include(c => c.Groups)
          .Include(c => c.Project.ProjectFields.Select(pf => pf.DropdownValues))
          .Where(e => characterIds.Contains(e.CharacterId) && e.ProjectId == projectId).ToListAsync();
    }

    public async Task<IList<CharacterGroup>> LoadGroups(int projectId, IReadOnlyCollection<int> groupIds)
    {
      return await Ctx.Set<CharacterGroup>().Where(cg => cg.ProjectId == projectId && groupIds.Contains(cg.CharacterGroupId)).ToListAsync();
    }

    public Task<ProjectField> GetProjectField(int projectId, int projectCharacterFieldId)
    {
      return Ctx.Set<ProjectField>()
        .Include(f => f.Project)
        .Include(f => f.DropdownValues)
        .SingleOrDefaultAsync(f => f.ProjectFieldId == projectCharacterFieldId && f.ProjectId == projectId);
    }

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

    public async Task<ICollection<Character>> GetCharacters(int projectId)
    {
      await LoadProjectFields(projectId);
      await LoadProjectCharactersAndGroups(projectId);
      await LoadMasters(projectId);
      await LoadProjectClaims(projectId);

      //This is sync operation becase Project should be already loaded here
      return Ctx.Set<Project>().Find(projectId).Characters;
    }

    public async Task<IEnumerable<Project>> GetProjectsWithoutAllrpgAsync()
      => await ActiveProjects.Where(p => p.Details.AllrpgId == null).ToListAsync();

    public async Task<CharacterGroup> LoadGroupWithTreeAsync(int projectId)
    {
      var project = await GetProjectAsync(projectId);
      return await LoadGroupWithTreeAsync(projectId, project.RootGroup.CharacterGroupId);
    }

    public async Task<CharacterGroup> LoadGroupWithTreeSlimAsync(int projectId)
    {
      await Ctx.ProjectsSet
        .Include(p => p.CharacterGroups.Select(cg => cg.ParentGroups))
        .Include(p => p.Characters.Select(cg => cg.Groups))
        .Where(p => p.ProjectId == projectId)
        .LoadAsync();

      var project1 = await Ctx.ProjectsSet
        .Include(p => p.Details)
        .SingleOrDefaultAsync(p => p.ProjectId == projectId);
      return project1.RootGroup;
    }
  }

}