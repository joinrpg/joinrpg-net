using JoinRpg.Markdown;

namespace JoinRpg.Web.Models.Characters;

public class CharacterGroupWithDescViewModel : CharacterGroupLinkViewModel
{
    public JoinHtmlString Description { get; }

    public CharacterGroupWithDescViewModel(CharacterGroupFullInfo group) : base(group) => Description = group.Description.ToHtmlString();
}
