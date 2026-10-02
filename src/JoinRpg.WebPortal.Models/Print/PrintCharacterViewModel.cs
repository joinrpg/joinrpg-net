using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
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

    /// <param name="envelope">
    /// Заглавная страница конверта. Приходит готовой, а не собирается здесь: ей нужны название
    /// проживания и телефон игрока, а их в агрегате персонажа нет — собирает вызывающий,
    /// как и для отдельных страниц наклеек и конвертов.
    /// </param>
    public PrintCharacterViewModel
      (ICurrentUserAccessor currentUser,
       CharacterInfo character,
       EnvelopeViewModel envelope,
       IReadOnlyCollection<PlotTextDto> plots,
       IReadOnlyCollection<PlotTextDto> handouts,
       ILinkRenderer linkRenderer,
       IReadOnlyDictionary<UserIdentification, UserInfoHeader> fieldUsers)
    {
        ArgumentNullException.ThrowIfNull(character);

        CharacterName = character.CharacterName;
        ProjectName = character.ProjectInfo.ProjectName;

        Envelope = envelope;

        HasUnready = !plots.All(x => x.Completed) || !handouts.All(x => x.Completed);
        Plots = new PlotDisplayViewModel(plots, currentUser, character, linkRenderer);

        Handouts = [.. handouts.Select(e => new HandoutListItemViewModel(e))];

        Fields = CustomFieldsViewModel.ForPrint(
            character,
            AccessArgumentsFactory.Create(character, currentUser, CharacterAccessMode.Print) with { EditAllowed = false },
            fieldUsers);
    }
}
