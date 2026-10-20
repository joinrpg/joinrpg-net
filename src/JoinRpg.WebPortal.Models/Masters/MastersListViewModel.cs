using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Web.ProjectMasterTools.Acl;
using JoinRpg.WebPortal.Models.Masters;

namespace JoinRpg.Web.Models.Masters;

public class MastersListViewModel
{
    public IReadOnlyCollection<AclViewModel> Masters { get; }

    /// <summary>Те же мастера, в том же порядке, — для JoinMoveControl в каждой строке.</summary>
    public IReadOnlyCollection<MasterMoveItemViewModel> MoveItems { get; }

    public bool CanCurrentUserGrantRights { get; }

    public bool AnyoneElseCanGrantRights { get; }

    public int CurrentUserId { get; }

    public int ProjectId { get; }

    public MastersListViewModel(
        IReadOnlyCollection<ClaimCountByMaster> claims,
        ICurrentUserAccessor currentUser,
        ProjectInfo projectInfo,
        IReadOnlyCollection<ProjectMasterProfileDto> profiles,
        IReadOnlyDictionary<UserIdentification, UserInfo> masterUsers,
        UserProfileDetailsBuilder cardBuilder)
    {
        Masters = [.. projectInfo.Masters.Select(master => new AclViewModel(
            master,
            masterUsers[master.UserId],
            claims.SingleOrDefault(c => c.MasterId == master.UserId.Value)?.ClaimCount ?? 0,
            projectInfo,
            currentUser,
            cardBuilder)
        {
            Role = profiles.SingleOrDefault(p => p.UserId == master.UserId)?.Role.Value ?? "",
            IsPublic = master.IsPublic,
        })];

        MoveItems = [.. Masters.Select(m => new MasterMoveItemViewModel(m.MasterId, m.UserDetails.User.DisplayName, m.Role))];

        CanCurrentUserGrantRights = Masters.SingleOrDefault(acl => acl.UserId == currentUser.UserId)?.CanGrantRights ?? false;

        AnyoneElseCanGrantRights = Masters.Any(x => x.CanGrantRights && x.UserId != currentUser.UserId);

        CurrentUserId = currentUser.UserId;
        ProjectId = projectInfo.ProjectId.Value;
    }
}
