using System.Data.Entity;
using System.Linq.Expressions;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl;

public class PlotServiceImpl(IUnitOfWork unitOfWork,
    IMassProjectEmailService massProjectEmailService,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository) : DbServiceImplBase(unitOfWork, currentUserAccessor), IPlotService
{
    /// <summary>
    /// Проверяет мастерский доступ к сюжетам проекта по снимку метаданных.
    /// </summary>
    /// <remarks>
    /// Права берутся из <see cref="ProjectInfo"/>, а не из навигации <c>folder.Project.ProjectAcls</c>:
    /// у сущности проекта, поднятой через <c>UnitOfWork</c>, ACL не загружены, и легаси-проверка
    /// стоила ленивой догрузки <c>ProjectAcls</c> на каждом маршруте сюжетов (#4989, #4670).
    /// Метаданные на запрос уже загружены — за <see cref="IProjectMetadataRepository"/> в Portal
    /// стоит кеш запроса.
    /// </remarks>
    private async Task RequestMasterAccessAsync(ProjectIdentification projectId, Permission permission = Permission.CanManagePlots)
        => _ = (await projectMetadataRepository.GetProjectMetadata(projectId))
            .RequestMasterAccess(currentUserAccessor, permission)
            .EnsureProjectActive();

    public async Task<PlotFolderIdentification> CreatePlotFolder(ProjectIdentification projectId, string masterTitle, string todo)
    {
        if (masterTitle == null)
        {
            throw new ArgumentNullException(nameof(masterTitle));
        }

        await RequestMasterAccessAsync(projectId);

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

        // Папка добавляется в свой DbSet, а не в project.PlotFolders: обращение к коллекции
        // проекта тянуло бы все его папки сюжетов отдельным запросом (#4989).
        _ = UnitOfWork.GetDbSet<PlotFolder>().Add(plotFolder);
        await UnitOfWork.SaveChangesAsync();

        return new PlotFolderIdentification(projectId, plotFolder.PlotFolderId);
    }

    /// <summary>
    /// Папка сюжета вместе со связями, которые правит операция.
    /// </summary>
    /// <remarks>
    /// Отдельный запрос, а не <c>LoadProjectSubEntityAsync</c>: тот поднимает сущность через
    /// <c>Find</c>, куда <c>Include</c> не поставить, и связи приезжали ленивой догрузкой (#4989).
    /// </remarks>
    private async Task<PlotFolder> LoadFolderAsync(
        PlotFolderIdentification plotFolderId,
        params Expression<Func<PlotFolder, object>>[] includes)
    {
        var query = UnitOfWork.GetDbSet<PlotFolder>().AsQueryable();
        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.SingleOrDefaultAsync(folder =>
                folder.ProjectId == plotFolderId.ProjectId.Value
                && folder.PlotFolderId == plotFolderId.PlotFolderId)
            ?? throw new JoinRpgEntityNotFoundException(plotFolderId.PlotFolderId, nameof(PlotFolder));
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
        await RequestMasterAccessAsync(new ProjectIdentification(projectId));

        var folder = await LoadFolderAsync(new PlotFolderIdentification(projectId, plotFolderId), f => f.PlotTags);

        folder.TodoField = todoField;
        folder.IsActive = true; //Restore if deleted
        folder.ModifiedDateTime = DateTime.UtcNow;

        await AssignTagList(folder.PlotTags, plotFolderMasterTitle);

        folder.MasterTitle = Required(plotFolderMasterTitle.RemoveTagNames());
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task<PlotVersionIdentification> CreatePlotElement(PlotFolderIdentification plotFolderId, string content, string todoField,
      IReadOnlyCollection<CharacterGroupIdentification> targetGroups, IReadOnlyCollection<CharacterIdentification> targetChars, PlotElementType elementType, bool isMasterOnly)
    {
        await RequestMasterAccessAsync(plotFolderId.ProjectId, Permission.None);

        var folder = await LoadProjectSubEntityAsync<PlotFolder>(plotFolderId);

        if (isMasterOnly)
        {
            targetGroups = [];
            targetChars = [];
        }

        var now = DateTime.UtcNow;
        var characterGroups = await LoadTargetGroups(plotFolderId.ProjectId, targetGroups);
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
            IsMasterOnly = isMasterOnly,
        };

        plotElement.Texts.Add(new PlotElementTexts()
        {
            Content = new MarkdownDbValue(Required(content.Trim())),
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
        await RequestMasterAccessAsync(new ProjectIdentification(projectId));

        var folder = await LoadFolderAsync(new PlotFolderIdentification(projectId, plotFolderId), f => f.Elements);

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
        await RequestMasterAccessAsync(plotElementId.ProjectId, Permission.None);
        return await LoadElementCore(plotElementId);
    }

    private async Task<PlotElement> LoadElementForManage(PlotElementIdentification plotElementId)
    {
        await RequestMasterAccessAsync(plotElementId.ProjectId);
        return await LoadElementCore(plotElementId);
    }

    /// <summary>
    /// Вводная со всем, что правят операции над ней: версии текста, таргеты и папка.
    /// </summary>
    /// <remarks>
    /// Раньше вводную достали из <c>folder.Elements</c> — то есть ленивой догрузкой всех вводных
    /// папки, а за ней по одной догрузке на версии, таргет-группы и таргет-персонажей. Запрос по
    /// обоим id проверяет принадлежность папке так же, как прежняя выборка из коллекции (#4989).
    /// </remarks>
    private async Task<PlotElement> LoadElementCore(PlotElementIdentification plotElementId)
        => await UnitOfWork.GetDbSet<PlotElement>()
            .Include(element => element.Texts)
            .Include(element => element.TargetGroups)
            .Include(element => element.TargetCharacters)
            .Include(element => element.PlotFolder)
            .SingleOrDefaultAsync(element =>
                element.ProjectId == plotElementId.ProjectId.Value
                && element.PlotFolderId == plotElementId.PlotFolderId.PlotFolderId
                && element.PlotElementId == plotElementId.PlotElementId)
            ?? throw new JoinRpgEntityNotFoundException(plotElementId.PlotElementId, nameof(PlotElement));

    public async Task EditPlotElement(PlotElementIdentification plotelementid, string contents,
      string todoField, IReadOnlyCollection<CharacterGroupIdentification> targetGroups, IReadOnlyCollection<CharacterIdentification> targetChars, bool isMasterOnly)
    {
        var plotElement = await LoadElement(plotelementid);

        if (isMasterOnly)
        {
            targetGroups = [];
            targetChars = [];
        }

        plotElement.IsMasterOnly = isMasterOnly;
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

    /// <summary>
    /// Загружает группы, на которые нацелен сюжет, и убеждается, что все они есть в проекте.
    /// </summary>
    private async Task<IList<CharacterGroup>> LoadTargetGroups(ProjectIdentification projectId, IReadOnlyCollection<CharacterGroupIdentification> targetGroups)
    {
        var characterGroups = await ProjectRepository.LoadGroups(targetGroups);

        var loadedIds = characterGroups.Select(cg => cg.CharacterGroupId).ToHashSet();
        var missing = targetGroups.Distinct().Where(g => !loadedIds.Contains(g.CharacterGroupId)).ToArray();
        if (missing.Length != 0)
        {
            throw new CharacterGroupsNotFoundException(projectId, missing);
        }
        return characterGroups;
    }

    private async Task UpdateElementTarget(IReadOnlyCollection<CharacterGroupIdentification> targetGroups, IReadOnlyCollection<CharacterIdentification> targetChars, PlotElement plotElement)
    {
        var characterGroups = await LoadTargetGroups(new ProjectIdentification(plotElement.ProjectId), targetGroups);
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
            Content = new MarkdownDbValue(contents),
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


            await massProjectEmailService.PlotEmail([.. claims.Select(c => c.GetId())], new MarkdownDbValue(commentText), plotElement.GetId());
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
        await RequestMasterAccessAsync(plotFolderId.ProjectId);

        var targetFolder = await LoadProjectSubEntityAsync<PlotFolder>(plotFolderId);

        var afterFolder = afterPlotFolderId is not null ? await LoadProjectSubEntityAsync<PlotFolder>(afterPlotFolderId) : null;

        targetFolder.Project.Details.PlotFoldersOrdering
            = targetFolder.Project.GetPlotFoldersContainer().MoveAfter(targetFolder, afterFolder).GetStoredOrder();

        await UnitOfWork.SaveChangesAsync();
    }

    public async Task ReorderPlotElements(PlotElementIdentification plotElementId, PlotElementIdentification? afterPlotElementId)
    {
        await RequestMasterAccessAsync(plotElementId.ProjectId);

        var targetFolder = await LoadProjectSubEntityAsync<PlotFolder>(plotElementId.PlotFolderId);

        targetFolder.ElementsOrdering = DomainMoveHelper.MoveAfter(plotElementId,
            afterPlotElementId,
            targetFolder.ElementsOrdering,
            targetFolder.Elements.Select(x => new PlotElementIdentification(x.ProjectId, x.PlotFolderId, x.PlotElementId)),
            preserveOrder: false);

        await UnitOfWork.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<PlotElementIdentification>> ReorderPlotByChar(CharacterIdentification characterId, PlotElementIdentification targetId, PlotElementIdentification? afterId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(characterId.ProjectId);

        var character = await CharactersRepository.GetCharacterAsync(characterId);

        _ = projectInfo.RequestMasterAccess(currentUserAccessor, Permission.CanManagePlots).EnsureProjectActive();

        var plotTarget = ToTarget(character, projectInfo);

        var plots = await PlotRepository.GetPlotsBySpecification(new PlotSpecification(plotTarget, PlotVersionFilter.PublishedVersion, PlotElementType.RegularPlot));

        var elementIds = plots.Select(x => x.Id.PlotElementId).ToList();
        character.PlotElementOrderData = DomainMoveHelper.MoveAfter(targetId, afterId, character.PlotElementOrderData, elementIds, preserveOrder: true);

        await UnitOfWork.SaveChangesAsync();

        return [.. elementIds.OrderByStoredOrder(id => id.Id, character.PlotElementOrderData, preserveOrder: true)];
    }


    private static TargetsInfo ToTarget(Character character, ProjectInfo projectInfo)
    {
        return new TargetsInfo(
        [new(new(character.ProjectId, character.CharacterId), character.CharacterName)],
            [.. character.GetParentGroupsToTop(projectInfo).Select(x => new GroupTarget(x.Id, x.Name))]);
    }
}
