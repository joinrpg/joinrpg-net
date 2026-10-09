using JoinRpg.DomainTypes.Characters.Claims;
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
}
