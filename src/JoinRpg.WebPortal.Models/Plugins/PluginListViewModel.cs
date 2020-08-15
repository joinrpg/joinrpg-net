using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.PluginHost.Interfaces;

namespace JoinRpg.Web.Models.Plugins
{
    public class PluginListViewModel : IProjectIdAware
    {
        public PluginListViewModel(int projectId, IEnumerable<ProjectPluginInfo> pluginInfo, int currentUserId)
        {
            ProjectId = projectId;
            Plugins = pluginInfo.Select(pi => new PluginListItemViewModel(pi)).ToList();
        }

        public int ProjectId { get; }
        public IReadOnlyCollection<PluginListItemViewModel> Plugins { get; }
    }

    public class PluginListItemViewModel
    {
        public PluginListItemViewModel(ProjectPluginInfo pi)
        {
            Name = pi.Name;
            Installed = pi.Installed;
            StaticPages = pi.StaticPages;
            Description = pi.Description;
            ActiveMappings = pi.ActiveMappings.Select(m => new PluginFieldMappingViewModel(m)).ToList();
        }

        public List<PluginFieldMappingViewModel> ActiveMappings { get; }

        [Display(Name = "Страницы")]
        public IReadOnlyCollection<string> StaticPages { get; }

        [Display(Name = "Имя плагина")]
        public string Name { get; }

        [Display(Name = "Описание")]
        public string Description { get; }
        public bool Installed { get; }
    }

    public class PluginFieldMappingViewModel
    {
        public string FieldName { get; }
        public int ProjectFieldId { get; }
        public string MappingName { get; }
        public PluginFieldMappingType MappingType { get; }

        public PluginFieldMappingViewModel(PluginFieldMapping m)
        {
            FieldName = m.ProjectField.FieldName;
            ProjectFieldId = m.ProjectFieldId;
            MappingName = m.MappingName;
            MappingType = m.PluginFieldMappingType;
        }
    }
}
