using JoinRpg.DataModel;
using JoinRpg.Helpers.Web;
using JoinRpg.Markdown;

namespace JoinRpg.Web.Models.Characters;

public class CharacterGroupWithDescViewModel : CharacterGroupLinkViewModel
{
    public JoinHtmlString Description { get; }


    public CharacterGroupWithDescViewModel(CharacterGroup group) : base(group) => Description = group.Description.ToHtmlString();
}
