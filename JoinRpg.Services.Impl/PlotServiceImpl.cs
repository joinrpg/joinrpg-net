using System;
using System.Collections.Generic;
using System.Data.Entity;
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
      if (masterTitle == null) throw new ArgumentNullException(nameof(masterTitle));
      var project = await UnitOfWork.GetDbSet<Project>().FindAsync(projectId);
      project.RequestMasterAccess(CurrentUserId, acl => acl.CanManagePlots);
      var startTimeUtc = DateTime.UtcNow;
      var plotFolder = new PlotFolder
      {
        CreatedDateTime = startTimeUtc,
        ModifiedDateTime = startTimeUtc,
        ProjectId = projectId,
        MasterTitle = Required(masterTitle.RemoveTagNames()),
        TodoField = todo,
        IsActive = true
      };

      await AssignTagList(plotFolder.PlotTags, masterTitle);

      project.PlotFolders.Add(plotFolder);
      await UnitOfWork.SaveChangesAsync();
    }

    private async Task AssignTagList(ICollection<ProjectItemTag> presentTags, string title)
    {
      var currentTags = new List<ProjectItemTag>(presentTags);
      var tagObjects = new List<ProjectItemTag>();

      foreach (var tagName in title.ExtractTagNames())
      {
        tagObjects.Add(
          currentTags.SingleOrDefault(tag => tag.TagName == tagName) ??
          await UnitOfWork.GetDbSet<ProjectItemTag>().FirstOrDefaultAsync(pit => pit.TagName == tagName) ??
          new ProjectItemTag() {TagName = tagName});
      }

      presentTags.AssignLinksList(tagObjects);
    }

    public async Task EditPlotFolder(int projectId, int plotFolderId, string plotFolderMasterTitle, string todoField)
    {
      var folder = await LoadProjectSubEntityAsync<PlotFolder>(projectId, plotFolderId);

      folder.RequestMasterAccess(CurrentUserId, acl => acl.CanManagePlots);
      
      folder.TodoField = todoField;
      folder.IsActive = true; //Restore if deleted
      folder.ModifiedDateTime = DateTime.UtcNow;

      await AssignTagList(folder.PlotTags, plotFolderMasterTitle);

      folder.MasterTitle = Required(plotFolderMasterTitle.RemoveTagNames());
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task CreatePlotElement(int projectId, int plotFolderId, string content, string todoField,
      IReadOnlyCollection<int> targetGroups, IReadOnlyCollection<int> targetChars, PlotElementType elementType)
    {
      var folder = await LoadProjectSubEntityAsync<PlotFolder>(projectId, plotFolderId);

      folder.RequestMasterAccess(CurrentUserId);

      var now = DateTime.UtcNow;
      var characterGroups = await ProjectRepository.LoadGroups(projectId, targetGroups);

      if (characterGroups.Count != targetGroups.Distinct().Count())
      {
        var missing = string.Join(", ", targetGroups.Except(characterGroups.Select(cg => cg.CharacterGroupId)));
        throw new Exception($"Groups {missing} doesn't belong to project");
      }
      var plotElement = new PlotElement()
      {
        CreatedDateTime = now,
        ModifiedDateTime = now,
        IsActive = true,
        IsCompleted = false,
        ProjectId = projectId,
        PlotFolderId = plotFolderId,
        TargetGroups = characterGroups,
        TargetCharacters = await ValidateCharactersList(projectId, targetChars),
        ElementType = elementType
      };

      plotElement.Texts.Add(new PlotElementTexts()
      {
        Content = new MarkdownString(Required(content.Trim())),
        TodoField = todoField,
        Version = 0,
        ModifiedDateTime = now,
        AuthorUserId = CurrentUserId
      });

      folder.ModifiedDateTime = now;

      UnitOfWork.GetDbSet<PlotElement>().Add(plotElement);
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteFolder(int projectId, int plotFolderId)
    {
      var folder = await LoadProjectSubEntityAsync<PlotFolder>(projectId, plotFolderId);
      if (!folder.HasMasterAccess(CurrentUserId, acl => acl.CanManagePlots))
      {
        throw new DbEntityValidationException();
      }
      var now = DateTime.UtcNow;
      SmartDelete(folder);
      foreach (var element in folder.Elements)
      {
        element.IsActive = false;
        element.ModifiedDateTime = now;
      }
      folder.ModifiedDateTime = now;
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteElement(int projectId, int plotFolderId, int plotelementid)
    {
      var plotElement = await LoadElement(projectId, plotFolderId, plotelementid);
      plotElement.RequestMasterAccess(CurrentUserId, acl => acl.CanManagePlots);

      SmartDelete(plotElement);
      plotElement.ModifiedDateTime = DateTime.UtcNow;
      await UnitOfWork.SaveChangesAsync();
    }

    private async Task<PlotElement> LoadElement(int projectId, int plotFolderId, int plotelementid)
    {
      var folder = await LoadProjectSubEntityAsync<PlotFolder>(projectId, plotFolderId);
      folder.RequestMasterAccess(CurrentUserId);
      return folder.Elements.Single(e => e.PlotElementId == plotelementid);
    }

    public async Task EditPlotElement(int projectId, int plotFolderId, int plotelementid, string contents,
      string todoField, IReadOnlyCollection<int> targetGroups, IReadOnlyCollection<int> targetChars)
    {
      var now = DateTime.UtcNow;
      var plotElement = await LoadElement(projectId, plotFolderId, plotelementid);

      UpdateElementText(contents, todoField, plotElement, now);

      await UpdateElementTarget(projectId, targetGroups, targetChars, plotElement);

      UpdateElementMetadata(plotElement, now);
      await UnitOfWork.SaveChangesAsync();
    }

    private static void UpdateElementMetadata(PlotElement plotElement, DateTime now)
    {
      plotElement.IsActive = true;
      plotElement.ModifiedDateTime = now;
      plotElement.PlotFolder.ModifiedDateTime = now;
    }

    private async Task UpdateElementTarget(int projectId, IReadOnlyCollection<int> targetGroups, IReadOnlyCollection<int> targetChars,
      PlotElement plotElement)
    {
      var characterGroups = await ProjectRepository.LoadGroups(projectId, targetGroups);

      if (characterGroups.Count != targetGroups.Distinct().Count())
      {
        var missing = string.Join(", ", targetGroups.Except(characterGroups.Select(cg => cg.CharacterGroupId)));
        throw new Exception($"Groups {missing} doesn't belong to project");
      }
      plotElement.TargetGroups.AssignLinksList(characterGroups);
      plotElement.TargetCharacters.AssignLinksList(await ValidateCharactersList(projectId, targetChars));
    }

    private static void UpdateElementText(string contents, string todoField, PlotElement plotElement, DateTime now)
    {
      if (plotElement.LastVersion().Content.Contents == contents &&
          plotElement.LastVersion().TodoField == todoField) return;
      var text = new PlotElementTexts()
      {
        Content = new MarkdownString(contents),
        TodoField = todoField,
        Version = plotElement.Texts.Select(t => t.Version).Max() + 1,
        PlotElementId = plotElement.PlotElementId,
        ModifiedDateTime = now,
        AuthorUserId = CurrentUserId
      };
      plotElement.Texts.Add(text);
      plotElement.IsCompleted = false;
    }

    public async Task EditPlotElementText(int projectId, int plotFolderId, int plotelementid, string contents, string todoField)
    {
      var now = DateTime.UtcNow;
      var plotElement = await LoadElement(projectId, plotFolderId, plotelementid);

      UpdateElementText(contents, todoField, plotElement, now);

      UpdateElementMetadata(plotElement, now);
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task MoveElement(int projectId, int plotElementId, int parentCharacterId, int direction)
    {
      var character = await LoadProjectSubEntityAsync<Character>(projectId, parentCharacterId);
      character.RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles);

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

    public async Task PublishElementVersion(int projectId, int plotFolderId, int plotelementid, int? version)
    {
      var plotElement = await LoadElement(projectId, plotFolderId, plotelementid);
      plotElement.EnsureActive();
      plotElement.RequestMasterAccess(CurrentUserId, acl => acl.CanManagePlots);
      plotElement.IsCompleted = version != null;
      plotElement.Published = version;
      plotElement.ModifiedDateTime = plotElement.PlotFolder.ModifiedDateTime = DateTime.UtcNow;
      await UnitOfWork.SaveChangesAsync();
    }
    public PlotServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }
  }
}
