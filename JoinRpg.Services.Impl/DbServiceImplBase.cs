using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Impl
{
  //TODO: Split on specific and not specific to domain helpers
  //TODO: We should be able to use Repositories here
  public class DbServiceImplBase
  {
    protected readonly IUnitOfWork UnitOfWork;
    protected IUserRepository UserRepository => _userRepository.Value;

    private readonly Lazy<IUserRepository> _userRepository;

    protected DbServiceImplBase(IUnitOfWork unitOfWork)
    {
      UnitOfWork = unitOfWork;
      _userRepository = new Lazy<IUserRepository>(unitOfWork.GetUsersRepository);
    }

    [NotNull]
    protected T LoadProjectSubEntity<T>(int projectId, int subentityId) where T : class, IProjectSubEntity
    {
      var field = UnitOfWork.GetDbSet<T>().Find(subentityId);
      if (field != null && field.ProjectId == projectId)
      {
        return field;
      }
      throw new DbEntityValidationException();
    }

    [NotNull]
    protected async Task<T> LoadProjectSubEntityAsync<T>(int projectId, int subentityId)
      where T : class, IProjectSubEntity
    {
      var field = await UnitOfWork.GetDbSet<T>().FindAsync(subentityId);
      if (field != null && field.ProjectId == projectId)
      {
        return field;
      }
      throw new DbEntityValidationException();
    }

    protected static string Required(string stringValue)
    {
      if (string.IsNullOrWhiteSpace(stringValue))
      {
        throw new DbEntityValidationException();
      }

      return stringValue.Trim();
    }

    protected static ICollection<T> Required<T>(ICollection<T> items)
    {
      if (items.Count == 0)
      {
        throw new DbEntityValidationException();
      }

      return items;
    }

    protected void SmartDelete<T>(T field) where T : class, IDeletableSubEntity
    {
      if (field.CanBePermanentlyDeleted)
      {
        UnitOfWork.GetDbSet<T>().Remove(field);
      }
      else
      {
        field.IsActive = false;
      }
    }

    protected List<CharacterGroup> ValidateCharacterGroupList(int projectId, ICollection<int> parentCharacterGroupIds)
    {
      var characterGroups =
        UnitOfWork.GetDbSet<CharacterGroup>().Where(cg => cg.ProjectId == projectId)
          .Where(cg => parentCharacterGroupIds.Contains(cg.CharacterGroupId))
          .ToList();

      if (characterGroups.Count != parentCharacterGroupIds.Distinct().Count())
      {
        throw new DbEntityValidationException();
      }
      return characterGroups;
    }

    //TODO: Merge this with prev. (we can't do it directly, cause LINQ can't filter by ((IProjectSubEntity)char).Id <- this is not a "simple" property
    protected List<Character> ValidateCharactersList(int projectId, ICollection<int> characterIds)
    {
      var characters =
        UnitOfWork.GetDbSet<Character>().Where(cg => cg.ProjectId == projectId)
          .Where(cg => characterIds.Contains(cg.CharacterId))
          .ToList();

      if (characters.Count != characterIds.Distinct().Count())
      {
        throw new DbEntityValidationException();
      }
      return characters;
    }
  }
}