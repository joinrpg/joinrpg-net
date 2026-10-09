using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Accommodation.Rooms;
using JoinRpg.Web.ProjectCommon.Claims;

namespace JoinRpg.WebPortal.Managers.Accommodation;

/// <summary>
/// Страница «Комнаты» одного типа проживания: серверная реализация контрола расселения.
/// </summary>
/// <remarks>
/// Денег в плане поселения нет (ADR018, §5): жильцов он несёт идентификаторами заявок, а баланс
/// и имена игроков берутся из агрегата персонажа (ADR013) — одной общей выборкой на страницу,
/// а не по запросу на жильца. Права на мутации проверяет <see cref="IAccommodationService"/>.
/// </remarks>
public class RoomTypeRoomsViewService(
    IRoomCategoryPlanRepository roomCategoryPlanRepository,
    ICharacterInfoRepository characterInfoRepository,
    IProjectMetadataRepository projectMetadataRepository,
    IAccommodationService accommodationService,
    ICurrentUserAccessor currentUserAccessor) : IAccommodationRoomsClient
{
    /// <summary>
    /// Модель страницы или <c>null</c>, если такого типа проживания в проекте нет.
    /// </summary>
    public async Task<RoomTypeRoomsViewModel?> GetRoomsOrDefault(AccommodationTypeIdentification typeId)
    {
        var plan = await roomCategoryPlanRepository.GetPlanForTypeOrDefault(typeId);
        if (plan is null)
        {
            return null;
        }

        return RoomTypeRoomsViewModelBuilder.Build(
            plan,
            typeId,
            await LoadResidents(plan),
            currentUserAccessor.UserIdentification);
    }

    /// <inheritdoc />
    public async Task<RoomTypeRoomsViewModel> GetRooms(AccommodationTypeIdentification typeId)
        => await GetRoomsOrDefault(typeId) ?? throw new AccommodationTypeNotFoundException(typeId);

    /// <inheritdoc />
    /// <exception cref="FieldRequiredException">В строке не нашлось ни одного имени комнаты</exception>
    public async Task<RoomTypeRoomsViewModel> AddRooms(AccommodationTypeIdentification typeId, string roomNames)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(typeId.ProjectId);

        // Категорию по типу проживания знают только метаданные — конвертации идентификаторов
        // в домене нет и заводить её нельзя (ADR018, §2).
        var typeInfo = projectInfo.AccommodationSettings.GetTypeByIdOrDefault(typeId)
            ?? throw new AccommodationTypeNotFoundException(typeId);

        // Синтаксис поля ввода («1,2,5-8») разбирает web-слой: сервис принимает готовые имена.
        var names = RoomNamesParser.Parse(roomNames);
        if (names.Count == 0)
        {
            throw new FieldRequiredException(nameof(roomNames));
        }

        _ = await accommodationService.AddRooms(typeInfo.RoomCategoryId, names);
        return await GetRooms(typeId);
    }

    /// <inheritdoc />
    public Task RenameRoom(AccommodationRoomIdentification roomId, string name)
        => accommodationService.RenameRoom(roomId, name);

    /// <inheritdoc />
    public Task DeleteRoom(AccommodationRoomIdentification roomId)
        => accommodationService.DeleteRoom(roomId);

    /// <inheritdoc />
    public Task OccupyRoom(
        AccommodationRoomIdentification roomId,
        IReadOnlyCollection<AccommodationRequestIdentification> groupIds)
        => accommodationService.OccupyRoom(roomId, groupIds);

    /// <inheritdoc />
    public Task UnOccupyGroup(AccommodationRequestIdentification groupId)
        => accommodationService.UnOccupyGroup(groupId);

    /// <inheritdoc />
    public Task UnOccupyRoom(AccommodationRoomIdentification roomId)
        => accommodationService.UnOccupyRoom(roomId);

    /// <inheritdoc />
    public Task UnOccupyRoomType(AccommodationTypeIdentification typeId)
        => accommodationService.UnOccupyRoomType(typeId);

    private async Task<IReadOnlyDictionary<ClaimIdentification, RoomResidentWithFee>> LoadResidents(RoomCategoryPlan plan)
    {
        var claimIds = plan.Groups.SelectMany(group => group.Subjects).ToHashSet();

        if (claimIds.Count == 0)
        {
            return new Dictionary<ClaimIdentification, RoomResidentWithFee>();
        }

        var characters = await characterInfoRepository.GetCharacterInfosByClaims(claimIds);

        return characters
            .SelectMany(character => character.Claims.Select(claim => new ClaimInCharacter(character, claim)))
            .Where(claim => claimIds.Contains(claim.ClaimId))
            .ToDictionary(
                claim => claim.ClaimId,
                claim =>
                {
                    var balance = claim.CalculateBalance();
                    return new RoomResidentWithFee(
                        new ClaimLinkViewModel(
                            claim.ClaimId,
                            claim.Claim.Player.DisplayName,
                            claim.Character.CharacterName,
                            OtherPlayerNicks: "",
                            claim.Claim.PlayerId),
                        balance.TotalFee,
                        balance.FeeDue);
                });
    }
}
