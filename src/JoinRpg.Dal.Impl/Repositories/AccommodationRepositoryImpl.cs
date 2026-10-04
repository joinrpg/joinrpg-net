using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

public class AccommodationRepositoryImpl(MyDbContext ctx) : IAccommodationRepository
{
    public async Task<bool> HasOccupiedRoomOfType(AccommodationTypeIdentification accommodationTypeId)
    {
        return await ctx.Set<ProjectAccommodation>()
            .Where(room => room.ProjectId == accommodationTypeId.ProjectId.Value
                && room.AccommodationTypeId == accommodationTypeId.AccommodationTypeId)
            .AnyAsync(room => room.Inhabitants.Any())
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<ClaimAccommodationInfoRow>>
        GetClaimAccommodationReport(int project)
    {
        // Игрок приезжает разобранным на части: отображаемое имя и телефон. Сущность User в DTO
        // означала бы доступ к любым её навигациям из контроллера и ленивые загрузки (ADR013).
        // Образец плоской проекции имени — CharacterInfoLoader.
        var rows = await ctx.Set<Claim>().AsExpandable()
            .Where(ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active))
            .Where(claim => claim.ProjectId == project)
            .Select(
                claim => new
                {
                    claim.ClaimId,
                    AccomodationType = claim.AccommodationRequest != null
                        ? claim.AccommodationRequest.AccommodationType.Name
                        : null,
                    RoomName =
                        claim.AccommodationRequest != null &&
                        claim.AccommodationRequest.Accommodation != null
                            ? claim.AccommodationRequest.Accommodation.Name
                            : null,
                    claim.Player.PrefferedName,
                    claim.Player.BornName,
                    claim.Player.SurName,
                    claim.Player.FatherName,
                    claim.Player.Email,
                    // Телефон живёт в отдельной таблице UserExtra, и обращение к навигации
                    // 1:1-зависимой сущности EF6 строит через LEFT OUTER JOIN — у игрока без
                    // ряда колонка приедет null, а не выбросит его из отчёта. Каст к string?
                    // в SQL ничего не меняет, он только делает nullability явной.
                    PhoneNumber = (string?)claim.Player.Extra!.PhoneNumber,
                }).ToListAsync();

        // Отображаемое имя собирается в памяти: конструкторы value-типов в дерево выражений
        // EF6 не переводятся.
        return
        [
            .. rows.Select(row => new ClaimAccommodationInfoRow()
            {
                ClaimId = row.ClaimId,
                AccomodationType = row.AccomodationType,
                RoomName = row.RoomName,
                PlayerName = new UserDisplayName(
                    new UserFullName(
                        PrefferedName.FromOptional(row.PrefferedName),
                        BornName.FromOptional(row.BornName),
                        SurName.FromOptional(row.SurName),
                        FatherName.FromOptional(row.FatherName)),
                    new Email(row.Email)),
                Phone = PhoneNumber.FromOptional(row.PhoneNumber),
            })
        ];
    }

    public async Task<IReadOnlyCollection<RoomTypeInfoRow>> GetRoomTypesForProject(ProjectIdentification projectId)
    {
        var project = projectId.Value;

        // Сам тип проживания из базы не читается: его настройки приходят из метаданных проекта
        // (ADR015), поэтому запросу нужны только идентификатор и счётчики занятости.
        var rows = await ctx.Set<ProjectAccommodationType>().Where(a => a.ProjectId == project)
            .Select(x => new
            {
                x.Id,
                // cast to int? required to correctly handle SQL-LINQ nullness
                Occupied = x.ProjectAccommodations.Sum(room => room.Inhabitants.Sum(ar => (int?)ar.Subjects.Count)) ?? 0,
                RoomsCount = x.ProjectAccommodations.Count,
                ApprovedClaims = x.Desirous.Sum(ar => (int?)ar.Subjects.Count) ?? 0,
                FullyFreeRoomsCount = x.ProjectAccommodations.Count(room => (room.Inhabitants.Sum(ar => (int?)ar.Subjects.Count) ?? 0) == 0),
                FullyOccupiedRoomsCount = x.ProjectAccommodations.Count(room => (room.Inhabitants.Sum(ar => (int?)ar.Subjects.Count) ?? 0) == x.Capacity),
            })
            .ToListAsync()
            .ConfigureAwait(false);

        // Типизированный идентификатор собирается уже в памяти: конструктор в дерево выражений
        // EF6 не переводится.
        return
        [
            .. rows.Select(row => new RoomTypeInfoRow()
            {
                RoomTypeId = new AccommodationTypeIdentification(projectId, row.Id),
                Occupied = row.Occupied,
                RoomsCount = row.RoomsCount,
                ApprovedClaims = row.ApprovedClaims,
                FullyFreeRoomsCount = row.FullyFreeRoomsCount,
                FullyOccupiedRoomsCount = row.FullyOccupiedRoomsCount,
            })
        ];
    }
}
