using System.Data.Entity.Validation;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Impl
{
  public class DbServiceImplBase
  {
    protected readonly IUnitOfWork UnitOfWork;

    protected DbServiceImplBase(IUnitOfWork unitOfWork)
    {
      UnitOfWork = unitOfWork;
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
    protected async Task<T> LoadProjectSubEntityAsync<T>(int projectId, int subentityId) where T : class, IProjectSubEntity
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

    protected void SmartDelete<T>(T field) where T:class, IDeletableSubEntity
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
  }
}