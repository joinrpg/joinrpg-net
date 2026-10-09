using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata.Accommodation;

namespace JoinRpg.Domain;

public static class AccommodationExtensions
{
    /// <summary>
    /// Тип проживания, выбранный в заявке, — из метаданных проекта (ADR015), или <c>null</c>, если
    /// заявки на проживание нет.
    /// </summary>
    /// <remarks>
    /// Навигационное свойство <c>AccommodationRequest.AccommodationType</c> читать не нужно:
    /// оно почти нигде не входит в <c>Include</c>, и на списке заявок это давало ленивую загрузку
    /// на каждую строку (#5166). Типы проживания — метаданные, они уже лежат в
    /// <see cref="ProjectInfo.AccommodationSettings"/>, закешированном на запрос.
    /// </remarks>
    public static AccommodationTypeInfo? GetAccommodationType(this Claim claim, ProjectInfo projectInfo)
        => claim.AccommodationRequest is AccommodationRequest request
            ? projectInfo.AccommodationSettings.GetTypeById(
                new AccommodationTypeIdentification(projectInfo.ProjectId, request.AccommodationTypeId))
            : null;
}
