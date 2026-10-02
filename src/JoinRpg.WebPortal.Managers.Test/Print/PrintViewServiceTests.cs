using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DataModel.Users;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.Users;
using JoinRpg.WebPortal.Managers.Print;

namespace JoinRpg.WebPortal.Managers.Test.Print;

/// <summary>
/// Сборка заглавной страницы конверта (<c>EnvelopeViewModel</c>) поверх агрегатов.
/// </summary>
/// <remarks>
/// Конверт собирается из трёх источников — агрегата персонажа, телефонов пачкой и планов
/// поселения, — и источники разъезжаются молча: страницу печати никто не проверяет глазами до
/// самой игры. Поэтому тесты сторожат не «страница открылась», а соответствие полей источникам.
/// </remarks>
public class PrintViewServiceTests
{
    private readonly MockedProject mock = new();
    private readonly FakePhoneRepository phones = new();
    private readonly RecordingRoomPlanRepository roomPlans = new();

    private PrintViewService CreateService()
        => new(new FakeCharacterInfoRepository(mock), phones, roomPlans);

    [Fact]
    public async Task Envelopes_DoNotMixUpPhonesBetweenCharacters()
    {
        var first = mock.CreateCharacter("Первый");
        var firstPlayer = mock.Player;
        _ = mock.CreateApprovedClaim(first, firstPlayer);

        var second = mock.CreateCharacter("Второй");
        var secondPlayer = mock.CreateMaster("Второй игрок");
        _ = mock.CreateApprovedClaim(second, secondPlayer);

        phones.Set(new UserIdentification(firstPlayer.UserId), "+7 900 000-00-01");
        phones.Set(new UserIdentification(secondPlayer.UserId), "+7 900 000-00-02");

        var envelopes = (await CreateService().GetEnvelopes(
            [mock.GetCharacterInfo(first), mock.GetCharacterInfo(second)]))
            .ToDictionary(envelope => envelope.CharacterName);

        envelopes["Первый"].PlayerPhoneNumber.ShouldBe("+7 900 000-00-01");
        envelopes["Второй"].PlayerPhoneNumber.ShouldBe("+7 900 000-00-02");
    }

    [Fact]
    public async Task Envelope_TakesPlayerNameAndMasterFromAggregate()
    {
        var character = mock.CreateCharacter("Элендил");
        _ = mock.CreateApprovedClaim(character, mock.Player);
        var characterInfo = mock.GetCharacterInfo(character);

        var envelope = await CreateService().GetEnvelope(characterInfo);

        envelope.CharacterId.ShouldBe(characterInfo.Id);
        envelope.CharacterName.ShouldBe("Элендил");
        envelope.PlayerDisplayName.ShouldBe(characterInfo.ApprovedClaim!.Player.DisplayName);
        envelope.ResponsibleMaster.DisplayName
            .ShouldBe(mock.ProjectInfo.GetMasterById(characterInfo.ResponsibleMasterId).Name.DisplayName);
        envelope.ProjectName.ShouldBe(mock.ProjectInfo.ProjectName);
    }

    /// <summary>
    /// Конверт печатают и на свободную роль: игрока там нет, а взнос показывается по расписанию
    /// проекта на сегодня — так же, как это делал прежний сборщик поверх EF-сущности.
    /// </summary>
    [Fact]
    public async Task Envelope_WithoutApprovedClaim_ShowsProjectFeeAndNoPlayer()
    {
        var character = mock.CreateCharacter("Свободная роль");

        var envelope = await CreateService().GetEnvelope(mock.GetCharacterInfo(character));

        envelope.FeeDue.ShouldBe(
            mock.ProjectInfo.ProjectFinanceSettings.GetFeeForDate(DateTime.UtcNow, preferential: false));
        envelope.PlayerDisplayName.ShouldBeNull();
        envelope.PlayerPhoneNumber.ShouldBeNull();
        envelope.AccommodationTypeName.ShouldBeNull();
        envelope.AccommodationName.ShouldBeNull();
    }

    /// <summary>
    /// Планы поселения — это весь пул комнат проекта с жильцами. Пока в пачке ни у кого не выбрано
    /// проживание, спрашивать их незачем: печать одного конверта не должна тянуть расселение
    /// целого проекта.
    /// </summary>
    [Fact]
    public async Task Envelopes_WithoutAccommodationInBatch_DoNotLoadRoomPlans()
    {
        var character = mock.CreateCharacter("Без поселения");
        _ = mock.CreateApprovedClaim(character, mock.Player);

        _ = await CreateService().GetEnvelope(mock.GetCharacterInfo(character));

        roomPlans.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Envelopes_EmptyBatch_TouchesNoRepositories()
    {
        var envelopes = await CreateService().GetEnvelopes([]);

        envelopes.ShouldBeEmpty();
        phones.CallCount.ShouldBe(0);
        roomPlans.CallCount.ShouldBe(0);
    }

    private sealed class RecordingRoomPlanRepository : IRoomCategoryPlanRepository
    {
        public int CallCount { get; private set; }

        public Task<IReadOnlyCollection<RoomCategoryPlan>> GetAllPlans(ProjectIdentification projectId)
        {
            CallCount++;
            return Task.FromResult<IReadOnlyCollection<RoomCategoryPlan>>([]);
        }

        public Task<RoomCategoryPlan?> GetPlanForTypeOrDefault(AccommodationTypeIdentification typeId)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Телефоны тестов. Остальные методы намеренно бросают: поход сервиса за лишним профилем
    /// должен быть виден в тесте, а не подменяться пустышкой.
    /// </summary>
    private sealed class FakePhoneRepository : IUserRepository
    {
        private readonly Dictionary<UserIdentification, PhoneNumber> phones = [];

        public int CallCount { get; private set; }

        public void Set(UserIdentification userId, string phone) => phones[userId] = new PhoneNumber(phone);

        public Task<IReadOnlyDictionary<UserIdentification, PhoneNumber>> GetPhoneNumbers(
            IReadOnlyCollection<UserIdentification> userIds)
        {
            CallCount++;
            return Task.FromResult<IReadOnlyDictionary<UserIdentification, PhoneNumber>>(
                userIds.Where(phones.ContainsKey).ToDictionary(userId => userId, userId => phones[userId]));
        }

        public Task<User> GetById(int id) => throw new NotSupportedException();
        public Task<User> WithProfile(int userId) => throw new NotSupportedException();
        public Task<User> GetWithSubscribe(int currentUserId) => throw new NotSupportedException();
        public Task<UserAvatar> LoadAvatar(AvatarIdentification userAvatarId) => throw new NotSupportedException();
        public Task<UserInfo?> GetUserInfo(UserIdentification userId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<UserInfo>> GetUserInfos(IReadOnlyCollection<UserIdentification> userIds) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<UserInfoHeader>> GetUserInfoHeaders(IReadOnlyCollection<UserIdentification> userIds) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<UserInfoHeader>> GetAdminUserInfoHeaders() => throw new NotSupportedException();
        public Task<UserIdentification?> FindByVk(string vkId) => throw new NotSupportedException();
        public Task<UserIdentification?> FindByTelegram(string telegramUsername) => throw new NotSupportedException();
        public Task<UserIdentification?> FindByEmail(string email) => throw new NotSupportedException();
    }
}
