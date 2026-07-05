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
        ProjectInfo projectInfo)
    {
        Masters = [.. projectInfo.Masters.Select(master => new AclViewModel(
            master,
            claims.SingleOrDefault(c => c.MasterId == master.UserId.Value)?.ClaimCount ?? 0,
            projectInfo))];

        CanCurrentUserGrantRights = Masters.Single(acl => acl.UserId == currentUser.UserId).CanGrantRights;

        AnyoneElseCanGrantRights = Masters.Any(x => x.CanGrantRights && x.UserId != currentUser.UserId);

        CurrentUserId = currentUser.UserId;
    }
}
