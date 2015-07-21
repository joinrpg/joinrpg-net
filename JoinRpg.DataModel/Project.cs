using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoinRpg.DataModel
{
  public class Project
  {
    public int ProjectId { get; set; }

    public string ProjectName { get; set; }

    public DateTime CreatedDate { get; set; }

    public int CreatorUserId { get; set; }

    public virtual User CreatorUser { get; set; }

    public bool Active { get; set; }

    public virtual ICollection<ProjectAcl> ProjectAcls { get; set; }

    public virtual ICollection<ProjectCharacterField> ProjectFields { get; set; }

    public IEnumerable<ProjectCharacterField> ActiveProjectFields => ProjectFields.Where(pf => pf.IsActive).OrderBy(pf => pf.Order);

    public IEnumerable<ProjectCharacterField> AllProjectFields => ProjectFields.OrderByDescending(pf => pf.IsActive).ThenBy(pf => pf.Order);

    public virtual ICollection<CharacterGroup>  CharacterGroups { get; set; }
    public CharacterGroup RootGroup => CharacterGroups.Single(g => g.IsRoot);
  }

  public static class ProjectExtensions
  {
    public static bool CheckAccess(this Project project, int currentUserId, Func<ProjectAcl, bool> requiredAccess)
    {
      return project.ProjectAcls.Where(requiredAccess).Any(pa => pa.UserId == currentUserId);
    }
  }
}
