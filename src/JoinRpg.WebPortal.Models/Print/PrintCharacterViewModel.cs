using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain.Access;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Models.Plot;
using JoinRpg.Web.ProjectMasterTools.Print;

namespace JoinRpg.Web.Models.Print;

public class PrintCharacterViewModel
{
    public string ProjectName { get; }
    public string CharacterName { get; }
    public int FeeDue => Envelope.FeeDue;
    public PlotDisplayViewModel Plots { get; }
    public IReadOnlyCollection<HandoutListItemViewModel> Handouts { get; }
    public CustomFieldsViewModel Fields { get; }
    public bool RegistrationOnHold => FeeDue > 0 || HasUnready;

    public bool HasUnready { get; }

    public EnvelopeViewModel Envelope { get; }

    public PrintCharacterViewModel
      (ICurrentUserAccessor currentUser, Character character, IReadOnlyCollection<PlotTextDto> plots, ProjectInfo projectInfo, IReadOnlyCollection<PlotTextDto> handouts)
    {
        ArgumentNullException.ThrowIfNull(character);

        CharacterName = character.CharacterName;
        ProjectName = character.Project.ProjectName;

        Envelope = character.ToEnvelopeViewModel(projectInfo);

        var plotElements = plots;
        HasUnready = !plotElements.All(x => x.Completed) || !handouts.All(x => x.Completed);
        Plots = new PlotDisplayViewModel(plotElements, currentUser, character, projectInfo);

        Handouts = [.. handouts.Select(e => new HandoutListItemViewModel(e))];

        Fields = new CustomFieldsViewModel(
            character,
            projectInfo,
            AccessArgumentsFactory.CreateForPrint(character, currentUser.UserIdOrDefault) with { EditAllowed = false },
            wherePrintEnabled: true);
    }
}
