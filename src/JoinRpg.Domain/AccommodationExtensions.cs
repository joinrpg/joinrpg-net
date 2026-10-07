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
    /// <para>
    /// Обслуживает контур приглашений и карточку заявки — он остался на EF-сущностях (ADR018, §13).
    /// Свободное место в комнате для страницы комнат и для заселения считает доменный агрегат —
    /// <c>RoomCategoryPlan.GetFreeSpace</c>.
    /// </para>
    /// <para>
    /// Вместимость берётся из метаданных проекта (ADR015), а не из навигаций EF: внутри мутации
    /// обращение к незагруженной навигации — скрытый запрос, чего ADR014 не допускает. Перегрузки
    /// поверх навигаций нет намеренно: правило минимума по типам жильцов на ней дало бы ленивую
    /// загрузку на каждую группу, а без него список приглашений и приём приглашения считали бы
    /// свободное место по-разному.
    /// </para>
    /// </remarks>
    public static int GetRoomFreeSpace(this AccommodationRequest accommodationRequest, ProjectInfo projectInfo)
    {
        if (accommodationRequest.Accommodation is ProjectAccommodation room)
        {
            // Правило свободного места ADR018: комнату ужимает тип каждой живущей в ней группы,
            // а не тип комнаты — у комнаты его нет, она принадлежит категории (ADR020).
            var limit = room.Inhabitants
                .Select(group => Capacity(group.AccommodationTypeId))
                .Append(Capacity(accommodationRequest.AccommodationTypeId))
                .Min();
            return limit - room.GetAllInhabitants().Count();
        }

        return Capacity(accommodationRequest.AccommodationTypeId) - accommodationRequest.Subjects.Count;

        int Capacity(int accommodationTypeId)
            => projectInfo.AccommodationSettings
                .GetTypeById(new AccommodationTypeIdentification(projectInfo.ProjectId, accommodationTypeId))
                .Capacity;
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
