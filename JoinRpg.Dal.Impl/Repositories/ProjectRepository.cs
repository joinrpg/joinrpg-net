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
    private IQueryable<Project> AllProjects => Ctx.ProjectsSet;

    public IEnumerable<Project> GetAllMyProjects(int userInfoId)
      => AllProjects.Where(MyProjectPredicate(userInfoId));

    public IEnumerable<Project> GetMyActiveProjects(int? userInfoId)
      => ActiveProjects.Where(MyProjectPredicate(userInfoId));

    public Project GetMyProject(int projectId, int userInfoId)
      => AllProjects.Where(MyProjectPredicate(userInfoId)).Include(nameof(ProjectAcl)).SingleOrDefault(project => project.ProjectId == projectId);

    public Project GetProject(int project) => AllProjects.SingleOrDefault(p => p.ProjectId == project);

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