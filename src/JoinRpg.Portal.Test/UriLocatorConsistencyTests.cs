using JoinRpg.Blazor.Client;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon;
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

    private static string NormalizePathAndQuery(Uri uri) =>
        uri.IsAbsoluteUri ? uri.PathAndQuery : uri.ToString();
}
