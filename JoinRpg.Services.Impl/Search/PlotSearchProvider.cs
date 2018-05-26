using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search
{
  internal class PlotSearchProvider : ISearchProvider
  {
    public IUnitOfWork UnitOfWork { protected get; set; }

    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
      var results =
       await
         UnitOfWork.GetDbSet<PlotFolder>()
           .Where(p  =>
             p.IsActive && p.Project.ProjectAcls.Any(acl => acl.UserId == currentUserId) && p.MasterTitle.Contains(searchString)
           )
           .ToListAsync();

      return results.Select(plot => new SearchResultImpl
      {
        LinkType = LinkType.Plot,
        Name = plot.MasterTitle,
        Description = new MarkdownString(""),
        Identification = plot.PlotFolderId.ToString(),
        ProjectId = plot.ProjectId,
        IsPublic = false,
        IsActive = plot.IsActive,
      }).ToList();
    }
  }
}
