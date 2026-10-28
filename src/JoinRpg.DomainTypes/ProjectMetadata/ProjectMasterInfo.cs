using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Helpers;

namespace JoinRpg.DomainTypes.ProjectMetadata;

/// <param name="IsPublic">Видят ли мастера немастера (ADR019, §3). На права и рассылки не влияет.</param>
/// <param name="Avatar">Выбранная аватарка мастера, null если её нет.</param>
/// <param name="ProjectAclId">Ключ для порядка мастеров (строка ProjectAclId в ProjectDetails.MastersOrdering).</param>
public record class ProjectMasterInfo(
    UserIdentification UserId,
    UserDisplayName Name,
    AvatarIdentification? Avatar,
    Email Email,
    Permission[] Permissions,
    bool IsOwner,
    ProjectAclStatus Status,
    bool IsPublic,
    int ProjectAclId) : IOrderableEntity
{
    public UserInfoHeader UserInfo { get; } = new UserInfoHeader(UserId, Name, Avatar);

    int IOrderableEntity.Id => ProjectAclId;
}
