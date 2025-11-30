using System.Linq.Expressions;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.Claims;

namespace JoinRpg.Domain;

public abstract class JoinRpgProjectException(ProjectIdentification projectId, string message) : JoinRpgBaseException(message)
{
    public ProjectIdentification ProjectId { get; } = projectId;
}

public class PlayerOnlyException(ClaimIdentification claimId, int? currentUserId)
    : JoinRpgProjectException(claimId.ProjectId, $"Нет доступа к заявке {claimId}, потому что ее отправил не UserId={currentUserId}")
{
    public ClaimIdentification ClaimId { get; } = claimId;
}

public abstract class JoinRpgProjectEntityException : JoinRpgProjectException
{
    public IProjectEntityWithId Entity { get; set; }
    protected JoinRpgProjectEntityException(IProjectEntityWithId entity, string message)
        : base(entity.ProjectIdentification, message)
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

public class PaymentException(Project project, string message) : JoinRpgProjectException(new ProjectIdentification(project.ProjectId), message)
{
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

public class ProjectDeactivatedException(ProjectIdentification projectId)
    : JoinRpgProjectException(projectId, "This operation can\'t be performed on deactivated project.");

public class ClaimWrongStatusException : JoinRpgProjectEntityException
{
    public ClaimWrongStatusException(Claim entity, IEnumerable<ClaimStatus> possible)
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

public class InsufficientContactsException() : JoinRpgBaseException("Для отправки заявки необходимы контакты");

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

public class NoAccessToProjectException : JoinRpgProjectException
{

    public Permission Permission = Permission.None;
    public int? UserId { get; }

    [Obsolete("Use ctor that accepts ProjectInfo")]
    public NoAccessToProjectException(Project project, int? userId, Expression<Func<ProjectAcl, bool>> accessExpression)
      : base(new ProjectIdentification(project.ProjectId), $"No access to project {project.ProjectName} for user {userId}. Required access: {accessExpression.AsPropertyName()}") => UserId = userId;

    [Obsolete("Use ctor that accepts ProjectInfo")]
    public NoAccessToProjectException(Project project, int? userId)
      : base(new ProjectIdentification(project.ProjectId), $"No access to project {project.ProjectName} for user {userId}") => UserId = userId;

    public NoAccessToProjectException(ProjectInfo project, int? userId, Permission permission = Permission.None)
       : base(project.ProjectId, $"No access to project {project.ProjectName} for user {userId}")
    {
        UserId = userId;
        Permission = permission;
    }

    [Obsolete("Use ctor that accepts ProjectInfo")]
    public NoAccessToProjectException(IProjectEntity entity, int? userId)
  : base(new ProjectIdentification(entity.ProjectId), $"No access to entity of {entity.Project.ProjectName} for user {userId}") => UserId = userId;
}

public class FieldRequiredException(string fieldName, string message) : JoinRpgBaseException(message)
{
    public FieldRequiredException(string fieldName) : this(fieldName, $"Field {fieldName} is required")
    {

    }

    public string FieldName { get; } = fieldName;
}

public class CharacterFieldRequiredException(string fieldName, ProjectFieldIdentification fieldId, CharacterIdentification characterId)
    : FieldRequiredException(fieldName, $"Проблема при сохранении персонажа {characterId}: поле \"{fieldName}\"{fieldId} обязательно, но не заполнено.")
{
    public ProjectFieldIdentification FieldId { get; } = fieldId;
    public CharacterIdentification CharacterId { get; } = characterId;
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

public class JoinRpgAccountOperationFailedException(string message) : JoinRpgBaseException(message) { }

public class JoinRpgProjectMisconfiguredException(ProjectIdentification projectId, string message) : JoinRpgProjectException(projectId, message);
