using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Interfaces;
using JoinRpg.Web.Models.Print;

namespace JoinRpg.WebPortal.Managers.Plots;

//TODO: extract interface
public class CharacterPlotViewService(
    ICharacterInfoRepository characterInfoRepository,
    IPlotRepository plotRepository,
    ICurrentUserAccessor currentUser
    )
{
    /// <summary>
    /// Отчёт по раздаткам всех активных персонажей проекта.
    /// </summary>
    /// <remarks>
    /// Отдаём сразу агрегированный отчёт, а не словарь «персонаж → раздатки»: словарь не несёт
    /// порядка сюжетов, и строки отчёта получались в порядке обхода персонажей, то есть случайном.
    /// </remarks>
    public async Task<HandoutReportViewModel> GetHandoutReport(ProjectIdentification projectId, PlotVersionFilter version)
    {
        var plotInfo = await LoadPlotInfoForActiveCharacters(projectId, CharacterAccessMode.Print);

        var charactersTargets = plotInfo.Values.Select(x => x.Targets).ToArray();

        var specification = new PlotSpecification(charactersTargets.UnionAll(), version, PlotElementType.Handout);

        var plots = await plotRepository.GetPlotsBySpecification(specification);

        return HandoutReportViewModelBuilder.Build(plots, charactersTargets);
    }

    public async Task<IReadOnlyDictionary<CharacterIdentification, IReadOnlyList<PlotTextDto>>> GetHandoutsForCharacters(
        IReadOnlyCollection<CharacterIdentification> characterIdList)
    {
        characterIdList.EnsureSameProject();

        if (characterIdList.Count == 0)
        {
            return new Dictionary<CharacterIdentification, IReadOnlyList<PlotTextDto>>();
        }

        var plotInfo = await LoadPlotInfoForCharacters(characterIdList, CharacterAccessMode.Print);

        var specification = new PlotSpecification(plotInfo.Values.Select(x => x.Targets).UnionAll(), PlotVersionFilter.PublishedVersion, PlotElementType.Handout);

        var plots = await plotRepository.GetPlotsBySpecification(specification);

        return MapPlotToTargets(plotInfo, plots);
    }

    public async Task<IReadOnlyDictionary<CharacterIdentification, IReadOnlyList<PlotTextDto>>> GetPlotForCharacters(
        IReadOnlyCollection<CharacterIdentification> characterIdList,
        CharacterAccessMode characterAccessMode)
    {
        characterIdList.EnsureSameProject();

        if (characterIdList.Count == 0)
        {
            return new Dictionary<CharacterIdentification, IReadOnlyList<PlotTextDto>>();
        }
        var plotInfo = await LoadPlotInfoForCharacters(characterIdList, characterAccessMode);

        var specification = new PlotSpecification(plotInfo.Values.Select(x => x.Targets).UnionAll(), PlotVersionFilter.PublishedVersion, PlotElementType.RegularPlot);

        var plots = await plotRepository.GetPlotsBySpecification(specification);

        return MapPlotToTargets(plotInfo, plots);
    }

    private static Dictionary<CharacterIdentification, IReadOnlyList<PlotTextDto>> MapPlotToTargets(Dictionary<CharacterIdentification, ChPlotInfo> targets, IReadOnlyCollection<PlotTextDto> plots)
    {
        var dict = new Dictionary<CharacterIdentification, IReadOnlyList<PlotTextDto>>(targets.Count);

        foreach (var (characterId, info) in targets)
        {
            List<PlotTextDto> characterPlots = [];

            foreach (var plot in plots)
            {
                TargetsInfo plotTarget = plot.Target;
                if (plotTarget.HasIntersections(info.Targets))
                {
                    characterPlots.Add(plot);
                }
            }

            dict[characterId] = [.. characterPlots.OrderByStoredOrder(info.Ordering, preserveOrder: true)];
        }

        return dict;
    }

    private async Task<Dictionary<CharacterIdentification, ChPlotInfo>> LoadPlotInfoForCharacters(IReadOnlyCollection<CharacterIdentification> characterIdList, CharacterAccessMode characterAccessMode)
    {
        var characters = await characterInfoRepository.GetCharacterInfos(characterIdList);

        return ToPlotInfo(characters, characterAccessMode);
    }

    private async Task<Dictionary<CharacterIdentification, ChPlotInfo>> LoadPlotInfoForActiveCharacters(ProjectIdentification projectId, CharacterAccessMode characterAccessMode)
    {
        var characters = await characterInfoRepository.GetAllCharacterInfos(projectId, CharacterStatusSpec.Active);

        return ToPlotInfo(characters, characterAccessMode);
    }

    private Dictionary<CharacterIdentification, ChPlotInfo> ToPlotInfo(
        IReadOnlyCollection<CharacterInfo> characters,
        CharacterAccessMode characterAccessMode)
        => characters
            .Where(c => AccessArgumentsFactory.Create(c, currentUser, characterAccessMode).CharacterPlotAccess)
            .ToDictionary(x => x.Id, x => new ChPlotInfo(new TargetsInfo(x), x.PlotElementOrderData));

    private record ChPlotInfo(TargetsInfo Targets, string? Ordering);

    public async Task<IReadOnlyList<PlotTextDto>> GetPlotsForCharacter(CharacterIdentification characterId)
    {
        var dict = await GetPlotForCharacters([characterId], CharacterAccessMode.Usual);
        return dict.TryGetValue(characterId, out var plot) ? plot : [];
    }
}
