using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search;

internal class ProjectSearchProvider : ISearchProvider
{
    private readonly IUnitOfWork unitOfWork;

    public ProjectSearchProvider(IUnitOfWork unitOfWork) => this.unitOfWork = unitOfWork;

    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString)
    {

        var results =
          await
            unitOfWork.GetDbSet<Project>()
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
