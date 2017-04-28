using System.ComponentModel.DataAnnotations;

namespace JoinRpg.DataModel
{
  public class ProjectItemTag
  {
    [Key]
    public int ProjectItemTagId { get; set; }
    [MaxLength(400)]
    public string TagName { get; set; }
  }
}