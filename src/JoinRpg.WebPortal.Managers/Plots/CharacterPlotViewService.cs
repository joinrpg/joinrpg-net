using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.Plots;

namespace JoinRpg.WebPortal.Managers.Plots;

//TODO: Uncouple from DataModel, extract interface
public class CharacterPlotViewService(
    ICharacterRepository characterRepository,
    IPlotRepository plotRepository,
    ICurrentUserAccessor currentUser
    )
{
    public async Task<IReadOnlyDictionary<CharacterIdentification, IReadOnlyList<PlotTextDto>>> GetHandoutsForActiveCharacters(ProjectIdentification projectId, PlotVersionFilter version)
    {
        var plotInfo = await LoadPlotInfoForActiveCharacters(projectId, CharacterAccessMode.Print);

        var specification = new PlotSpecification(plotInfo.Values.Select(x => x.Targets).UnionAll(), version, PlotElementType.Handout);

        var plots = await plotRepository.GetPlotsBySpecification(specification);

        return MapPlotToTargets(plotInfo, plots);
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
        //TODO introduce method that loads only required data
        var characters = await characterRepository.GetCharacters(characterIdList);

        characters = [.. characters.Where(c => AccessArgumentsFactory.Create(c, currentUser, characterAccessMode).CharacterPlotAccess)];

        return characters.ToDictionary(x => x.GetId(), x => new ChPlotInfo(ToTarget(x), x.PlotElementOrderData));
    }

    private async Task<Dictionary<CharacterIdentification, ChPlotInfo>> LoadPlotInfoForActiveCharacters(ProjectIdentification projectId, CharacterAccessMode characterAccessMode)
    {
        //TODO introduce method that loads only required data
        var characters = await characterRepository.GetAllCharacters(projectId);

        characters = [.. characters.Where(c => AccessArgumentsFactory.Create(c, currentUser, characterAccessMode).CharacterPlotAccess)];

        return characters.ToDictionary(x => x.GetId(), x => new ChPlotInfo(ToTarget(x), x.PlotElementOrderData));
    }

    private record ChPlotInfo(TargetsInfo Targets, string Ordering);

    private TargetsInfo ToTarget(Character character)
    {
        return new TargetsInfo(
        [new(new(character.ProjectId, character.CharacterId), character.CharacterName)],
            [.. character.GetParentGroupsToTop().Select(x => new GroupTarget(x.GetId(), x.CharacterGroupName))]);
    }

    public async Task<IReadOnlyList<PlotTextDto>> GetPlotsForCharacter(CharacterIdentification characterId)
    {
        var dict = await GetPlotForCharacters([characterId], CharacterAccessMode.Usual);
        return dict.TryGetValue(characterId, out var plot) ? plot : [];
    }
}
