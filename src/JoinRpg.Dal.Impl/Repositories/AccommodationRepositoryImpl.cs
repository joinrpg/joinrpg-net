using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

public class AccommodationRepositoryImpl(MyDbContext ctx) : IAccommodationRepository
{
    public async Task<bool> HasOccupiedRoomOfType(AccommodationTypeIdentification accommodationTypeId)
    {
        // Занят тип, а не комната: комната принадлежит категории, и в ней могут жить группы
        // сестринских типов (ADR020). Поэтому ищем расселённые группы именно этого типа.
        return await ctx.Set<AccommodationRequest>()
            .AnyAsync(group => group.ProjectId == accommodationTypeId.ProjectId.Value
                && group.AccommodationTypeId == accommodationTypeId.AccommodationTypeId
                && group.AccommodationId != null)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<ClaimIdentification>> GetClaimsInGroupsOfType(
        AccommodationTypeIdentification accommodationTypeId)
    {
        var projectId = accommodationTypeId.ProjectId.Value;
        var typeId = accommodationTypeId.AccommodationTypeId;
        var claimIds = await ctx.Set<AccommodationRequest>()
            .Where(request => request.ProjectId == projectId && request.AccommodationTypeId == typeId)
            .SelectMany(request => request.Subjects)
            .Select(claim => claim.ClaimId)
            .ToListAsync()
            .ConfigureAwait(false);
        return [.. claimIds.Select(claimId => new ClaimIdentification(accommodationTypeId.ProjectId, claimId))];
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

}
