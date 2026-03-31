using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata.Payments;

namespace JoinRpg.Domain;

public abstract class JoinRpgProjectException(ProjectIdentification projectId, string message) : JoinRpgBaseException(message)
{
    public ProjectIdentification ProjectId { get; } = projectId;
}

public class ProjectDeactivatedException(ProjectIdentification projectId)
    : JoinRpgProjectException(projectId, "This operation can\'t be performed on deactivated project.");

public class PaymentTypeInfoDeactivatedException(PaymentTypeIdentification paymentTypeIdentification)
    : JoinRpgProjectException(paymentTypeIdentification.ProjectId, $"{paymentTypeIdentification} деактивирован");

public class FieldValueInvalidException(ProjectFieldIdentification fieldId, int variantId)
    : JoinRpgBaseException($"Поле {fieldId}: значение {variantId} не является допустимым вариантом")
{
    public ProjectFieldIdentification FieldId { get; } = fieldId;
    public int VariantId { get; } = variantId;
}
