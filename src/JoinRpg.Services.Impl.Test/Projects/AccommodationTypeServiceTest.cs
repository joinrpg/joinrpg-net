using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Accommodation;
using JoinRpg.Services.Impl.Characters;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Impl.Projects.Metadata;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test.Projects;

/// <summary>
/// Тесты <see cref="AccommodationTypeService"/>: создание, изменение и удаление типа проживания
/// как настройки проекта (ADR015). Результат проверяется по пересобранному
/// <see cref="ProjectInfo"/> (ADR009), а не по EF-сущности.
/// </summary>
public class AccommodationTypeServiceTest : ProjectMetadataServiceTestBase
{
    /// <summary>Что репозиторий отвечает про занятость комнат этого типа.</summary>
    private bool hasOccupiedRoom;

    private readonly FakeAccommodationNotificationService accommodationNotifications = new();

    private AccommodationTypeService CreateService(int? currentUserId = null, bool isAdmin = false)
    {
        var currentUser = CreateCurrentUser(currentUserId, isAdmin);
        return new AccommodationTypeService(
            CreatePropsService(currentUser),
            CreateCharacterPropsService(currentUser),
            new FakeAccommodationRepository(mock, () => hasOccupiedRoom));
    }

    /// <summary>
    /// Боевой props-сервис агрегата персонажа поверх того же <see cref="FakeUnitOfWork"/>: через
    /// него удаление типа расформировывает группы проживания.
    /// </summary>
    private CharacterPropsService CreateCharacterPropsService(FakeCurrentUserAccessor currentUser)
        => new(
            unitOfWork,
            currentUser,
            new FieldSaveHelper(new MockedFieldDefaultValueGenerator(), NullLogger<FieldSaveHelper>.Instance),
            new CommentHelper(currentUser),
            new FakeClaimNotificationService(),
            accommodationNotifications,
            NullLogger<CharacterPropsService>.Instance);

    private Claim CreateClaim(string characterName)
    {
        var claim = mock.CreateClaim(mock.CreateCharacter(characterName), mock.Player);
        claim.ClaimStatus = ClaimStatus.AddedByUser;
        return claim;
    }

    private AccommodationTypeIdentification AddAccommodationType(string name = "Палатка")
    {
        var entity = mock.CreateAccommodationType(name);
        mock.ReInitProjectInfo();
        return new AccommodationTypeIdentification(ProjectId, entity.Id);
    }

