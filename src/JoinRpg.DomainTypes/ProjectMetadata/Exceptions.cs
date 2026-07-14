using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Helpers;

namespace JoinRpg.DomainTypes.ProjectMetadata;

public abstract class JoinRpgProjectException(ProjectIdentification projectId, string message) : JoinRpgBaseException(message)
{
    public ProjectIdentification ProjectId { get; } = projectId;
}

public class ProjectDeactivatedException(ProjectIdentification projectId)
    : JoinRpgProjectException(projectId, "This operation can\'t be performed on deactivated project.");

public class MasterHasResponsibleException(ProjectIdentification projectId, UserIdentification userId)
    : JoinRpgProjectException(projectId, "Cannot remove master that has groups attached to it.")
{
    public UserIdentification UserId { get; } = userId;
}

public class PaymentTypeInfoDeactivatedException(PaymentTypeIdentification paymentTypeIdentification)
    : JoinRpgProjectException(paymentTypeIdentification.ProjectId, $"{paymentTypeIdentification} деактивирован");

public class FieldValueInvalidException(ProjectFieldIdentification fieldId, int variantId)
    : JoinRpgBaseException($"Поле {fieldId}: значение {variantId} не является допустимым вариантом")
{
    public ProjectFieldIdentification FieldId { get; } = fieldId;
    public int VariantId { get; } = variantId;
}
