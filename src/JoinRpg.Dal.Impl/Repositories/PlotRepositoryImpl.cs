using JoinRpg.Data.Interfaces.Plots;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Helpers;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

internal class PlotRepositoryImpl(MyDbContext ctx) : GameRepositoryImplBase(ctx), IPlotRepository
{

    public async Task<PlotElementDetailsDto?> GetPlotElementDetails(PlotElementIdentification elementId, int? version = null)
    {
        var row = await Ctx.Set<PlotElement>()
          .Where(e => e.PlotElementId == elementId.PlotElementId && e.ProjectId == elementId.ProjectId.Value)
          .ToRows(version)
          .SingleOrDefaultAsync();

        if (row is null)
        {
            return null;
        }

        if (!row.CurrentVersionExists)
        {
            throw new ArgumentOutOfRangeException(
                nameof(version),
                version,
                $"У вводной {elementId} нет версии {row.CurrentVersionNumber}");
        }

        return row.ToDto();
    }

    public async Task<PlotFolderDetailsDto?> GetPlotFolderDetails(PlotFolderIdentification plotFolderId)
    {
        var folder = await FolderRows(
            pf => pf.PlotFolderId == plotFolderId.PlotFolderId && pf.ProjectId == plotFolderId.ProjectId.Value)
          .SingleOrDefaultAsync();

        if (folder is null)
        {
            return null;
        }

        return folder.ToDto();
    }

    /// <summary>
    /// Отбирает папки сюжета вместе со вводными и отображаемой версией текста каждой вводной.
    /// </summary>
    /// <remarks>
    /// Проекция, а не загрузка EF-сущностей: страницам нужна только последняя версия текста плюс
    /// даты соседних, а история правок у крупных папок — основной объём данных. Через сущности так
    /// не выходит: <c>Include</c> в EF6 фильтровать нельзя.
    ///
    /// Вводные приходят вложенной проекцией через LinqKit: вызвать там обычный extension-метод
    /// нельзя — EF6 его не транслирует, страницы падали с 500, — а подстановка выражения
    /// <c>Invoke</c> на <c>AsExpandable</c>-запросе работает.
    /// </remarks>
    private IQueryable<PlotFolderRow> FolderRows(Expression<Func<PlotFolder, bool>> filter)
        => Ctx.Set<PlotFolder>()
          .AsExpandable()
          .Where(filter)
          .Select(pf => new PlotFolderRow
          {
              ProjectId = pf.ProjectId,
              PlotFolderId = pf.PlotFolderId,
              MasterTitle = pf.MasterTitle,
              TodoField = pf.TodoField,
              MasterSummary = pf.MasterSummary.Contents,
              IsActive = pf.IsActive,
              ElementsOrdering = pf.ElementsOrdering,
              Tags = pf.PlotTags.Select(tag => tag.TagName),
              Elements = pf.Elements.AsQueryable().Select(e => PlotElementRowQuery.RowSelector(null).Invoke(e)),
          });

