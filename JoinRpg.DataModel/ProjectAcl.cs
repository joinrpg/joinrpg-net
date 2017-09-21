using System;
using JoinRpg.Helpers;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global (required by LINQ)
  public class ProjectAcl : IProjectEntity
  {
    public int ProjectAclId { get; set; }
    public int ProjectId { get; set; }

    public virtual Project Project { get; set; }

    public int UserId { get; set; }

    public virtual  User User { get; set; }

    public Guid Token { get; set; } = new Guid();

    public bool CanChangeFields { get; set; }

    public bool CanChangeProjectProperties { get; set; }

    public bool IsOwner { get; set; }

    public bool CanGrantRights { get; set; }

    public bool CanManageClaims { get; set; }

    public bool CanEditRoles { get; set; }
    public bool CanManageMoney { get; set; }

    public bool CanSendMassMails { get; set; }

    public bool CanManagePlots { get; set; }

    public static ProjectAcl CreateRootAcl(int userId, bool isOwner = false)
    {
      return new ProjectAcl
      {
        CanChangeFields = true,
        CanChangeProjectProperties = true,
        UserId = userId,
        IsOwner = isOwner,
        CanGrantRights =  true,
        CanManageClaims = true,
        CanEditRoles =  true,
        CanManageMoney = true,
        CanSendMassMails = true,
        CanManagePlots = true,
      };
    }

    int IOrderableEntity.Id => ProjectAclId;
  }
}
