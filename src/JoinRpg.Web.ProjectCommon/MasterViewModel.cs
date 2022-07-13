using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.ProjectCommon;

public record MasterViewModel(int MasterId, UserDisplayName DisplayName)
{
    public static MasterViewModel Empty(string label)
        => new(-1, new UserDisplayName(DisplayName: label, FullName: null));
}