    [Fact]
    public async Task CreateAccommodationType_AddsTypeToProjectSettings()
    {
        await CreateService().CreateAccommodationType(
            ProjectId,
            new AccommodationTypeRequest("Домик", new MarkdownString("Тёплый"), Cost: 1500, Capacity: 4, IsPlayerSelectable: true));

        var created = Result.AccommodationSettings.Types.ShouldHaveSingleItem();
        created.Name.ShouldBe("Домик");
        created.Description.Value.ShouldBe("Тёплый");
        created.Cost.ShouldBe(1500);
        created.Capacity.ShouldBe(4);
        created.IsPlayerSelectable.ShouldBeTrue();
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task CreateAccommodationType_WithoutPermission_Throws()
    {
        // Игрок (mock.Player) не входит в ACL проекта
        await Should.ThrowAsync<NoAccessToProjectException>(() => CreateService(mock.Player.UserId).CreateAccommodationType(
            ProjectId,
            new AccommodationTypeRequest("Домик", new MarkdownString(""), Cost: 0, Capacity: 1, IsPlayerSelectable: true)));

        mock.AccommodationTypes.ShouldBeEmpty();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task UpdateAccommodationType_ChangesSettings()
    {
        var id = AddAccommodationType();

        await CreateService().UpdateAccommodationType(
            id,
            new AccommodationTypeRequest("Люкс", new MarkdownString("С душем"), Cost: 9000, Capacity: 2, IsPlayerSelectable: false));

        var updated = Result.AccommodationSettings.Types.ShouldHaveSingleItem();
        updated.Name.ShouldBe("Люкс");
        updated.Description.Value.ShouldBe("С душем");
        updated.Cost.ShouldBe(9000);
        updated.Capacity.ShouldBe(2);
        updated.IsPlayerSelectable.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateAccommodationType_WithoutPermission_Throws()
    {
        var id = AddAccommodationType();

        await Should.ThrowAsync<NoAccessToProjectException>(() => CreateService(mock.Player.UserId).UpdateAccommodationType(
            id,
            new AccommodationTypeRequest("Люкс", new MarkdownString(""), Cost: 9000, Capacity: 2, IsPlayerSelectable: false)));

        mock.AccommodationTypes.ShouldHaveSingleItem().Name.ShouldBe("Палатка");
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task DeleteAccommodationType_RemovesTypeFromProjectSettings()
    {
        var id = AddAccommodationType();

        await CreateService().DeleteAccommodationType(id);

        Result.AccommodationSettings.Types.ShouldBeEmpty();
        mock.AccommodationTypes.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAccommodationType_WithoutPermission_Throws()
    {
        var id = AddAccommodationType();

        await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).DeleteAccommodationType(id));

        mock.AccommodationTypes.ShouldHaveSingleItem();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task DeleteAccommodationType_WithOccupiedRoom_Throws()
    {
        var id = AddAccommodationType();
        hasOccupiedRoom = true;

        _ = await Should.ThrowAsync<AccommodationTypeIsOccupiedException>(
            () => CreateService().DeleteAccommodationType(id));

        mock.AccommodationTypes.ShouldHaveSingleItem();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Нерасселённая группа проживания удаляемого типа. В бою база каскадом удаляла группу вместе
    /// с типом, а заявки продолжали на неё ссылаться — удаление падало на внешнем ключе
    /// <c>Claims.AccommodationRequest_Id</c>. Теперь группа расформировывается до удаления типа.
    /// </summary>
    [Fact]
    public async Task DeleteAccommodationType_WithUnplacedGroup_DisbandsGroup_AndDeletesType()
    {
        var claim = CreateClaim("Вася");
        var neighbour = CreateClaim("Петя");
        var bystander = CreateClaim("Коля");
        var type = mock.CreateAccommodationType("Палатка");
        var otherType = mock.CreateAccommodationType("Домик");
        _ = mock.CreateAccommodationRequest(type, claim, neighbour);
        var otherGroup = mock.CreateAccommodationRequest(otherType, bystander);
        mock.ReInitProjectInfo();

        await CreateService().DeleteAccommodationType(new AccommodationTypeIdentification(ProjectId, type.Id));

        claim.AccommodationRequest.ShouldBeNull();
        claim.AccommodationRequest_Id.ShouldBeNull();
        neighbour.AccommodationRequest.ShouldBeNull();
        neighbour.AccommodationRequest_Id.ShouldBeNull();
        mock.AccommodationRequests.ShouldBe([otherGroup]);
        bystander.AccommodationRequest.ShouldBe(otherGroup);

        Result.AccommodationSettings.Types.ShouldHaveSingleItem().Name.ShouldBe("Домик");
        mock.AccommodationTypes.ShouldBe([otherType]);
        accommodationNotifications.RoomOccupancy.ShouldBeEmpty();
    }

    /// <summary>
    /// Неотвеченные приглашения участников расформированной группы отклоняются автоматически —
    /// звать больше некуда. Отвеченные — история, их не трогаем.
    /// </summary>
    [Fact]
    public async Task DeleteAccommodationType_WithUnplacedGroup_DeclinesUnansweredInvites()
    {
        var claim = CreateClaim("Вася");
        var invited = CreateClaim("Петя");
        var type = mock.CreateAccommodationType("Палатка");
        _ = mock.CreateAccommodationRequest(type, claim);
        var pending = mock.CreateAccommodationInvite(claim, invited);
        var answered = mock.CreateAccommodationInvite(invited, claim);
        answered.IsAccepted = InviteState.Canceled;
        answered.ResolveDescription = ResolveDescription.Canceled;
        mock.ReInitProjectInfo();

        await CreateService().DeleteAccommodationType(new AccommodationTypeIdentification(ProjectId, type.Id));

        pending.IsAccepted.ShouldBe(InviteState.Declined);
        pending.ResolveDescription.ShouldBe(ResolveDescription.DeclinedAuto);
        answered.IsAccepted.ShouldBe(InviteState.Canceled);
        answered.ResolveDescription.ShouldBe(ResolveDescription.Canceled);

        var notification = accommodationNotifications.Invites.ShouldHaveSingleItem();
        notification.RecipientClaims.ShouldBe([invited.GetId()]);
        notification.Kind.ShouldBe(InviteChangeKind.Cancelled);
    }

    /// <summary>
    /// Расформирование групп требует того же права, что и удаление типа, — а не права расселять
    /// игроков, которого у мастера может и не быть.
    /// </summary>
    [Fact]
    public async Task DeleteAccommodationType_WithUnplacedGroup_ByMasterWithoutSetAccommodationsRight_Works()
    {
        var master = mock.CreateMaster();
        var acl = mock.Project.ProjectAcls.Single(a => a.UserId == master.UserId);
        acl.CanSetPlayersAccommodations = false;
        var claim = CreateClaim("Вася");
        var type = mock.CreateAccommodationType("Палатка");
        _ = mock.CreateAccommodationRequest(type, claim);
        mock.ReInitProjectInfo();

        await CreateService(master.UserId).DeleteAccommodationType(new AccommodationTypeIdentification(ProjectId, type.Id));

        claim.AccommodationRequest.ShouldBeNull();
        mock.AccommodationTypes.ShouldBeEmpty();
    }

    /// <summary>
    /// Гонка: проверка занятости сказала «свободно», но к моменту мутации группу успели расселить.
    /// Удалять тип уже нельзя, и ничего не сохраняется.
    /// </summary>
    [Fact]
    public async Task DeleteAccommodationType_WhenGroupPlacedAfterOccupancyCheck_Throws_AndDoesNotSave()
    {
        var claim = CreateClaim("Вася");
        var type = mock.CreateAccommodationType("Палатка");
        var group = mock.CreateAccommodationRequest(type, claim);
        _ = mock.CreateRoom(group);
        mock.ReInitProjectInfo();
        hasOccupiedRoom = false;

        _ = await Should.ThrowAsync<AccommodationTypeIsOccupiedException>(
            () => CreateService().DeleteAccommodationType(new AccommodationTypeIdentification(ProjectId, type.Id)));

        claim.AccommodationRequest.ShouldBe(group);
        mock.AccommodationRequests.ShouldBe([group]);
        mock.AccommodationTypes.ShouldHaveSingleItem();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Репозиторий назвал заявку, но к моменту мутации она уже не в группе удаляемого типа — её
    /// мутация холостая, а тип всё равно удаляется.
    /// </summary>
    [Fact]
    public async Task DeleteAccommodationType_WhenClaimAlreadyLeftGroup_SkipsIt_AndDeletesType()
    {
        var claim = CreateClaim("Вася");
        var type = mock.CreateAccommodationType("Палатка");
        var otherType = mock.CreateAccommodationType("Домик");
        var otherGroup = mock.CreateAccommodationRequest(otherType, claim);
        mock.ReInitProjectInfo();

        var service = new AccommodationTypeService(
            CreatePropsService(CreateCurrentUser()),
            CreateCharacterPropsService(CreateCurrentUser()),
            new FakeAccommodationRepository(mock, () => false, staleClaims: [claim.GetId()]));

        await service.DeleteAccommodationType(new AccommodationTypeIdentification(ProjectId, type.Id));

        claim.AccommodationRequest.ShouldBe(otherGroup);
        mock.AccommodationRequests.ShouldBe([otherGroup]);
        mock.AccommodationTypes.ShouldBe([otherType]);
        // Сохранение одно — удаление типа; холостая мутация заявки не сохраняет.
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task DeleteAccommodationType_WithUnplacedGroup_WithoutPermission_Throws_AndKeepsGroup()
    {
        var claim = CreateClaim("Вася");
        var type = mock.CreateAccommodationType("Палатка");
        var group = mock.CreateAccommodationRequest(type, claim);
        mock.ReInitProjectInfo();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).DeleteAccommodationType(
                new AccommodationTypeIdentification(ProjectId, type.Id)));

        claim.AccommodationRequest.ShouldBe(group);
        mock.AccommodationRequests.ShouldBe([group]);
        mock.AccommodationTypes.ShouldHaveSingleItem();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Занятость комнат — оперативные данные вне метаданных проекта, сервис берёт их отдельным
    /// запросом. Фейк отдаёт то, что задал тест, а состав групп — по моку. <paramref name="staleClaims"/>
    /// подменяет список заявок устаревшим — так выглядит гонка с выходом заявки из группы.
    /// </summary>
    private sealed class FakeAccommodationRepository(
        MockedProject mock,
        Func<bool> hasOccupiedRoom,
        IReadOnlyCollection<ClaimIdentification>? staleClaims = null)
        : IAccommodationRepository
    {
        public Task<bool> HasOccupiedRoomOfType(AccommodationTypeIdentification accommodationTypeId)
            => Task.FromResult(hasOccupiedRoom());

        public Task<IReadOnlyCollection<ClaimIdentification>> GetClaimsInGroupsOfType(
            AccommodationTypeIdentification accommodationTypeId)
            => Task.FromResult<IReadOnlyCollection<ClaimIdentification>>(staleClaims ?? [.. mock.AccommodationRequests
                    .Where(request => request.AccommodationTypeId == accommodationTypeId.AccommodationTypeId)
                    .SelectMany(request => request.Subjects)
                    .Select(claim => claim.GetId())]);

        public Task<IReadOnlyCollection<ClaimAccommodationInfoRow>> GetClaimAccommodationReport(int project)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<RoomTypeInfoRow>> GetRoomTypesForProject(ProjectIdentification projectId)
            => throw new NotSupportedException();
    }
}
