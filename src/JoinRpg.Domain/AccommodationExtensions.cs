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

    public static IEnumerable<Claim> GetAllInhabitants(this ProjectAccommodation room) =>
        room.Inhabitants.SelectMany(i => i.Subjects);

    /// <summary>
    /// Сколько ещё человек влезет к этой группе: если группа расселена — по её комнате, если нет —
    /// по вместимости выбранного типа проживания.
    /// </summary>
    /// <remarks>
    /// Обслуживает контур приглашений и карточку заявки — он остался на EF-сущностях (ADR018, §13).
    /// Свободное место в комнате для страницы комнат и для заселения считает доменный агрегат —
    /// <c>RoomCategoryPlan.GetFreeSpace</c>; отдельного расчёта «свободное место комнаты» поверх
    /// EF-сущностей больше нет (ADR018, §8).
    /// </remarks>
    public static int GetRoomFreeSpace(this AccommodationRequest accommodationRequest1)
    {
        if (accommodationRequest1.Accommodation is ProjectAccommodation accommodation)
        {
            return accommodation.ProjectAccommodationType.Capacity - accommodation.GetAllInhabitants().Count();
        }
        else
        {
            return accommodationRequest1.AccommodationType.Capacity - accommodationRequest1.Subjects.Count;
        }
    }

    public static List<User> GetClaimNeighbours(this Claim claim)
    {
        if (claim.AccommodationRequest is AccommodationRequest accommodationRequest)
        {
            if (claim.AccommodationRequest.Accommodation is ProjectAccommodation accommodation)
            {
                return [.. accommodation.Inhabitants.SelectMany(i => i.Subjects).Where(s => s.ClaimId != claim.ClaimId).Select(c => c.Player)];
            }
            else
            {
                return [.. accommodationRequest.Subjects.Where(s => s.ClaimId != claim.ClaimId).Select(c => c.Player)];
            }
        }
        else
        {
            return [];
        }
    }
}
