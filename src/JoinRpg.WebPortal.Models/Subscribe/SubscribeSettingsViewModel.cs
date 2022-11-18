using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.ProjectMasterTools.Subscribe;

namespace JoinRpg.Web.Models.Subscribe;

public class SubscribeSettingsViewModel
{
    public SubscribeSettingsViewModel()
    {
    }

    public SubscribeSettingsViewModel(User user, CharacterGroup group)
    {
        var direct =
            user.Subscriptions.SingleOrDefault(
                s => s.CharacterGroupId == group.CharacterGroupId);
        if (direct != null)
        {
            _ = Options.AssignFrom(direct);
        }
        ProjectId = group.ProjectId;
        CharacterGroupId = group.CharacterGroupId;
        Name = group.CharacterGroupName;
    }

    public string Name { get; }

    public SubscribeOptionsViewModel Options { get; set; } = new SubscribeOptionsViewModel();

    public int ProjectId { get; set; }
    public int CharacterGroupId { get; set; }

}
