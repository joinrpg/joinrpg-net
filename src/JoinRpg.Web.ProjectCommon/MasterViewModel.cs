using JoinRpg.Common.PrimitiveTypes;

namespace JoinRpg.Web.ProjectCommon;

public record MasterViewModel(UserIdentification MasterId, UserDisplayName DisplayName)
{
    public static MasterViewModel Empty(string label)
        => new(new UserIdentification(-1), new UserDisplayName(DisplayName: label, FullName: null));

    public UserLinkViewModel ToUserLinkViewModel() => new UserLinkViewModel(MasterId.Value, DisplayName.DisplayName, ViewMode.Show);
}
