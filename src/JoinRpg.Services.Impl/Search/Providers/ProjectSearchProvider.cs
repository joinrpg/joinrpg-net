using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search.Providers;

internal class ProjectSearchProvider(IUnitOfWork unitOfWork) : ISearchProvider
{
    public async Task<IReadOnlyCollection<SearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
        if (searchString.Length < 3)
        {
            return [];
        }
        var results =
          await
            unitOfWork.GetDbSet<Project>()
              .Where(pr => pr.ProjectName.Contains(searchString)

              )
              .ToListAsync();

        return results.Select(proj => new SearchResult
        {
            LinkType = LinkType.Project,
            Name = proj.ProjectName,
            Description = new MarkdownString(""),
            Identification = proj.ProjectId.ToString(),
            ProjectId = proj.ProjectId,
            IsPublic = true,
            IsActive = proj.Active,
        }).ToList();

    }
}
