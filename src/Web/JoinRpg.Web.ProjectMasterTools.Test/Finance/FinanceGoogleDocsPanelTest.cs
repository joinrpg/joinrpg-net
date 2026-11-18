using Bunit;
using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectMasterTools.Finance;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.Finance;

public class FinanceGoogleDocsPanelTest : BunitContext
{
    [Fact]
    public void FormulaImportsPersonalExportLink()
    {
        Services.AddSingleton<IFinanceSettingsClient>(new FakeFinanceSettingsClient());

        var cut = Render<FinanceGoogleDocsPanel>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1)));

        cut.Find("code").TextContent.ShouldBe("=IMPORTDATA(\"https://joinrpg.ru/1/money/SummaryByMaster?token=0123\")");
    }
}