    public async Task<IReadOnlyCollection<PlotElement>> GetDirectPlotsForCharacter(CharacterIdentification character)
    {
        return
          await Ctx.Set<PlotElement>()
            .Include(e => e.Texts)
            .Include(e => e.TargetCharacters)
            .Include(e => e.TargetGroups)
            .Where(e => e.TargetCharacters.Any(ch => ch.CharacterId == character.CharacterId))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PlotFolderDetailsDto>> GetActivePlotFolders(ProjectIdentification projectId)
    {
        var folders = await FolderRows(pf => pf.IsActive && pf.ProjectId == projectId.Value).ToListAsync();

        return [.. folders.Select(row => row.ToDto())];
    }

    public async Task<IReadOnlyList<PlotFolder>> GetPlots(ProjectIdentification projectId)
    {
        var project = await Ctx.Set<Project>()
          .Include(p => p.PlotFolders.Select(pf => pf.Elements))
          .Include(p => p.PlotFolders.Select(pf => pf.PlotTags))
          .Include(p => p.Details)
          .SingleAsync(pf => pf.ProjectId == projectId.Value);

        return VirtualOrderContainerFacade.Create(project.PlotFolders, project.Details.PlotFoldersOrdering).OrderedItems;
    }

    public Task<List<PlotFolder>> GetPlotsForTargets(int projectId, List<int> characterIds, List<int> characterGroupIds)
    {
        return Ctx.Set<PlotFolder>()
          .Include(pf => pf.Elements.Select(e => e.TargetCharacters))
          .Include(pf => pf.Elements.Select(e => e.TargetGroups))
          .Include(pf => pf.PlotTags)
          .Where(pf => pf.ProjectId == projectId)
          .Where(
              pf =>
                pf.Elements.Any(
                  e => e.TargetCharacters.Select(c => c.CharacterId).Intersect(characterIds).Any()
                       || e.TargetGroups.Select(c => c.CharacterGroupId).Intersect(characterGroupIds).Any()))
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<PlotFolder>> GetPlotsByTag(int projectid, string tagname)
    {
        return await Ctx.Set<PlotFolder>()
          .Include(pf => pf.Elements)
          .Include(pf => pf.PlotTags)
          .Where(pf => pf.ProjectId == projectid && pf.PlotTags.Any(tag => tag.TagName == tagname))
          .ToListAsync();
    }

    public async Task<IReadOnlyCollection<PlotTextDto>> GetPlotsBySpecification(PlotSpecification plotSpecification)
    {
        Expression<Func<PlotElement, bool>> targetPredicate = GetTargetPredicate(plotSpecification.Targets);

        var dataset = Ctx.Set<PlotElementTexts>()
            .Include(e => e.PlotElement.TargetCharacters)
            .Include(e => e.PlotElement.TargetGroups)
            .Include(e => e.PlotElement.PlotFolder);

        var query = from text in dataset.AsExpandable()
                    let element = text.PlotElement
                    where element.IsActive && element.ElementType == plotSpecification.PlotElementType && !element.IsMasterOnly
                    where
                      targetPredicate.Invoke(element)
                    let maxVersion = element.Texts.Max(t => t.Version)
                    select new
                    {
                        PlotElement = element,
                        Text = text,
                        Latest = text.Version == maxVersion,
                        Published = element.Published == text.Version,
                        HasPublished = element.Published != null,
                        Completed = element.Published == maxVersion,
                        element.ProjectId,
                        element.PlotFolderId,
                        element.PlotElementId,
                        text.Version,
                        element.IsActive,
                        element.TargetGroups,
                        element.TargetCharacters,
                    };

        var resultQuery = plotSpecification.VersionFilter switch
        {
            PlotVersionFilter.PublishedVersion => query.Where(x => x.Published),
            PlotVersionFilter.LatestVersion => query.Where(x => x.Latest),
            _ => throw new NotImplementedException(),
        };
        var result = await resultQuery.ToListAsync();


        if (result.Count == 0)
        {
            return [];
        }

        var typedResult = result
            .Select(r => new PlotTextDto()
            {
                Completed = r.Completed,
                HasPublished = r.HasPublished,
                Id = new PlotVersionIdentification(r.ProjectId, r.PlotFolderId, r.PlotElementId, r.Version),
                Latest = r.Latest,
                Published = r.Published,
                Content = r.Text.Content,
                TodoField = r.Text.TodoField,
                IsActive = r.IsActive,
                Target = new TargetsInfo(
                    [.. r.TargetCharacters.Select(x => new CharacterTarget(x.GetId(), x.CharacterName))],
                    [.. r.TargetGroups.Select(x => new GroupTarget(x.GetId(), x.CharacterGroupName))])
            }
            ).ToArray();

        // Загружаем порядок
        var projectId = typedResult.First().Id.ProjectId;
        var folderOrdering = await Ctx.Set<Project>().Where(p => p.ProjectId == projectId).Select(p => p.Details.PlotFoldersOrdering).SingleAsync();
        var elementOrderingDict = await Ctx.Set<PlotFolder>().Where(p => p.ProjectId == projectId)
            .ToDictionaryAsync(x => x.PlotFolderId, x => x.ElementsOrdering);

        return [..
            typedResult
            .GroupBy(e => e.Id.PlotFolderId.PlotFolderId)
            .OrderByStoredOrder(e => e.Key, folderOrdering)
            .SelectMany(f => f.OrderByStoredOrder(e => e.Id.PlotElementId.PlotElementId, elementOrderingDict[f.Key]))
            ];
    }

    private Expression<Func<PlotElement, bool>> GetTargetPredicate(TargetsInfo targets)
    {
        var charIds = targets.CharacterTargets.Select(x => x.CharacterId.CharacterId).ToList();
        var groupIds = targets.GroupTargets.Select(x => x.CharacterGroupId.CharacterGroupId).ToList();
        return element => element.TargetCharacters.Any(ch => charIds.Contains(ch.CharacterId)) ||
                      element.TargetGroups.Any(g => groupIds.Contains(g.CharacterGroupId));
    }
}
