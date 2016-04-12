﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
  public interface IProjectRepository : IDisposable
  {
    Task<IEnumerable<Project>> GetActiveProjectsWithClaimCount();
    Task<IEnumerable<Project>> GetAllProjectsWithClaimCount();

    IEnumerable<Project> GetMyActiveProjects(int? userInfoId);

    Task<Project> GetProjectAsync(int project);
    Task<Project> GetProjectWithDetailsAsync(int project);
    Task<CharacterGroup> LoadGroupAsync(int projectId, int characterGroupId);
    Task<CharacterGroup> LoadGroupWithTreeAsync(int projectId, int characterGroupId);
    Task<CharacterGroup> LoadGroupWithChildsAsync(int projectId, int characterGroupId);
    Task<Character> GetCharacterAsync(int projectId, int characterId);
    Task<Character> GetCharacterWithGroups(int projectId, int characterId);
    Task<Character> GetCharacterWithDetails(int projectId, int characterId);
    Task<IList<CharacterGroup>> LoadGroups(int projectId, ICollection<int> groupIds);
    Task<IList<Character>> LoadCharacters(int projectId, ICollection<int> characterIds);
    Task<ProjectField> GetProjectField(int projectId, int projectCharacterFieldId);
    Task<ProjectFieldDropdownValue> GetFieldValue(int projectId, int projectFieldId, int projectCharacterFieldDropdownValueId);
    Task<Project> GetProjectWithFinances(int projectid);
    Task<ICollection<Character>> GetCharacters(int projectId);
    Task<IEnumerable<Project>>  GetProjectsWithoutAllrpgAsync();
  }
}
