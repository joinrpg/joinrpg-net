using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  [UsedImplicitly]
  public class PlotServiceImpl : DbServiceImplBase, IPlotService
  {

    public async Task CreatePlotFolder(int projectId, string masterTitle, string todo)
    {
      var project = await UnitOfWork.GetDbSet<Project>().FindAsync(projectId);
      var startTimeUtc = DateTime.UtcNow;
      project.PlotFolders.Add(new PlotFolder
      {
        CreatedDateTime = startTimeUtc,
        ModifiedDateTime = startTimeUtc,
        ProjectId = projectId,
        MasterTitle = masterTitle,
        TodoField = todo
      });
      await UnitOfWork.SaveChangesAsync();
    }

    public PlotServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }
  }
}
