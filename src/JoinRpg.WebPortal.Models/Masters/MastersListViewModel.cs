using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.Masters;

public class MastersListViewModel
{
    public IReadOnlyCollection<AclViewModel> Masters { get; }

    public bool CanCurrentUserGrantRights { get; }

    public bool AnyoneElseCanGrantRights { get; }

    public int CurrentUserId { get; }

    public MastersListViewModel(
        Project project,
        IReadOnlyCollection<ClaimCountByMaster> claims,
        IReadOnlyCollection<CharacterGroup> groups,
        User currentUser,
        IUriService uriService)
    {
        Masters = project.ProjectAcls.Select(acl =>
        {
            return new AclViewModel(acl, currentUser, claims.SingleOrDefault(c => c.MasterId == acl.UserId)?.ClaimCount ?? 0,
              groups.Where(gr => gr.ResponsibleMasterUserId == acl.UserId && gr.IsActive),
                uriService);
        }).ToList();

        CanCurrentUserGrantRights = Masters.Single(acl => acl.UserId == currentUser.UserId).CanGrantRights;

        AnyoneElseCanGrantRights = Masters.Any(x => x.CanGrantRights && x.UserId != currentUser.UserId);

        CurrentUserId = currentUser.UserId;
    }
}
