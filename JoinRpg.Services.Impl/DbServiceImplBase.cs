using System.Data.Entity.Validation;
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

    protected T LoadProjectSubEntity<T>(int projectId, int subentityId) where T : class, IProjectSubEntity
    {
      var field = UnitOfWork.GetDbSet<T>().Find(subentityId);
      if (field.ProjectId != projectId)
      {
        throw new DbEntityValidationException();
      }
      return field;
    }
  }
}