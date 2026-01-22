using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.PrimitiveTypes.ProjectMetadata;

public record class ProjectMasterInfo(UserIdentification UserId, UserDisplayName Name, Email Email, Permission[] Permissions)
{
    public UserInfoHeader UserInfo { get; } = new UserInfoHeader(UserId, Name);
}
