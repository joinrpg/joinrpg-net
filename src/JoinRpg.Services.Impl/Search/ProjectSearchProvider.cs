using Microsoft.EntityFrameworkCore;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search;

internal class ProjectSearchProvider : ISearchProvider
{
    public IUnitOfWork UnitOfWork { protected get; set; }
    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString)
    {

        var results =
          await
            UnitOfWork.GetDbSet<Project>()
              .Where(pr =>
                pr.Active == true && pr.ProjectName.Contains(searchString)

              )
              .ToListAsync();

        return results.Select(proj => new SearchResultImpl
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
