using System.Collections.Generic;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Interfaces
{
  public interface IProjectRepository
  {
    IEnumerable<Project> AllProjects { get; }
    IEnumerable<Project> ActiveProjects { get; }
  }
}
