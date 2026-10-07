using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Interfaces;
using JoinRpg.WebPortal.Models.Masters;

namespace JoinRpg.Web.Models.Masters;

public class MastersListViewModel
{
    public IReadOnlyCollection<AclViewModel> Masters { get; }

    public bool CanCurrentUserGrantRights { get; }

    public bool AnyoneElseCanGrantRights { get; }

    public int CurrentUserId { get; }

    public MastersListViewModel(
        IReadOnlyCollection<ClaimCountByMaster> claims,
        ICurrentUserAccessor currentUser,
        ProjectInfo projectInfo,
        IReadOnlyCollection<ProjectMasterProfileDto> profiles)
    {
        Masters = [.. projectInfo.Masters.Select(master => new AclViewModel(
            master,
            claims.SingleOrDefault(c => c.MasterId == master.UserId.Value)?.ClaimCount ?? 0,
            projectInfo)
        {
            Role = profiles.SingleOrDefault(p => p.UserId == master.UserId)?.Role.Value ?? "",
            IsPublic = master.IsPublic,
        })];

        // Админ, который не мастер проекта, тоже видит эту страницу — его в списке нет.
        CanCurrentUserGrantRights = Masters.SingleOrDefault(acl => acl.UserId == currentUser.UserId)?.CanGrantRights ?? false;

        AnyoneElseCanGrantRights = Masters.Any(x => x.CanGrantRights && x.UserId != currentUser.UserId);

        CurrentUserId = currentUser.UserId;
    }
}
