using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.Web.Claims.UnifiedGrid;
using JoinRpg.WebPortal.Managers.UnifiedGrid;

namespace JoinRpg.WebPortal.Managers.Test.UnifiedGrid;

/// <summary>
/// Заголовок файла выгрузки грида строится из фильтра (ADR027 §5): по имени файла видно, какой
/// список в нём лежит.
/// </summary>
public class ClaimListExportServiceTest
{
    private MockedProject Mock { get; } = new();

    [Fact]
    public void TitleWithoutFiltersIsJustStatus()
        => ClaimListExportService.BuildTitle(new UgFilterView(UgStatusFilterView.Approved), Mock.ProjectInfo)
            .ShouldBe("Заявки: Приняты");

    [Fact]
    public void TitleDescribesAllFilters()
    {
        var title = ClaimListExportService.BuildTitle(
            new UgFilterView(
                UgStatusFilterView.Active,
                OnlyWithProblems: true,
                Group: new CharacterGroupIdentification(Mock.ProjectInfo.ProjectId, Mock.Group.CharacterGroupId),
                Responsible: new UserIdentification(Mock.Master.UserId)),
            Mock.ProjectInfo);

        title.ShouldStartWith("Заявки: Активные");
        title.ShouldContain($"группа «{Mock.Group.CharacterGroupName}»");
        title.ShouldContain("ответственный — ");
        title.ShouldEndWith("только с проблемами");
    }
}
