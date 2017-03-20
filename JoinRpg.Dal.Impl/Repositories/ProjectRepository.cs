﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
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

    public async Task<IEnumerable<Project>> GetArchivedProjectsWithClaimCount()
     => await AllProjects.Include(p => p.Claims).Where(p => !p.Active).ToListAsync();

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
      await LoadProjectFields(projectId);

      var project = await Ctx.ProjectsSet
        .Include(p => p.Details)
        .SingleOrDefaultAsync(p => p.ProjectId == projectId);

      return project.CharacterGroups.SingleOrDefault(cg => cg.CharacterGroupId == characterGroupId);
    }

    public async Task<CharacterGroup> LoadGroupWithChildsAsync(int projectId, int characterGroupId)
    {
      await LoadProjectCharactersAndGroups(projectId);
      return
        await Ctx.Set<CharacterGroup>()
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


    public async Task<Character> GetCharacterAsync(int projectId, int characterId)
    {
      await LoadProjectFields(projectId);
      return
        await Ctx.Set<Character>()
          .Include(c => c.Project)
          .SingleOrDefaultAsync(e => e.CharacterId == characterId && e.ProjectId == projectId);
    }

    public async Task<Character> GetCharacterWithGroups(int projectId, int characterId)
    {
      await LoadProjectGroups(projectId);
      await LoadProjectFields(projectId);

      return
        await Ctx.Set<Character>().SingleOrDefaultAsync(e => e.CharacterId == characterId && e.ProjectId == projectId);
    }
    public async Task<Character> GetCharacterWithDetails(int projectId, int characterId)
    {
      await LoadProjectCharactersAndGroups(projectId);
      await LoadProjectClaims(projectId);
      await LoadProjectFields(projectId);

      return
        await Ctx.Set<Character>()
          .SingleOrDefaultAsync(e => e.CharacterId == characterId && e.ProjectId == projectId);
    }


    public async Task<IReadOnlyCollection<Character>> LoadCharacters(int projectId, IReadOnlyCollection<int> characterIds)
    {
      return await Ctx.Set<Character>().Where(cg => cg.ProjectId == projectId && characterIds.Contains(cg.CharacterId)).ToListAsync();
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

    public async Task<CharacterGroup> LoadGroupWithTreeAsync(int projectId)
    {
      var project = await GetProjectAsync(projectId);
      return await LoadGroupWithTreeAsync(projectId, project.RootGroup.CharacterGroupId);
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

    public async Task<IClaimSource> GetClaimSource(int projectId, int? characterGroupId, int? characterId)
    {
      if (characterGroupId != null)
      {
        return await GetGroupAsync(projectId, (int) characterGroupId);
      }
      if (characterId != null)
      {
        return await GetCharacterAsync(projectId, (int) characterId);
      }
      throw new InvalidOperationException();
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
  }

}