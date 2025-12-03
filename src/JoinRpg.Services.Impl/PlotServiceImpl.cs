using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.Plots;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl;

public class PlotServiceImpl(IUnitOfWork unitOfWork,
    IMassProjectEmailService massProjectEmailService,
    ICurrentUserAccessor currentUserAccessor) : DbServiceImplBase(unitOfWork, currentUserAccessor), IPlotService
{
    public async Task<PlotFolderIdentification> CreatePlotFolder(ProjectIdentification projectId, string masterTitle, string todo)
    {
        if (masterTitle == null)
        {
            throw new ArgumentNullException(nameof(masterTitle));
        }

        var project = await UnitOfWork.GetDbSet<Project>().FindAsync(projectId.Value);
        _ = project.RequestMasterAccess(CurrentUserId, acl => acl.CanManagePlots).EnsureProjectActive();
        var startTimeUtc = DateTime.UtcNow;
        var plotFolder = new PlotFolder
        {
            CreatedDateTime = startTimeUtc,
            ModifiedDateTime = startTimeUtc,
            ProjectId = projectId,
            MasterTitle = Required(masterTitle.RemoveTagNames()),
            TodoField = todo,
            IsActive = true,
        };

        await AssignTagList(plotFolder.PlotTags, masterTitle);

        project.PlotFolders.Add(plotFolder);
        await UnitOfWork.SaveChangesAsync();

        return new PlotFolderIdentification(projectId, plotFolder.PlotFolderId);
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
              new ProjectItemTag() { TagName = tagName });
        }

        presentTags.AssignLinksList(tagObjects);
    }

    public async Task EditPlotFolder(int projectId, int plotFolderId, string plotFolderMasterTitle, string todoField)
    {
        var folder = await LoadProjectSubEntityAsync<PlotFolder>(projectId, plotFolderId);

        _ = folder.RequestMasterAccess(CurrentUserId, acl => acl.CanManagePlots).EnsureProjectActive();

        folder.TodoField = todoField;
        folder.IsActive = true; //Restore if deleted
        folder.ModifiedDateTime = DateTime.UtcNow;

        await AssignTagList(folder.PlotTags, plotFolderMasterTitle);

        folder.MasterTitle = Required(plotFolderMasterTitle.RemoveTagNames());
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task<PlotVersionIdentification> CreatePlotElement(PlotFolderIdentification plotFolderId, string content, string todoField,
      IReadOnlyCollection<CharacterGroupIdentification> targetGroups, IReadOnlyCollection<CharacterIdentification> targetChars, PlotElementType elementType)
    {
        var folder = await LoadProjectSubEntityAsync<PlotFolder>(plotFolderId);

        _ = folder.RequestMasterAccess(CurrentUserId).EnsureProjectActive();

        var now = DateTime.UtcNow;
        var characterGroups = await ProjectRepository.LoadGroups(targetGroups);

        if (characterGroups.Count != targetGroups.Distinct().Count())
        {
            var missing = string.Join(", ", targetGroups.Select(g => g.CharacterGroupId).Except(characterGroups.Select(cg => cg.CharacterGroupId)));
            throw new Exception($"Groups {missing} doesn't belong to project");
        }
        var plotElement = new PlotElement()
        {
            CreatedDateTime = now,
            ModifiedDateTime = now,
            IsActive = true,
            IsCompleted = false,
            ProjectId = plotFolderId.ProjectId,
            PlotFolderId = plotFolderId,
            TargetGroups = characterGroups,
            TargetCharacters = await ValidateCharactersList(targetChars),
            ElementType = elementType,
        };

        plotElement.Texts.Add(new PlotElementTexts()
        {
            Content = new MarkdownString(Required(content.Trim())),
            TodoField = todoField,
            Version = 0,
            ModifiedDateTime = now,
            AuthorUserId = CurrentUserId,
        });

        folder.ModifiedDateTime = now;

        _ = UnitOfWork.GetDbSet<PlotElement>().Add(plotElement);
        await UnitOfWork.SaveChangesAsync();
        return new PlotVersionIdentification(new PlotElementIdentification(plotFolderId, plotElement.PlotElementId), Version: 0);
    }

    public async Task DeleteFolder(int projectId, int plotFolderId)
    {
        var folder = await LoadProjectSubEntityAsync<PlotFolder>(projectId, plotFolderId);
        _ = folder.RequestMasterAccess(CurrentUserId, Permission.CanManagePlots).EnsureProjectActive();

        _ = SmartDelete(folder);
        foreach (var element in folder.Elements)
        {
            element.IsActive = false;
            element.ModifiedDateTime = Now;
        }
        folder.ModifiedDateTime = Now;
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteElement(PlotElementIdentification plotElementId)
    {
        var plotElement = await LoadElementForManage(plotElementId);

        _ = SmartDelete(plotElement);
        plotElement.ModifiedDateTime = DateTime.UtcNow;
        await UnitOfWork.SaveChangesAsync();
    }

    private async Task<PlotElement> LoadElement(PlotElementIdentification plotElementId)
    {
        var folder = await LoadProjectSubEntityAsync<PlotFolder>(plotElementId.PlotFolderId);
        _ = folder.RequestMasterAccess(CurrentUserId).EnsureProjectActive();
        return folder.Elements.Single(e => e.PlotElementId == plotElementId.PlotElementId);
    }

    private async Task<PlotElement> LoadElementForManage(PlotElementIdentification plotElementId)
    {
        var folder = await LoadProjectSubEntityAsync<PlotFolder>(plotElementId.PlotFolderId);
        _ = folder.RequestMasterAccess(CurrentUserId, Permission.CanManagePlots).EnsureProjectActive();
        return folder.Elements.Single(e => e.PlotElementId == plotElementId.PlotElementId);
    }

    public async Task EditPlotElement(PlotElementIdentification plotelementid, string contents,
      string todoField, IReadOnlyCollection<CharacterGroupIdentification> targetGroups, IReadOnlyCollection<CharacterIdentification> targetChars)
    {
        var plotElement = await LoadElement(plotelementid);

        UpdateElementText(contents, todoField, plotElement);

        await UpdateElementTarget(targetGroups, targetChars, plotElement);

        UpdateElementMetadata(plotElement);
        await UnitOfWork.SaveChangesAsync();
    }

    private void UpdateElementMetadata(PlotElement plotElement)
    {
        plotElement.IsActive = true;
        plotElement.ModifiedDateTime = Now;
        plotElement.PlotFolder.ModifiedDateTime = Now;
    }

    private async Task UpdateElementTarget(IReadOnlyCollection<CharacterGroupIdentification> targetGroups, IReadOnlyCollection<CharacterIdentification> targetChars, PlotElement plotElement)
    {
        var characterGroups = await ProjectRepository.LoadGroups(targetGroups);

        if (characterGroups.Count != targetGroups.Distinct().Count())
        {
            var missing = string.Join(", ", targetGroups.Select(x => x.CharacterGroupId).Except(characterGroups.Select(cg => cg.CharacterGroupId)));
            throw new Exception($"Groups {missing} doesn't belong to project");
        }
        plotElement.TargetGroups.AssignLinksList(characterGroups);
        plotElement.TargetCharacters.AssignLinksList(await ValidateCharactersList(targetChars));
    }

    private void UpdateElementText(string contents, string todoField, PlotElement plotElement)
    {
        if (plotElement.LastVersion().Content.Contents == contents &&
            plotElement.LastVersion().TodoField == todoField)
        {
            return;
        }

        var text = new PlotElementTexts()
        {
            Content = new MarkdownString(contents),
            TodoField = todoField,
            Version = plotElement.Texts.Select(t => t.Version).Max() + 1,
            PlotElementId = plotElement.PlotElementId,
            ModifiedDateTime = Now,
            AuthorUserId = CurrentUserId,
        };
        plotElement.Texts.Add(text);
        plotElement.IsCompleted = false;
    }

    public async Task EditPlotElementText(PlotElementIdentification plotelementid, string contents, string todoField)
    {
        var plotElement = await LoadElement(plotelementid);

        UpdateElementText(contents, todoField, plotElement);

        UpdateElementMetadata(plotElement);
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task MoveElement(int projectId, int plotElementId, int parentCharacterId, int direction)
    {
        var character = await LoadProjectSubEntityAsync<Character>(projectId, parentCharacterId);
        _ = character.RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles).EnsureProjectActive();

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


    private List<Claim> GetClaimsFromGroups(IEnumerable<CharacterGroup> groups)
    {
        var claims = new List<Claim>();

        void InternalGetUsersFromGroups(IEnumerable<CharacterGroup> src)
        {
            foreach (var g in src)
            {
                claims.AddRange(g.Characters
                    .Select(c => c.ApprovedClaim)
                    .WhereNotNull()
                    );
                InternalGetUsersFromGroups(g.ChildGroups);
            }
        }

        InternalGetUsersFromGroups(groups);
        return claims;
    }

    public async Task PublishElementVersion(PlotVersionIdentification version, bool sendNotification, string? commentText)
    {
        // Publishing
        var plotElement = await LoadElementForManage(version.PlotElementId);

        plotElement.IsCompleted = true;
        plotElement.Published = version.Version;
        UpdateElementMetadata(plotElement);
        await UnitOfWork.SaveChangesAsync();

        if (plotElement.IsCompleted && sendNotification)
        {
            // Preparing list of users to send notification to
            List<Claim> claims = GetClaimsFromGroups(plotElement.TargetGroups);
            claims.AddRange(plotElement.TargetCharacters
                .Select(c => c.ApprovedClaim)
                            .WhereNotNull());


            await massProjectEmailService.PlotEmail([.. claims.Select(c => c.GetId())], new MarkdownString(commentText), plotElement.GetId());
        }
    }

    public async Task UnPublishElement(PlotElementIdentification plotElementId)
    {
        var plotElement = await LoadElementForManage(plotElementId);

        plotElement.IsCompleted = false;
        plotElement.Published = null;
        UpdateElementMetadata(plotElement);
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task ReorderPlots(PlotFolderIdentification plotFolderId, PlotFolderIdentification? afterPlotFolderId)
    {
        var targetFolder = await LoadProjectSubEntityAsync<PlotFolder>(plotFolderId);
        _ = targetFolder.RequestMasterAccess(CurrentUserId, Permission.CanManagePlots).EnsureProjectActive();

        var afterFolder = afterPlotFolderId is not null ? await LoadProjectSubEntityAsync<PlotFolder>(afterPlotFolderId) : null;

        targetFolder.Project.Details.PlotFoldersOrdering
            = targetFolder.Project.GetPlotFoldersContainer().MoveAfter(targetFolder, afterFolder).GetStoredOrder();

        await UnitOfWork.SaveChangesAsync();
    }

    public async Task ReorderPlotElements(PlotElementIdentification plotElementId, PlotElementIdentification? afterPlotElementId)
    {
        var targetFolder = await LoadProjectSubEntityAsync<PlotFolder>(plotElementId.PlotFolderId);

        _ = targetFolder.RequestMasterAccess(CurrentUserId, Permission.CanManagePlots).EnsureProjectActive();

        targetFolder.ElementsOrdering = DomainMoveHelper.MoveAfter(plotElementId,
            afterPlotElementId,
            targetFolder.ElementsOrdering,
            targetFolder.Elements.Select(x => new PlotElementIdentification(x.ProjectId, x.PlotFolderId, x.PlotElementId)),
            preserveOrder: false);

        await UnitOfWork.SaveChangesAsync();
    }

    public async Task ReorderPlotByChar(CharacterIdentification characterId, PlotElementIdentification targetId, PlotElementIdentification? afterId)
    {

        var character = await CharactersRepository.GetCharacterAsync(characterId);

        _ = character.RequestMasterAccess(CurrentUserId, Permission.CanManagePlots).EnsureProjectActive();

        var plotTarget = ToTarget(character);

        var plots = await PlotRepository.GetPlotsBySpecification(new PlotSpecification(plotTarget, PlotVersionFilter.PublishedVersion, PlotElementType.RegularPlot));

        var resultOrder = DomainMoveHelper.MoveAfter(targetId, afterId, character.PlotElementOrderData, plots.Select(x => x.Id.PlotElementId), preserveOrder: true);
        character.PlotElementOrderData = resultOrder;

        await UnitOfWork.SaveChangesAsync();
    }


    private static TargetsInfo ToTarget(Character character)
    {
        return new TargetsInfo(
        [new(new(character.ProjectId, character.CharacterId), character.CharacterName)],
            [.. character.GetParentGroupsToTop().Select(x => new GroupTarget(x.GetId(), x.CharacterGroupName))]);
    }
}
