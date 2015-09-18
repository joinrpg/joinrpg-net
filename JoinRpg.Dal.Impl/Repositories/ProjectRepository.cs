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
  public class ProjectRepository : RepositoryImplBase, IProjectRepository
  {
    public ProjectRepository(MyDbContext ctx) : base(ctx) 
    {
    }

    async Task<IEnumerable<Project>> IProjectRepository.GetActiveProjects() => await ActiveProjects.ToListAsync();

    private IQueryable<Project> ActiveProjects => AllProjects.Where(project => project.Active);
    private IQueryable<Project> AllProjects => Ctx.ProjectsSet.Include(p => p.ProjectAcls); 

    public IEnumerable<Project> GetMyActiveProjects(int? userInfoId)
      => ActiveProjects.Where(MyProjectPredicate(userInfoId));

    public async Task<IEnumerable<Project>> GetMyActiveProjectsAsync(int? userInfoId)
    {
      if (userInfoId == null)
      {
        return new Project[] {};
      }
      return await ActiveProjects.Where(MyProjectPredicate(userInfoId)).ToListAsync();
    }

    public Project GetProject(int project) => AllProjects.SingleOrDefault(p => p.ProjectId == project);
    public Task<Project> GetProjectAsync(int project) => AllProjects.SingleOrDefaultAsync(p => p.ProjectId == project);

    public Task<Project> GetProjectWithDetailsAsync(int project)
      => AllProjects
        .Include(p => p.Details) //TODO: Include p.ProjectAcls.Users
        .SingleOrDefaultAsync(p => p.ProjectId == project);

    public Task<CharacterGroup> LoadGroupAsync(int projectId, int characterGroupId)
    {
      return
        Ctx.Set<CharacterGroup>()
          .Include(cg => cg.Project)
          .SingleOrDefaultAsync(cg => cg.CharacterGroupId ==characterGroupId && cg.ProjectId == projectId);
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
          .Include(c => c.Claims)
          //TODO .Include (c => c.Clalims.User)
          .SingleOrDefaultAsync(e => e.CharacterId == characterId && e.ProjectId == projectId);
    }

    public CharacterGroup GetCharacterGroup(int projectId, int groupId)
    {
      return GetProject(projectId).CharacterGroups.SingleOrDefault(c => c.CharacterGroupId == groupId);
    }

    public Task<Claim> GetClaim(int projectId, int claimId)
    {
      return
        Ctx.ClaimSet
          .Include(c => c.Project)
          .Include(c => c.Project.ProjectAcls)
          .Include(c => c.Character)
          .Include(c => c.Player)
          .Include(c => c.Player.Claims)
          .SingleOrDefaultAsync(e => e.ClaimId == claimId && e.ProjectId == projectId);
    }

    public async Task<IList<Character>> LoadCharacters(int projectId, ICollection<int> characterIds)
    {
      return await Ctx.Set<Character>().Where(cg => cg.ProjectId == projectId && characterIds.Contains(cg.CharacterId)).ToListAsync();
    }

    public async Task<IList<CharacterGroup>> LoadGroups(int projectId, ICollection<int> groupIds)
    {
      return await Ctx.Set<CharacterGroup>().Where(cg => cg.ProjectId == projectId&& groupIds.Contains(cg.CharacterGroupId)).ToListAsync();
    }
  }

}