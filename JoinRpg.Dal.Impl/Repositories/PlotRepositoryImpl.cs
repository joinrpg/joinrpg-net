using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Dal.Impl.Repositories
{
  [UsedImplicitly]
  public class PlotRepositoryImpl : GameRepositoryImplBase, IPlotRepository
  {
    public async Task<PlotFolder> GetPlotFolderAsync(int projectId, int plotFolderId)
    {
      await LoadProjectCharactersAndGroups(projectId);
      await LoadMasters(projectId);

      return
        await Ctx.Set<PlotFolder>()
          .Include(pf => pf.Elements)
          .Include(pf => pf.Elements.Select(e => e.Texts))
          .Include(pf => pf.Project.Claims)
          .SingleOrDefaultAsync(pf => pf.PlotFolderId == plotFolderId && pf.ProjectId == projectId);
    }

    public async Task<IReadOnlyCollection<PlotElement>> GetPlotsForCharacter(Character character)
    {
      var ids =
        character.Groups.SelectMany(group => group.FlatTree(g => g.ParentGroups))
          .Select(g => g.CharacterGroupId)
          .Distinct()
          .ToList(); //ToList required here so all lazy loads are finished before we are starting making condition below.
      return
        await Ctx.Set<PlotElement>()
          .Include(e => e.Texts)
          .Include(e => e.TargetCharacters)
          .Include(e => e.TargetGroups)
          .Where(
            e =>
              e.TargetCharacters.Any(ch => ch.CharacterId == character.CharacterId) ||
              e.TargetGroups.Any(g => ids.Contains(g.CharacterGroupId)))
          .ToListAsync();
    }

    public async Task<IReadOnlyCollection<PlotFolder>> GetPlotsWithTargetAndText(int projectid)
      =>
        await Ctx.Set<PlotFolder>()
          .Include(pf => pf.Elements.Select(e => e.TargetCharacters))
          .Include(pf => pf.Elements.Select(e => e.TargetGroups))
          .Include(pf => pf.Elements.Select(e => e.Texts))
          .Where(pf => pf.ProjectId == projectid)
          .ToListAsync();

    public async Task<List<PlotFolder>> GetPlots(int project)
    {
      await LoadProjectGroups(project); //TODO[GroupsLoad] it's unclear why we need this
      return await Ctx.Set<PlotFolder>()
        .Include(pf => pf.Elements)
        .Where(pf => pf.ProjectId == project)
        .ToListAsync();
    }

    public Task<List<PlotFolder>> GetPlotsWithTargets(int projectId)
      =>
        Ctx.Set<PlotFolder>()
          .Include(pf => pf.Elements.Select(e => e.TargetCharacters))
          .Include(pf => pf.Elements.Select(e => e.TargetGroups))
          .Where(pf => pf.ProjectId == projectId)
          .ToListAsync();


    public PlotRepositoryImpl(MyDbContext ctx) : base(ctx)
    {
    }

    public async Task<IReadOnlyCollection<PlotElement>> GetActiveHandouts(int projectid)
    {
      return  await Ctx.Set<PlotFolder>()
        
        .Where(pf => pf.ProjectId == projectid)
        .SelectMany(p => p.Elements)
        .Include(pf => pf.TargetCharacters)
        .Include(e => e.TargetGroups)
        .Include(e => e.Texts)
        .Where(e => e.ElementType == PlotElementType.Handout && e.IsActive)
        .ToListAsync();
    }

    public Task<List<PlotFolder>> GetPlotsForTargets(int projectId, List<int> characterIds, List<int> characterGroupIds)
    {
      return Ctx.Set<PlotFolder>()
        .Include(pf => pf.Elements.Select(e => e.TargetCharacters))
        .Include(pf => pf.Elements.Select(e => e.TargetGroups))
        .Where(pf => pf.ProjectId == projectId)
        .Where(
            pf =>
              pf.Elements.Any(
                e => e.TargetCharacters.Select(c => c.CharacterId).Intersect(characterIds).Any()
                     || e.TargetGroups.Select(c => c.CharacterGroupId).Intersect(characterGroupIds).Any()))
          .ToListAsync();
    }

    public async Task<IReadOnlyCollection<PlotFolder>> GetPlotsByTag(int projectid, string tagname)
    {
      await LoadProjectGroups(projectid); //TODO[GroupsLoad] it's unclear why we need this
      return await Ctx.Set<PlotFolder>()
        .Include(pf => pf.Elements)
        .Where(pf => pf.ProjectId == projectid && pf.PlotTags.Any(tag => tag.TagName == tagname))
        .ToListAsync();
    }
  }
}
