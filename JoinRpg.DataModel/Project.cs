using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global (used by LINQ)
  public class Project
  {
    public int ProjectId { get; set; }

    public string ProjectName { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool Active { get; set; }

    public virtual ICollection<ProjectAcl> ProjectAcls { get; set; }

    public virtual ICollection<ProjectCharacterField> ProjectFields { get; set; }

    public IEnumerable<ProjectCharacterField> ActiveProjectFields => ProjectFields.Where(pf => pf.IsActive).OrderBy(pf => pf.Order);

    public IEnumerable<ProjectCharacterField> AllProjectFields => ProjectFields.OrderByDescending(pf => pf.IsActive).ThenBy(pf => pf.Order);

    public virtual ICollection<CharacterGroup>  CharacterGroups { get; set; }
    public CharacterGroup RootGroup => CharacterGroups.Single(g => g.IsRoot);

    public virtual ICollection<Character>  Characters { get; set; }

    public virtual ICollection<Claim> Claims { get; set; }

    public virtual ProjectDetails Details { get; set; }

    public virtual ICollection<PlotFolder> PlotFolders { get; set; }
  }

  public class ProjectDetails
  {
    public int ProjectId { get; set; }
    public MarkdownString ClaimApplyRules { get; set; } = new MarkdownString();
  }

  public static class ProjectStaticExtensions
  {
    public static bool HasSpecificAccess(this Project project, int currentUserId, Func<ProjectAcl, bool> requiredAccess)
    {
      return project.ProjectAcls.Where(requiredAccess).Any(pa => pa.UserId == currentUserId);
    }

    public static bool HasAccess(this Project project, int? currentUserId)
    {
      return project.ProjectAcls.Any(pa => pa.UserId == currentUserId);
    }
  }
}
