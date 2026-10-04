using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Helpers;

namespace JoinRpg.DomainTypes.ProjectMetadata;

/// <param name="IsPublic">Видят ли мастера немастера (ADR019, §3). На права и рассылки не влияет.</param>
/// <param name="ProjectAclId">Ключ для порядка мастеров (строка ProjectAclId в ProjectDetails.MastersOrdering).</param>
public record class ProjectMasterInfo(
    UserIdentification UserId,
    UserDisplayName Name,
    Email Email,
    Permission[] Permissions,
    bool IsOwner,
    ProjectAclStatus Status,
    bool IsPublic,
    int ProjectAclId) : IOrderableEntity
{
    public UserInfoHeader UserInfo { get; } = new UserInfoHeader(UserId, Name);

    int IOrderableEntity.Id => ProjectAclId;
}
