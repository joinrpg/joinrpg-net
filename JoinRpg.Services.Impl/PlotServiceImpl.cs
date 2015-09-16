using System;
using System.Collections.Generic;
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
        MasterTitle = Required(masterTitle),
        TodoField = todo,
        IsActive = true
      });
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task EditPlotFolder(int projectId, int plotFolderId, string plotFolderMasterTitle, string todoField)
    {
      var folder = await LoadProjectSubEntityAsync<PlotFolder>(projectId, plotFolderId);
      folder.MasterTitle = Required(plotFolderMasterTitle);
      folder.TodoField = todoField;
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task AddPlotElement(int projectId, int plotFolderId, string content, string todoField, ICollection<int> targetGroups, ICollection<int> targetChars)
    {
      var now = DateTime.UtcNow;
      var plotElement = new PlotElement()
      {
        Content = new MarkdownString(content),
        TodoField = todoField,
        CreatedDateTime = now,
        ModifiedDateTime = now,
        IsActive = true,
        IsCompleted = false,
        ProjectId = projectId,
        PlotFolderId = plotFolderId,
        TargetGroups = ValidateCharacterGroupList(projectId, targetGroups),
        TargetCharacters = ValidateCharactersList(projectId, targetChars)
      };

      UnitOfWork.GetDbSet<PlotElement>().Add(plotElement);
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteFolder(int projectId, int plotFolderId, int currentUserId)
    {
      var folder = await LoadProjectSubEntityAsync<PlotFolder>(projectId, plotFolderId);
      SmartDelete(folder);
      foreach (var element in folder.Elements)
      {
        element.IsActive = false;
      }
      await UnitOfWork.SaveChangesAsync();
    }

    public PlotServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }
  }
}
