using JoinRpg.Common.PrimitiveTypes.Users;

namespace JoinRpg.DomainTypes.ProjectMetadata;

public record class ProjectMasterInfo(UserIdentification UserId, UserDisplayName Name, Email Email, Permission[] Permissions, bool IsOwner)
{
    public UserInfoHeader UserInfo { get; } = new UserInfoHeader(UserId, Name);
}
