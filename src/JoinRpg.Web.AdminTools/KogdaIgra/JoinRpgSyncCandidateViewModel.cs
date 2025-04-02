using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.AdminTools.KogdaIgra;
public record class JoinRpgSyncCandidateViewModel(
    ProjectIdentification ProjectId, string Name, UserLinkViewModel[] Masters, DateTimeOffset LastUpdatedAt, MarkdownString Description)
{
}
