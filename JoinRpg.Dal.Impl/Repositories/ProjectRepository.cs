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

    IEnumerable<Project> IProjectRepository.AllProjects => AllProjects;

    IEnumerable<Project> IProjectRepository.ActiveProjects => ActiveProjects;

    private IQueryable<Project> ActiveProjects => Ctx.ProjectsSet.Where(project => project.Active);
    private IQueryable<Project> AllProjects => Ctx.ProjectsSet.Include(p => p.ProjectAcls);

    public IEnumerable<Project> GetAllMyProjects(int userInfoId)
      => AllProjects.Where(MyProjectPredicate(userInfoId));

    public IEnumerable<Project> GetMyActiveProjects(int? userInfoId)
      => ActiveProjects.Where(MyProjectPredicate(userInfoId));

    public Project GetProject(int project) => AllProjects.SingleOrDefault(p => p.ProjectId == project);
    public Task<Project> GetProjectAsync(int project) => AllProjects.SingleOrDefaultAsync(p => p.ProjectId == project);
    public Task<PlotFolder> GetPlotFolderAsync(int projectId, int plotFolderId)
    {
      return
        Ctx.Set<PlotFolder>()
          .Include(pf => pf.Project)
          .Include(pf => pf.Project.ProjectAcls)
          .Include(pf => pf.Elements)
          .SingleOrDefaultAsync(pf => pf.PlotFolderId == plotFolderId && pf.ProjectId == projectId);
    }

    private static Expression<Func<Project, bool>> MyProjectPredicate(int? userInfoId)
    {
      if (userInfoId == null)
      {
        return project => false;
      }
      return project => project.ProjectAcls.Any(projectAcl => projectAcl.UserId == userInfoId);
    }
  }

}