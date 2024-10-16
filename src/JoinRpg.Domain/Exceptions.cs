using System.Linq.Expressions;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Domain;

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

public class JoinRpgProjectException : JoinRpgBaseException
{
    public Project Project { get; }

    public JoinRpgProjectException(Project project, string message) : base(message) => Project = project;
}

public abstract class JoinRpgProjectEntityException : JoinRpgProjectException
{
    public IProjectEntity Entity { get; set; }
    protected JoinRpgProjectEntityException(IProjectEntity entity, string message)
        : base(entity.Project, message)
    {
        Entity = entity;
    }
}

public class JoinRpgSlotLimitedException : JoinRpgProjectEntityException
{
    public JoinRpgSlotLimitedException(Character entity) : base(entity, "Couldn't add more characters because slot is full")
    {

    }
}

public class JoinRpgCharacterBrokenStateException : JoinRpgProjectEntityException
{
    public JoinRpgCharacterBrokenStateException(Character entity, string message) : base(entity, message)
    {

    }
}

public class JoinRpgNameFieldDeleteException : JoinRpgProjectEntityException
{
    public JoinRpgNameFieldDeleteException(ProjectField field)
    : base(field, "Can't delete field, because character name bound to it")
    {

    }
}

public class JoinFieldScheduleShouldBeUniqueException : JoinRpgProjectEntityException
{
    public JoinFieldScheduleShouldBeUniqueException(Project project)
    : base(project, "Schedule fields should be unique")
    {

    }
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

public class JoinRpgInsufficientRoomSpaceException : JoinRpgProjectEntityException
{
    public JoinRpgInsufficientRoomSpaceException(ProjectAccommodation entity) : base(entity, "There is no space in room")
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

public class PreferentialFeeNotEnabled : JoinRpgBaseException
{
    public PreferentialFeeNotEnabled() : base("Preferential fee not enabled")
    {
    }
}

public class PaymentException : JoinRpgProjectException
{
    public PaymentException(Project project, string message)
        : base(project, message) { }
}

public class OnlinePaymentUnexpectedStateException : PaymentException
{
    public FinanceOperation FinanceOperation { get; }

    public FinanceOperationState DesiredState { get; }

    public OnlinePaymentUnexpectedStateException(
        FinanceOperation financeOperation,
        FinanceOperationState desiredState)
        : base(
            financeOperation.Project,
            $"Unexpected finance operation {financeOperation.CommentId} state. {desiredState} expected, but {financeOperation.State} found")
    {
        FinanceOperation = financeOperation;
        DesiredState = desiredState;
    }
}

public class OnlinePaymentsNotAvailableException : PaymentException
{
    public OnlinePaymentsNotAvailableException(Project project)
        : base(project, $"Online payments are not available for project {project.ProjectName}")
    { }
}

public class PaymentMethodNotAllowedForRecurrentPaymentsException : PaymentException
{
    public PaymentMethodNotAllowedForRecurrentPaymentsException(Project project, object paymentMethod)
        : base(project, $"It is not possible to use payment method {paymentMethod} to start recurrent payments for project {project.ProjectName}")
    { }
}

public class ProjectEntityDeactivatedException : JoinRpgProjectEntityException
{
    public ProjectEntityDeactivatedException(IProjectEntity entity) : base(entity, $"This operation can't be performed on deactivated entity")
    {

    }
}

public class ProjectDeactivatedException : JoinRpgBaseException
{
    public ProjectDeactivatedException() : base("This operation can\'t be performed on deactivated project.")
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

public class EntityWrongStatusException : JoinRpgProjectEntityException
{
    public EntityWrongStatusException(IProjectEntity entity)
        : base(entity, $"This operation can not be performed on entity with this status")
    {
    }
}

public class ClaimAlreadyPresentException : JoinRpgBaseException
{
    public ClaimAlreadyPresentException() : base("Claim already present for this character or group.") { }
}

public class OnlyOneApprovedClaimException : JoinRpgBaseException
{
    public OnlyOneApprovedClaimException() : base("Approved claim already present for this player, and project allows only one character.") { }
}

public class ClaimTargetIsNotAcceptingClaims : JoinRpgBaseException
{
    public ClaimTargetIsNotAcceptingClaims() : base("This character or group does not accept claims.") { }
}

public class MasterHasResponsibleException : JoinRpgProjectEntityException
{
    public User Master { get; }
    public MasterHasResponsibleException(ProjectAcl entity) : base(entity, "Cannot remove master that has groups attached to it.") => Master = entity.User;
}

public class RoomIsOccupiedException : JoinRpgProjectEntityException
{
    public RoomIsOccupiedException(ProjectAccommodation entity) : base(entity, "Cannot peforrm this operation on occupied room.")
    {
    }
}

public class NoAccessToProjectException : JoinRpgProjectEntityException
{
    public int? UserId { get; }

    public NoAccessToProjectException(Project project, int? userId, Expression<Func<ProjectAcl, bool>> accessExpression)
      : base(project, $"No access to project {project.ProjectName} for user {userId}. Required access: {accessExpression.AsPropertyName()}") => UserId = userId;

    public NoAccessToProjectException(Project project, int? userId)
      : base(project, $"No access to project {project.ProjectName} for user {userId}") => UserId = userId;

    public NoAccessToProjectException(IProjectEntity entity, int? userId)
  : base(entity, $"No access to entity of {entity.Project.ProjectName} for user {userId}") => UserId = userId;
}

public class FieldRequiredException(string fieldName) : JoinRpgBaseException($"Field {fieldName} is required")
{
    public string FieldName { get; } = fieldName;
}

public class CharacterFieldRequiredException(string fieldName, ProjectFieldIdentification fieldId) : FieldRequiredException(fieldName)
{
    public ProjectFieldIdentification FieldId { get; } = fieldId;
}

public class ValueAlreadySetException : JoinRpgBaseException
{
    public ValueAlreadySetException(string message) : base(message)
    {
    }
}

public class JoinRpgConcealCommentException : JoinRpgBaseException
{
    public JoinRpgConcealCommentException(string message = "Current user doesn't have permission to conceal this comment") : base(message)
    {
    }
}
public class ProjectAccomodationNotFound : JoinRpgBaseException
{
    public ProjectAccomodationNotFound(int projectId, int accomodationTypeId, int accomodationType) : base($"Место проживание с id={accomodationType} не соотвествуют проекту с id={projectId} и типом проживания с id={accomodationTypeId} ")
    {
    }

}
