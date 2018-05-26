using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global EF requirement
  public class ProjectPlugin
  {
    public int ProjectPluginId { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; }
    public string Configuration { get; set; }
    public virtual ICollection<PluginFieldMapping> PluginFieldMappings { get; set; } = new HashSet<PluginFieldMapping>();
  }

  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global EF requirement
  public class PluginFieldMapping
  {
    public int PluginFieldMappingId { get; set; }
    public int ProjectId { get; set; }
    public virtual Project Project { get; set; }
    public int ProjectFieldId { get; set; }
    public virtual ProjectField ProjectField { get; set; }
    public int ProjectPluginId { get; set; }
    [Required]
    public string MappingName { get; set;  }
    public PluginFieldMappingType PluginFieldMappingType { get; set;}
  }

  public enum PluginFieldMappingType
  {
    GenerateDefault, 
  }
}