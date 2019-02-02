using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Models
{
    public class AclViewModelBase
    {
        [ReadOnly(true), Display(Name = "Мастер")]
        public UserProfileDetailsViewModel UserDetails { get; set; }

        [ReadOnly(true)]
        public IEnumerable<GameObjectLinkViewModel> ResponsibleFor { get; protected set; }

        public int? ProjectAclId { get; set; }

        [Display(Name = "Проект")]
        public int ProjectId { get; set; }

        [Display(Name = "Игра"), ReadOnly(true)]
        public string ProjectName { get; protected set; }

        [Display(Name = "Заявок"), ReadOnly(true)]
        public int ClaimsCount { get; protected set; }

        public int UserId { get; set; }
    }
    public class AclViewModel : AclViewModelBase
    {
        [Display(
                Name = "Администратор заявок",
                Description = "может изменять статус заявок (принимать, отклонять, переносить в лист ожидания) и переназначать ответственного мастера для любой заявки в базе")]
        public bool CanManageClaims { get; set; }

        [Display(Name = "Настраивать поля персонажа", Description = "может добавлять, удалять или редактировать поля заявки и поля персонажа")]
        public bool CanChangeFields { get; set; }

        [Display(Name = "Настраивать проект", Description = "может изменять свойства проекта, переименовывать его, отправлять проект в архив и т.д.")]
        public bool CanChangeProjectProperties { get; set; }

        [Display(Name = "Давать доступ другим мастерам", Description = "может добавлять или удалять пользователей, настраивать права доступа")]
        public bool CanGrantRights { get; set; }

        [Display(Name = "Редактировать ролевку", Description = "может добавлять новые группы или новых персонажей, редактировать и удалять группы и персонажей")]
        public bool CanEditRoles { get; set; }

        [Display(Name = "Управлять финансами", Description = "может настраивать размеры взносов и способы оплаты, отмечать взносы, принятые любым мастером, возвращать взносы и т.д.")]
        public bool CanManageMoney { get; set; }

        [Display(Name = "Делать массовые рассылки", Description = "может разослать письма на емейл любой группе игроков (не только своим)")]
        public bool CanSendMassMails { get; set; }

        [Display(Name = "Редактор сюжетов", Description = "может добавлять и удалять сюжеты и вводные, назначать группы и персонажей, которым они видны, публиковать вводные")]
        public bool CanManagePlots { get; set; }

        [Display(Name = "Настраивать поселение", Description = "может добавлять/удалять номера и типы поселений")]
        public bool CanManageAccommodation { get; set; }

        [Display(Name = "Расселять игроков", Description = "может назначать игрокам номер")]
        public bool CanSetPlayersAccommodations { get; set; }

        public bool AccomodationEnabled { get; set; }

        public static AclViewModel FromAcl(ProjectAcl acl,
            int count,
            IReadOnlyCollection<CharacterGroup> groups,
            User currentUser,
            IUriService uriService)
        {
            return new AclViewModel
            {
                ProjectId = acl.ProjectId,
                ProjectAclId = acl.ProjectAclId,
                UserId = acl.UserId,
                CanManageClaims = acl.CanManageClaims,
                CanChangeFields = acl.CanChangeFields,
                CanChangeProjectProperties = acl.CanChangeProjectProperties,
                CanGrantRights = acl.CanGrantRights,
                CanEditRoles = acl.CanEditRoles,
                CanManageMoney = acl.CanManageMoney,
                CanSendMassMails = acl.CanSendMassMails,
                CanManagePlots = acl.CanManagePlots,
                CanManageAccommodation = acl.CanManageAccommodation,
                CanSetPlayersAccommodations = acl.CanSetPlayersAccommodations,
                ProjectName = acl.Project.ProjectName,

                AccomodationEnabled = acl.Project.Details.EnableAccommodation,

                ClaimsCount = count,
                UserDetails = new UserProfileDetailsViewModel(acl.User,
                    (AccessReason)acl.User.GetProfileAccess(currentUser)),
                ResponsibleFor = groups.AsObjectLinks(uriService),
            };
        }
    }

    public class DeleteAclViewModel : AclViewModelBase
    {

        [Display(
          Name = "Новый ответственный мастер",
          Description = "Ответственный мастер, который будет назначен тем заявкам, за которые раньше отвечал этот мастер.")]
        public int? ResponsibleMasterId { get; set; }

        [ReadOnly(true)]
        public IEnumerable<MasterListItemViewModel> Masters { get; private set; }
        public static DeleteAclViewModel FromAcl(ProjectAcl acl, int count, IReadOnlyCollection<CharacterGroup> groups, IUriService uriService)
        {
            return new DeleteAclViewModel
            {
                ProjectId = acl.ProjectId,
                ProjectAclId = acl.ProjectAclId,
                UserId = acl.UserId,
                ProjectName = acl.Project.ProjectName,
                ClaimsCount = count,
                UserDetails = new UserProfileDetailsViewModel(acl.User, AccessReason.CoMaster),
                ResponsibleFor = groups.AsObjectLinks(uriService),
                Masters = acl.Project.GetMasterListViewModel().Where(master => master.Id != acl.UserId.ToString()).OrderBy(m => m.Name),
            };
        }
    }
}
