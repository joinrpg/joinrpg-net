using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain
{
    public abstract class JoinRpgBaseException : ApplicationException
    {
        protected JoinRpgBaseException(string message) : base(message)
        {
        }
    }

    public class JoinRpgInvalidUserException : JoinRpgBaseException
    {
        public JoinRpgInvalidUserException(string message = "Cannot perform this operation for current user.") : base(message)
        {
        }
    }

    public class MustBeAdminException : JoinRpgInvalidUserException
    {
        public MustBeAdminException() : base("Cannot perform this operation for non-admin user.")
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

    public class JoinRpgEntityNotFoundException : JoinRpgBaseException
    {
        public JoinRpgEntityNotFoundException(IEnumerable<int> ids, string typeName) : base($"Can't found entities of type {typeName} by ids {string.Join(", ", ids)}")
        {
        }

        public JoinRpgEntityNotFoundException(int id, string typeName) : base($"Can't found entity of type {typeName} by id {id}")
        {
        }
    }


    public class CannotPerformOperationInFuture : JoinRpgBaseException
    {
        public CannotPerformOperationInFuture() : base("Cannot perform operation in future")
        {
        }
    }

    public class CannotPerformOperationInPast : JoinRpgBaseException
    {
        public CannotPerformOperationInPast() : base("Cannot perform operation in past")
        {
        }
    }


    public class ProjectEntityDeactivedException : JoinRpgProjectEntityException
    {
        public ProjectEntityDeactivedException(IProjectEntity entity) : base(entity, $"This operation can't be performed on deactivated entity.")
        {

        }
    }

    public class ProjectDeactivedException : JoinRpgBaseException
    {
        public ProjectDeactivedException() : base("This operation can\'t be performed on deactivated project.")
        {

        }
    }

    public class ClaimWrongStatusException : JoinRpgProjectEntityException
    {
        public ClaimWrongStatusException(Claim entity, IEnumerable<Claim.Status> possible)
          : base(entity, $"This operation can be performed only on claims with status {string.Join(", ", possible.Select(s => s.ToString()))}, but current status is {entity.ClaimStatus}")
        {
        }

        public ClaimWrongStatusException(Claim entity)
          : base(entity, $"This operation can not be performed on claim with status = {entity.ClaimStatus}.")
        {
        }
    }

    public class ClaimAlreadyPresentException : JoinRpgBaseException
    {
        public ClaimAlreadyPresentException() : base("Claim already present for this character or group.") { }
    }

    public class ClaimTargetIsNotAcceptingClaims : JoinRpgBaseException
    {
        public ClaimTargetIsNotAcceptingClaims() : base("This character or group does not accept claims.") { }
    }

    public class MasterHasResponsibleException : JoinRpgProjectEntityException
    {
        public User Master { get; }
        public MasterHasResponsibleException(ProjectAcl entity) : base(entity, "Cannot remove master that has groups attached to it.")
        {
            Master = entity.User;
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

    public class EmailSendFailedException : JoinRpgBaseException
    {
        public EmailSendFailedException(string message) : base(message)
        {
        }
    }

    public class FieldRequiredException : JoinRpgBaseException
    {
        public string FieldName { get; }

        public FieldRequiredException(string fieldName) : base($"{fieldName}")
        {
            FieldName = fieldName;
        }
    }

    public class ValueAlreadySetException : JoinRpgBaseException
    {
        public ValueAlreadySetException(string message) : base(message)
        {
        }
    }

    public class ProjectAccomodationNotFound : JoinRpgBaseException
    {
        public ProjectAccomodationNotFound(int projectId, int accomodationTypeId, int accomodationType) : base($"Место проживание с id={accomodationType} не соотвествуют проекту с id={projectId} и типом проживания с id={accomodationTypeId} ")
        {
        }

    }
}
