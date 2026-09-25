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

/// <summary>
/// Нельзя снять доступ с последнего мастера, который умеет выдавать права:
/// иначе права в проекте больше никто не сможет выдать.
/// </summary>
public class LastMasterWithGrantRightsException(ProjectIdentification projectId, UserIdentification userId)
    : JoinRpgProjectException(projectId, "Cannot remove the last master that can grant rights.")
{
    public UserIdentification UserId { get; } = userId;
}

/// <summary>
/// Публичная группа осталась бы без единого публичного пути наверх — см.
/// <see cref="PublicGroupPathRule"/>.
/// </summary>
/// <remarks>
/// Сообщение готово к показу пользователю и перечисляет конкретные группы: чинить их всё равно
/// мастеру, а найти их в сетке ролей иначе тяжело.
/// </remarks>
public class PublicGroupWithoutPublicPathException(
    ProjectIdentification projectId,
    IReadOnlyCollection<string> groupNames)
    : JoinRpgProjectException(projectId, BuildMessage(groupNames))
{
    public IReadOnlyCollection<string> GroupNames { get; } = groupNames;

    private static string BuildMessage(IReadOnlyCollection<string> groupNames)
        => "Публичная группа должна лежать хотя бы в одной публичной — иначе снаружи её не видно, "
            + "а персонажи в ней выглядят доступными для заявки. "
            + $"После сохранения публичного пути наверх не останется у групп: {string.Join(", ", groupNames)}.";
}

public class PaymentTypeInfoDeactivatedException(PaymentTypeIdentification paymentTypeIdentification)
    : JoinRpgProjectException(paymentTypeIdentification.ProjectId, $"{paymentTypeIdentification} деактивирован");

public class FieldValueInvalidException(ProjectFieldIdentification fieldId, int variantId)
    : JoinRpgBaseException($"Поле {fieldId}: значение {variantId} не является допустимым вариантом")
{
    public ProjectFieldIdentification FieldId { get; } = fieldId;
    public int VariantId { get; } = variantId;
}
