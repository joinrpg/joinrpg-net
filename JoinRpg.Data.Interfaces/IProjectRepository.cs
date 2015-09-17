using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
  public interface IProjectRepository : IDisposable
  {
    Task<IEnumerable<Project>> GetActiveProjects();

    IEnumerable<Project> GetMyActiveProjects(int? userInfoId);

    Task<IEnumerable<Project>> GetMyActiveProjectsAsync(int? userInfoId);

    Project GetProject(int project);
    Task<Project> GetProjectAsync(int project);
    Task<Project> GetProjectWithDetailsAsync(int project);
    Task<CharacterGroup> LoadGroupAsync(int projectId, int characterGroupId);
    [Obsolete("LoadGroupAsync")]
    CharacterGroup GetCharacterGroup(int projectId, int groupId);
    Task<Character> GetCharacterAsync(int projectId, int characterId);
    Task<Claim> GetClaim(int projectId, int claimId);
  }
}
