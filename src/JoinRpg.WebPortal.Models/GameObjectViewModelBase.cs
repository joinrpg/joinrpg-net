using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models
{
    public abstract class GameObjectViewModelBase : IProjectIdAware
    {
        public int ProjectId { get; set; }

        [ReadOnly(true)]
        public string ProjectName { get; set; }

        [Display(Name = "Публично?",
            Description =
                "Публичные сущности показываются в сетке ролей, их описание и карточки доступны всем.")]
        public bool IsPublic { get; set; } = true;
    }
}
