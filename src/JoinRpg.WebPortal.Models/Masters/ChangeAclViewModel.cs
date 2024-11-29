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
}
