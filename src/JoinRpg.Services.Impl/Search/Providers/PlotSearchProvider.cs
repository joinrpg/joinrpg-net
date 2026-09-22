using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search;

internal class PlotSearchProvider(IUnitOfWork unitOfWork) : IProjectScopedSearchProvider
{
    public LinkType LinkType => LinkType.Plot;

    public async Task<IReadOnlyCollection<SearchResult>> SearchAsync(int? currentUserId, string searchString, ProjectIdentification? projectId)
    {
        if (searchString.Length < 3)
        {
            return [];
        }

        var query =
            unitOfWork.GetDbSet<PlotFolder>()
             .Where(p =>
               p.IsActive && p.Project.ProjectAcls.Any(acl => acl.UserId == currentUserId) && p.MasterTitle.Contains(searchString)
             );

        query = query.FilterByProject(projectId, p => p.ProjectId);

        var results = await query.ToListAsync();

        return results.Select(plot => new SearchResult
        {
            LinkType = LinkType.Plot,
            Name = plot.MasterTitle,
            Description = new MarkdownDbValue(""),
            Identification = plot.PlotFolderId.ToString(),
            ProjectId = plot.ProjectId,
            IsPublic = false,
            IsActive = plot.IsActive,
        }).ToList();
    }
}
