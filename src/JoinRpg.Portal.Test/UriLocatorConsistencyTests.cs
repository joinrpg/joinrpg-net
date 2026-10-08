using JoinRpg.Blazor.Client;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Projects;
using JoinRpg.Web.Schedule;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Test;

/// <summary>
/// Серверная (<see cref="JoinRpg.Portal.Infrastructure.UriServiceImpl"/>) и Blazor-клиентская
/// (<see cref="UriLocatorExtensions"/>) реализации локаторов строят одни и те же ссылки независимо друг от друга.
/// Этот тест проверяет, что они не расходятся.
/// </summary>
public class UriLocatorConsistencyTests(IntegrationTestPortalFactory factory)
    : IClassFixture<IntegrationTestPortalFactory>
{
    private static readonly ProjectIdentification ProjectId = new(1620);
    private static readonly CharacterGroupIdentification GroupId = new(ProjectId, 43014);
    private static readonly CharacterIdentification CharId = new(ProjectId, 999);
    private static readonly ProjectFieldIdentification FieldId = new(ProjectId, 7);
    private static readonly ProjectFieldVariantIdentification VariantId = new(FieldId, 3);

    private readonly IServiceProvider _clientServices = new ServiceCollection().AddUriLocator().BuildServiceProvider();

    public static IEnumerable<object[]> GroupCases() =>
    [
        ["GetClaimListUri", (Func<ICharacterGroupUriLocator, Uri>)(l => l.GetClaimListUri(GroupId))],
        ["GetDiscussingClaimListUri", (Func<ICharacterGroupUriLocator, Uri>)(l => l.GetDiscussingClaimListUri(GroupId))],
        ["GetCharacterListUri", (Func<ICharacterGroupUriLocator, Uri>)(l => l.GetCharacterListUri(GroupId))],
        ["GetReportUri", (Func<ICharacterGroupUriLocator, Uri>)(l => l.GetReportUri(GroupId))],
        ["GetSubscribeUri", (Func<ICharacterGroupUriLocator, Uri>)(l => l.GetSubscribeUri(GroupId))],
        ["GetEditUri", (Func<ICharacterGroupUriLocator, Uri>)(l => l.GetEditUri(GroupId))],
        ["GetDeleteUri", (Func<ICharacterGroupUriLocator, Uri>)(l => l.GetDeleteUri(GroupId))],
        ["GetCreateCharacterUri", (Func<ICharacterGroupUriLocator, Uri>)(l => l.GetCreateCharacterUri(GroupId))],
        ["GetAddGroupUri", (Func<ICharacterGroupUriLocator, Uri>)(l => l.GetAddGroupUri(GroupId))],
    ];

    [Theory]
    [MemberData(nameof(GroupCases))]
    public void CharacterGroupLocatorsShouldAgree(string caseName, Func<ICharacterGroupUriLocator, Uri> call)
    {
        _ = caseName;
        var server = call(factory.Services.GetRequiredService<ICharacterGroupUriLocator>());
        var client = call(_clientServices.GetRequiredService<ICharacterGroupUriLocator>());
        NormalizePathAndQuery(client).ShouldBe(NormalizePathAndQuery(server), StringCompareShould.IgnoreCase);
    }

    public static IEnumerable<object[]> CharacterCases() =>
    [
        ["GetDetailsUri", (Func<ICharacterUriLocator, Uri>)(l => l.GetDetailsUri(CharId))],
        ["GetAddClaimUri", (Func<ICharacterUriLocator, Uri>)(l => l.GetAddClaimUri(CharId))],
        ["GetEditUri", (Func<ICharacterUriLocator, Uri>)(l => l.GetEditUri(CharId))],
    ];

    [Theory]
    [MemberData(nameof(CharacterCases))]
    public void CharacterLocatorsShouldAgree(string caseName, Func<ICharacterUriLocator, Uri> call)
    {
        _ = caseName;
        var server = call(factory.Services.GetRequiredService<ICharacterUriLocator>());
        var client = call(_clientServices.GetRequiredService<ICharacterUriLocator>());
        NormalizePathAndQuery(client).ShouldBe(NormalizePathAndQuery(server), StringCompareShould.IgnoreCase);
    }

    public static IEnumerable<object[]> ProjectFieldCases() =>
    [
        ["GetEditUri", (Func<IProjectFieldUriLocator, Uri>)(l => l.GetEditUri(FieldId))],
        ["GetCreateVariantUri", (Func<IProjectFieldUriLocator, Uri>)(l => l.GetCreateVariantUri(FieldId))],
        ["GetEditVariantUri", (Func<IProjectFieldUriLocator, Uri>)(l => l.GetEditVariantUri(VariantId))],
    ];

    [Theory]
    [MemberData(nameof(ProjectFieldCases))]
    public void ProjectFieldLocatorsShouldAgree(string caseName, Func<IProjectFieldUriLocator, Uri> call)
    {
        _ = caseName;
        var server = call(factory.Services.GetRequiredService<IProjectFieldUriLocator>());
        var client = call(_clientServices.GetRequiredService<IProjectFieldUriLocator>());
        NormalizePathAndQuery(client).ShouldBe(NormalizePathAndQuery(server), StringCompareShould.IgnoreCase);
    }

    public static IEnumerable<object[]> ProjectCases() =>
    [
        ["GetMyClaimUri", (Func<IProjectUriLocator, Uri>)(l => l.GetMyClaimUri(ProjectId))],
        ["GetAddClaimUri", (Func<IProjectUriLocator, Uri>)(l => l.GetAddClaimUri(ProjectId))],
        ["GetCreatePlotUri", (Func<IProjectUriLocator, Uri>)(l => l.GetCreatePlotUri(ProjectId))],
        ["GetRolesListUri", (Func<IProjectUriLocator, Uri>)(l => l.GetRolesListUri(ProjectId))],
        ["GetCaptainCabinetUri", (Func<IProjectUriLocator, Uri>)(l => l.GetCaptainCabinetUri(ProjectId))],
        ["GetCreateCharacterUri", (Func<IProjectUriLocator, Uri>)(l => l.GetCreateCharacterUri(ProjectId))],
        ["GetMassMailUri", (Func<IProjectUriLocator, Uri>)(l => l.GetMassMailUri(ProjectId, [new(ProjectId, 13), new(ProjectId, 451), new(ProjectId, 452)]))],
        ["GetPrintCharactersUri", (Func<IProjectUriLocator, Uri>)(l => l.GetPrintCharactersUri(ProjectId, [CharId, new(ProjectId, 1000), new(ProjectId, 1358)]))],
        // Большие разрывы между id дают в CompressedIntList запятую — проверяем, что обе стороны экранируют её одинаково.
        ["GetMassMailUri with comma", (Func<IProjectUriLocator, Uri>)(l => l.GetMassMailUri(ProjectId, [new(ProjectId, 100), new(ProjectId, 200), new(ProjectId, 300)]))],
    ];

    [Theory]
    [MemberData(nameof(ProjectCases))]
    public void ProjectLocatorsShouldAgree(string caseName, Func<IProjectUriLocator, Uri> call)
    {
        _ = caseName;
        var server = call(factory.Services.GetRequiredService<IProjectUriLocator>());
        var client = call(_clientServices.GetRequiredService<IProjectUriLocator>());
        NormalizePathAndQuery(client).ShouldBe(NormalizePathAndQuery(server), StringCompareShould.IgnoreCase);
    }

    [Fact]
    public void CharacterGroupLinkShouldPointToDetails()
    {
        var server = factory.Services.GetRequiredService<IUriLocator<CharacterGroupIdentification>>().GetUri(GroupId);
        var client = _clientServices.GetRequiredService<IUriLocator<CharacterGroupIdentification>>().GetUri(GroupId);

        NormalizePathAndQuery(client).ShouldBe(NormalizePathAndQuery(server), StringCompareShould.IgnoreCase);
        NormalizePathAndQuery(server).ShouldEndWith("/details", Case.Insensitive);
    }

    /// <summary>
    /// Сверх обычной сверки серверной реализации с клиентской здесь закреплён и сам путь:
    /// иначе переименование action'а в <c>ShowScheduleController</c> сломало бы единственный
    /// выход из полноэкранного режима расписания молча — обе реализации независимы, но
    /// клиентская задана строкой и о роутах ничего не знает.
    /// </summary>
    [Fact]
    public void ScheduleLocatorsShouldAgree()
    {
        var server = factory.Services.GetRequiredService<IScheduleUriLocator>().GetScheduleUri(ProjectId);
        var client = _clientServices.GetRequiredService<IScheduleUriLocator>().GetScheduleUri(ProjectId);

        NormalizePathAndQuery(client).ShouldBe(NormalizePathAndQuery(server), StringCompareShould.IgnoreCase);
        NormalizePathAndQuery(server).ShouldBe($"/{ProjectId.Value}/schedule", StringCompareShould.IgnoreCase);
    }

    [Fact]
    public void ScheduleIcalLocatorsShouldAgree()
    {
        var server = factory.Services.GetRequiredService<IScheduleUriLocator>().GetIcalUri(ProjectId);
        var client = _clientServices.GetRequiredService<IScheduleUriLocator>().GetIcalUri(ProjectId);

        NormalizePathAndQuery(client).ShouldBe(NormalizePathAndQuery(server), StringCompareShould.IgnoreCase);
        NormalizePathAndQuery(server).ShouldBe($"/{ProjectId.Value}/schedule/ical", StringCompareShould.IgnoreCase);
    }

    [Fact]
    public void ScheduleFullScreenLocatorsShouldAgree()
    {
        var server = factory.Services.GetRequiredService<IScheduleUriLocator>().GetFullScreenUri(ProjectId);
        var client = _clientServices.GetRequiredService<IScheduleUriLocator>().GetFullScreenUri(ProjectId);

        NormalizePathAndQuery(client).ShouldBe(NormalizePathAndQuery(server), StringCompareShould.IgnoreCase);
        NormalizePathAndQuery(server).ShouldBe($"/{ProjectId.Value}/schedule/full", StringCompareShould.IgnoreCase);
    }

    private static string NormalizePathAndQuery(Uri uri) =>
        uri.IsAbsoluteUri ? uri.PathAndQuery : uri.ToString();
}
