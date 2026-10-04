using System.Web;
using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Projects;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Test;

/// <summary>
/// У <see cref="IProjectUriLocator"/> нет клиентской реализации, сверять серверную не с чем.
/// Поэтому путь закреплён здесь: ссылки ведут в экшены MVC, и переименование экшена или
/// параметра сломало бы кнопки списка молча.
/// </summary>
public class ProjectUriLocatorTest(IntegrationTestPortalFactory factory) : IClassFixture<IntegrationTestPortalFactory>
{
    private static readonly ProjectIdentification ProjectId = new(1620);

    private IProjectUriLocator Locator => factory.Services.GetRequiredService<IProjectUriLocator>();

    [Fact]
    public void CreateCharacterWithoutGroup()
        => Locator.GetCreateCharacterUri(ProjectId).AbsolutePath.ShouldBe("/1620/character/create", StringCompareShould.IgnoreCase);

    [Fact]
    public void MassMailKeepsClaimIds()
    {
        int[] ids = [13, 451, 892, 84514];
        var uri = Locator.GetMassMailUri(ProjectId, [.. ids.Select(id => new ClaimIdentification(ProjectId, id))]);

        uri.AbsolutePath.ShouldBe("/1620/massmail/ForClaims", StringCompareShould.IgnoreCase);
        ParseQueryList(uri, "claimIds").ShouldBe(ids, ignoreOrder: true);
    }

    [Fact]
    public void PrintKeepsCharacterIds()
    {
        int[] ids = [1, 2, 3, 1358, 1359];
        var uri = Locator.GetPrintCharactersUri(ProjectId, [.. ids.Select(id => new CharacterIdentification(ProjectId, id))]);

        uri.AbsolutePath.ShouldBe("/1620/print/CharacterList", StringCompareShould.IgnoreCase);
        ParseQueryList(uri, "characterIds").ShouldBe(ids, ignoreOrder: true);
    }

    private static IReadOnlyCollection<int> ParseQueryList(Uri uri, string name)
    {
        var query = HttpUtility.ParseQueryString(uri.Query);
        var key = query.AllKeys.Single(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
        return CompressedIntList.Parse(query[key]!).List;
    }
}
