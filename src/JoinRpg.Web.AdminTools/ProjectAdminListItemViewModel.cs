using JoinRpg.Web.Games.Projects;
using JoinRpg.Web.ProjectCommon.Projects;

namespace JoinRpg.Web.AdminTools;

public record ProjectAdminListItemViewModel(
    ProjectIdentification ProjectId,
    string ProjectName,
    DateOnly LastUpdatedAt,
    IReadOnlyCollection<KogdaIgraCardViewModel>? KiLinks
    ) : IProjectLinkViewModel;
