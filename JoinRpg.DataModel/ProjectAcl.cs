namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global (required by LINQ)
  public class ProjectAcl 
  {
    public int ProjectAclId { get; set; }
    public int ProjectId { get; set; }

    public virtual Project Project { get; set; }

    public int UserId { get; set; }

    public virtual  User User { get; set; }

    public bool CanChangeFields { get; set; }

    public bool CanChangeProjectProperties { get; set; }

    public bool IsOwner { get; set; }

    public bool CanGrantRights { get; set; }

    public bool CanApproveClaims { get; set; }

    public bool CanEditRoles { get; set; }
    public bool CanAcceptCash { get; set; }
    public bool CanManageMoney { get; set; }

    public static ProjectAcl CreateRootAcl(int userId)
    {
      return new ProjectAcl
      {
        CanChangeFields = true,
        CanChangeProjectProperties = true,
        UserId = userId,
        IsOwner = true,
        CanGrantRights =  true,
        CanApproveClaims = true,
        CanEditRoles =  true,
        CanAcceptCash = true,
        CanManageMoney = true
      };
    }
  }
}
