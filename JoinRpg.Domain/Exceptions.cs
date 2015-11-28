using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain
{
  public abstract  class JoinRpgBaseException : ApplicationException
  {
    protected JoinRpgBaseException(string message) : base(message)
    {
    }
  }

  public abstract class JoinRpgProjectEntityException : JoinRpgBaseException
  {
    protected JoinRpgProjectEntityException(IProjectEntity entity, string message) : base(message)
    {
      Project = entity.Project;
    }

    [PublicAPI]
    public Project Project { get; }
  }


  public class CannotPerformOperationInFuture : JoinRpgBaseException
  {
    public CannotPerformOperationInFuture() : base("Cannot perform operation in future")
    {
    }
  }

  public class ProjectEntityDeactivedException : JoinRpgProjectEntityException
  {
    public ProjectEntityDeactivedException(IProjectEntity entity) : base(entity, $"This operation can't be performed on deactivated entity.")
    {
      
    }
  }

  public class ClaimWrongStatusException : JoinRpgProjectEntityException
  {
    public ClaimWrongStatusException(Claim entity, IEnumerable<Claim.Status> possible) 
      : base(entity, $"This operation can be performed only on claims with status {string.Join(", ", possible.Select(s => s.ToString()))}, but current status is {entity.ClaimStatus}")
    {
    }
  }

  public class NoAccessToProjectException : JoinRpgProjectEntityException
  {
    [PublicAPI]
    public int? UserId { get; }

    public NoAccessToProjectException(Project project, int? userId, Expression<Func<ProjectAcl, bool>> accessExpression)
      : base(project, $"No access to project {project.ProjectName} for user {userId}. Required access: {accessExpression.AsPropertyName()}")
    {
      UserId = userId;
    }

    public NoAccessToProjectException(Project project, int? userId)
      : base(project, $"No access to project {project.ProjectName} for user {userId}")
    {
      UserId = userId;
    }

    public NoAccessToProjectException(IProjectEntity entity, int? userId)
  : base(entity, $"No access to entity of {entity.Project.ProjectName} for user {userId}")
    {
      UserId = userId;
    }
  }

  public class ValueAlreadySetException : JoinRpgBaseException
  {
    public ValueAlreadySetException(string message) : base(message)
    {
    }
  }
}
