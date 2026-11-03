using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.Web.Claims.UnifiedGrid;

namespace JoinRpg.Web.Claims.Test.UnifiedGrid;

/// <summary>
/// Адрес грида — контракт 301-редиректов со старых списков заявок (ADR027 §6): браузер кеширует
/// 301, поэтому имена параметров и формат значений фиксируются до первого редиректа. Тесты
/// закрепляют адрес целиком, а не по частям, чтобы изменение порядка или формата было видно.
/// </summary>
public class ClaimGridUrlsTest
{
    private static readonly ProjectIdentification ProjectId = new(1620);
    private static readonly CharacterGroupIdentification GroupId = new(ProjectId, 43014);
    private static readonly UserIdentification MasterId = new(51);

    [Fact]
    public void DefaultFilter_IsBareGridAddress()
        => ClaimGridUrls.Build(ProjectId).ShouldBe("/1620/claims");

    [Fact]
    public void ActiveStatus_IsDefaultValue_AndNotWritten()
        => ClaimGridUrls.Build(ProjectId, UgStatusFilterView.Active).ShouldBe("/1620/claims");

    [Theory]
    [InlineData(UgStatusFilterView.Discussion, "Discussion")]
    [InlineData(UgStatusFilterView.OnHold, "OnHold")]
    [InlineData(UgStatusFilterView.All, "All")]
    public void Status_IsWrittenByName(UgStatusFilterView status, string expected)
        => ClaimGridUrls.Build(ProjectId, status).ShouldBe($"/1620/claims?status={expected}");

    [Fact]
    public void FirstPage_IsDefaultValue_AndNotWritten()
        => ClaimGridUrls.Build(ProjectId, page: 1).ShouldBe("/1620/claims");

    [Fact]
    public void Page_IsWrittenFromSecond()
        => ClaimGridUrls.Build(ProjectId, page: 3).ShouldBe("/1620/claims?page=3");

    [Fact]
    public void OnlyWithProblems_IsWrittenAsTrue()
        => ClaimGridUrls
            .Build(ProjectId, UgStatusFilterView.All, onlyWithProblems: true)
            .ShouldBe("/1620/claims?status=All&onlyWithProblems=true");

    [Fact]
    public void GroupAndResponsible_AreWrittenInParseableForm()
        => ClaimGridUrls
            .Build(ProjectId, responsibleMasterId: MasterId, characterGroupId: GroupId)
            .ShouldBe(
                "/1620/claims"
                + $"?characterGroupId={Uri.EscapeDataString(GroupId.ToString())}"
                + $"&responsibleMasterId={Uri.EscapeDataString(MasterId.ToString())}");

    [Fact]
    public void Export_IsAppendedLast()
        => ClaimGridUrls
            .Build(ProjectId, UgStatusFilterView.OnHold, page: 2, export: "xlsx")
            .ShouldBe("/1620/claims?status=OnHold&page=2&export=xlsx");

    /// <summary>
    /// Формат значений typed id фиксирован кешированными 301: «<c>CharacterGroupId(1620-43014)</c>» —
    /// это <c>ToString()</c> типизированного id, как у самого грида; короткая форма тоже бы
    /// разбиралась биндером страницы, но менять уже нельзя.
    /// </summary>
    [Fact]
    public void TypedIdFormat_IsPinned()
        => ClaimGridUrls.Build(ProjectId, characterGroupId: GroupId)
            .ShouldBe("/1620/claims?characterGroupId=CharacterGroupId%281620-43014%29");
}
