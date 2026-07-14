namespace JoinRpg.Web.Models;

public class ChangeAclViewModel
{
    public bool CanManageClaims { get; set; }
    public bool CanChangeFields { get; set; }
    public bool CanChangeProjectProperties { get; set; }
    public bool CanGrantRights { get; set; }
    public bool CanEditRoles { get; set; }
    public bool CanManageMoney { get; set; }
    public bool CanSendMassMails { get; set; }
    public bool CanManagePlots { get; set; }
    public bool CanManageAccommodation { get; set; }
    public bool CanSetPlayersAccommodations { get; set; }

    public int ProjectId { get; set; }

    public int UserId { get; set; }

    public Permission[] ToPermissions() =>
        new[]
        {
            (CanGrantRights, Permission.CanGrantRights),
            (CanChangeFields, Permission.CanChangeFields),
            (CanChangeProjectProperties, Permission.CanChangeProjectProperties),
            (CanManageClaims, Permission.CanManageClaims),
            (CanEditRoles, Permission.CanEditRoles),
            (CanManageMoney, Permission.CanManageMoney),
            (CanSendMassMails, Permission.CanSendMassMails),
            (CanManagePlots, Permission.CanManagePlots),
            (CanManageAccommodation, Permission.CanManageAccommodation),
            (CanSetPlayersAccommodations, Permission.CanSetPlayersAccommodations),
        }
        .Where(t => t.Item1)
        .Select(t => t.Item2)
        .ToArray();
}
