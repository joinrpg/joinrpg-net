using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
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
      folder.IsActive = true; //Restore if deleted
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
        TargetGroups = await ValidateCharacterGroupList(projectId, targetGroups),
        TargetCharacters = await ValidateCharactersList(projectId, targetChars)
      };

      UnitOfWork.GetDbSet<PlotElement>().Add(plotElement);
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteFolder(int projectId, int plotFolderId, int currentUserId)
    {
      var folder = await LoadProjectSubEntityAsync<PlotFolder>(projectId, plotFolderId);
      if (!folder.HasMasterAccess(currentUserId))
      {
        throw new DbEntityValidationException();
      }
      SmartDelete(folder);
      foreach (var element in folder.Elements)
      {
        element.IsActive = false;
      }
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteElement(int projectId, int plotFolderId, int plotelementid, int currentUserId)
    {
      var plotElement = await LoadElement(projectId, plotFolderId, plotelementid, currentUserId);
      SmartDelete(plotElement);
      await UnitOfWork.SaveChangesAsync();
    }

    private async Task<PlotElement> LoadElement(int projectId, int plotFolderId, int plotelementid, int currentUserId)
    {
      var folder = await LoadProjectSubEntityAsync<PlotFolder>(projectId, plotFolderId);
      if (!folder.HasMasterAccess(currentUserId))
      {
        throw new DbEntityValidationException();
      }
      return folder.Elements.Single(e => e.PlotElementId == plotelementid);
    }

    public async Task EditPlotElement(int projectId, int plotFolderId, int plotelementid, string contents, string todoField, ICollection<int> targetGroups, ICollection<int> targetChars, bool isCompleted, int currentUserId)
    {
      var plotElement = await LoadElement(projectId, plotFolderId, plotelementid, currentUserId);
      plotElement.Content.Contents = contents;
      plotElement.TodoField = todoField;
      plotElement.TargetGroups.AssignLinksList(await ValidateCharacterGroupList(projectId, targetGroups));
      plotElement.TargetCharacters.AssignLinksList(await ValidateCharactersList(projectId, targetChars));
      plotElement.IsCompleted = isCompleted;
      await UnitOfWork.SaveChangesAsync();
    }

    public PlotServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }
  }
}
