using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
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

    public async Task AddPlotElement(int projectId, int plotFolderId, string content, string todoField,
      IReadOnlyCollection<int> targetGroups, IReadOnlyCollection<int> targetChars, PlotElementType elementType)
    {
      var now = DateTime.UtcNow;
      var characterGroups = await ProjectRepository.LoadGroups(projectId, targetGroups);

      if (characterGroups.Count != targetGroups.Distinct().Count())
      {
        var missing = string.Join(", ", targetGroups.Except(characterGroups.Select(cg => cg.CharacterGroupId)));
        throw new Exception($"Groups {missing} doesn't belong to project");
      }
      var plotElement = new PlotElement()
      {
        Texts = new PlotElementTexts()
        {
          Content = new MarkdownString(content),
          TodoField = todoField,
        },
        CreatedDateTime = now,
        ModifiedDateTime = now,
        IsActive = true,
        IsCompleted = false,
        ProjectId = projectId,
        PlotFolderId = plotFolderId,
        TargetGroups =   characterGroups,
        TargetCharacters = await ValidateCharactersList(projectId, targetChars),
        ElementType = elementType
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
      folder.RequestMasterAccess(currentUserId);
      return folder.Elements.Single(e => e.PlotElementId == plotelementid);
    }

    public async Task EditPlotElement(int projectId, int plotFolderId, int plotelementid, string contents,
      string todoField, IReadOnlyCollection<int> targetGroups, IReadOnlyCollection<int> targetChars, bool isCompleted,
      int currentUserId)
    {
      var plotElement = await LoadElement(projectId, plotFolderId, plotelementid, currentUserId);
      plotElement.Texts.Content.Contents = contents;
      plotElement.Texts.TodoField = todoField;
      var characterGroups = await ProjectRepository.LoadGroups(projectId, targetGroups);

      if (characterGroups.Count != targetGroups.Distinct().Count())
      {
        var missing = string.Join(", ", targetGroups.Except(characterGroups.Select(cg => cg.CharacterGroupId)));
        throw new Exception($"Groups {missing} doesn't belong to project");

      }
      plotElement.TargetGroups.AssignLinksList(characterGroups);
      plotElement.TargetCharacters.AssignLinksList(await ValidateCharactersList(projectId, targetChars));
      plotElement.IsCompleted = isCompleted;
      plotElement.IsActive = true;
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task MoveElement(int currentUserId, int projectId, int plotElementId, int parentCharacterId, int direction)
    {
      var character = await LoadProjectSubEntityAsync<Character>(projectId, parentCharacterId);
      character.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);

      var plots = await PlotRepository.GetPlotsForCharacter(character);

      var voc = character.GetCharacterPlotContainer(plots);
      var element = plots.Single(p => p.PlotElementId == plotElementId);
      switch (direction)
      {
        case -1:
          voc.MoveUp(element);
          break;
        case 1:
          voc.MoveDown(element);
          break;
        default:
          throw new ArgumentException(nameof(direction));
      }

      character.PlotElementOrderData = voc.GetStoredOrder();
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task PublishElement(int projectId, int plotFolderId, int plotelementid, int currentUserId)
    {
      var plotElement = await LoadElement(projectId, plotFolderId, plotelementid, currentUserId);
      if (plotElement.IsActive && !plotElement.IsCompleted)
      {
        plotElement.IsCompleted = true;
      }
      else
      {
        //TODO singal error
      }
      await UnitOfWork.SaveChangesAsync();
    }

    public PlotServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }
  }
}
