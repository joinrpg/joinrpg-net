using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebPortal.Managers.Characters;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.WebPortal.Managers.Test.Characters;

public class ApiInfoBuilderTest
{
    /// <summary>
    /// Статус в контракте внешнего API совпадает с доменным по имени: так клиенты API видели его,
    /// пока он получался приведением enum по числу.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllStatuses))]
    public void ToApiStatus_KeepsStatusName(ClaimStatus status)
        => ApiInfoBuilder.ToApiStatus(status).ToString().ShouldBe(status.ToString());

    public static TheoryData<ClaimStatus> AllStatuses() => [.. Enum.GetValues<ClaimStatus>()];

    [Fact]
    public void ContractHasNoStatusesUnknownToDomain()
        => Enum.GetNames<ClaimStatusEnum>().ShouldBe(Enum.GetNames<ClaimStatus>(), ignoreOrder: true);

    /// <summary>
    /// Каждый внутренний статус занятости даёт значение, которое есть во внешнем контракте:
    /// раньше статус приводился по числу, и шаблоны с горячими вакансиями уходили наружу как 4–7.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllBusyStatuses))]
    public void ToBusyStatus_GivesDefinedContractValue(CharacterBusyStatusView status)
        => Enum.IsDefined(ApiInfoBuilder.ToBusyStatus(status)).ShouldBeTrue();

    public static TheoryData<CharacterBusyStatusView> AllBusyStatuses() => [.. Enum.GetValues<CharacterBusyStatusView>()];

    [Theory]
    [InlineData(CharacterBusyStatusView.HasPlayer, CharacterBusyStatus.HasPlayer)]
    [InlineData(CharacterBusyStatusView.Discussed, CharacterBusyStatus.Discussed)]
    [InlineData(CharacterBusyStatusView.Npc, CharacterBusyStatus.Npc)]
    [InlineData(CharacterBusyStatusView.Vacancy, CharacterBusyStatus.NoClaims)]
    [InlineData(CharacterBusyStatusView.HotVacancy, CharacterBusyStatus.NoClaims)]
    [InlineData(CharacterBusyStatusView.Slot, CharacterBusyStatus.NoClaims)]
    [InlineData(CharacterBusyStatusView.HotSlot, CharacterBusyStatus.NoClaims)]
    public void ToBusyStatus_Maps(CharacterBusyStatusView status, CharacterBusyStatus expected)
        => ApiInfoBuilder.ToBusyStatus(status).ShouldBe(expected);
}
