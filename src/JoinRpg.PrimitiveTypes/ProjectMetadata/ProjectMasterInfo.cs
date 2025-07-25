using JoinRpg.PrimitiveTypes.Access;

namespace JoinRpg.PrimitiveTypes.ProjectMetadata;
public record class ProjectMasterInfo(UserIdentification UserId, UserDisplayName Name, Email Email, Permission[] Permissions)
{
}
