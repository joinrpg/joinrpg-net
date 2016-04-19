using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;

namespace JoinRpg.Services.Impl
{
  //TODO: Split on specific and not specific to domain helpers
  public class DbServiceImplBase
  {

    protected readonly IUnitOfWork UnitOfWork;
    protected IUserRepository UserRepository => _userRepository.Value;

    private readonly Lazy<IUserRepository> _userRepository;

    protected IProjectRepository ProjectRepository => _projectRepository.Value;

    private readonly Lazy<IProjectRepository> _projectRepository;

    protected IClaimsRepository ClaimsRepository => _claimRepository.Value;
    private readonly Lazy<IClaimsRepository> _claimRepository;

    private readonly Lazy<IPlotRepository> _plotRepository;
    protected IPlotRepository PlotRepository => _plotRepository.Value;

    protected DbServiceImplBase(IUnitOfWork unitOfWork)
    {
      UnitOfWork = unitOfWork;
      _userRepository = new Lazy<IUserRepository>(unitOfWork.GetUsersRepository);
      _projectRepository = new Lazy<IProjectRepository>(unitOfWork.GetProjectRepository);
      _claimRepository = new Lazy<IClaimsRepository>(unitOfWork.GetClaimsRepository);
      _plotRepository = new Lazy<IPlotRepository>(unitOfWork.GetPlotRepository);
    }

    [NotNull]
    protected async Task<T> LoadProjectSubEntityAsync<T>(int projectId, int subentityId)
      where T : class, IProjectEntity
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

    protected static ICollection<T> Required<T>(Expression<Func<ICollection<T>>> itemsLambda)
    {
      var name = itemsLambda.AsPropertyName();
      var items = itemsLambda.Compile()();
      if (items.Count == 0)
      {
        throw new FieldRequiredException(name);
      }

      return items;
    }

    protected bool SmartDelete<T>(T field) where T : class, IDeletableSubEntity
    {
      if (field == null)
      {
        return false;
      }
      if (field.CanBePermanentlyDeleted)
      {
        UnitOfWork.GetDbSet<T>().Remove(field);
        return true;
      }
      else
      {
        field.IsActive = false;
        return false;
      }
    }

    protected async Task<IList<CharacterGroup>> ValidateCharacterGroupList(int projectId, ICollection<int> groupIds)
    {
      var characterGroups = await ProjectRepository.LoadGroups(projectId, groupIds);

      if (characterGroups.Count != groupIds.Distinct().Count())
      {
        var missing = string.Join(", ", groupIds.Except(characterGroups.Select(cg => cg.CharacterGroupId)));
        throw new Exception($"Groups {missing} doesn't belong to project");
      }
      return characterGroups;
    }

    protected async Task<IList<Character>> ValidateCharactersList(int projectId, ICollection<int> characterIds)
    {
      var characters =
        await ProjectRepository.LoadCharacters(projectId, characterIds);

      if (characters.Count != characterIds.Distinct().Count())
      {
        throw new DbEntityValidationException();
      }
      return characters;
    }
  }
}