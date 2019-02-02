using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using JetBrains.Annotations;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.Accommodation
{
    public abstract class RoomTypeViewModelBase
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }

        [DisplayName("Количество мест в номере")]
        [Range(1, int.MaxValue)]
        public int Capacity { get; set; }

        [DisplayName("Бесконечное поселение")]
        public bool IsInfinite { get; set; } = false;

        [Display(Name = "Игроки могут выбрать данный тип проживания",
            Description = "Если снять этот флаг, то только мастер может назначать этот тип поселения игрокам")]
        public bool IsPlayerSelectable { get; set; } = true;

        [DisplayName("Автозаполнение")]
        public bool IsAutoFilledAccommodation { get; set; } = false;

        public abstract int RoomsCount { get; }

        [DisplayName("Общее количество мест")]
        public int TotalCapacity
            => RoomsCount * Capacity;

        [DisplayName("Название")]
        [Required]
        public string Name { get; set; }

        [DisplayName("Описание")]
        public IHtmlString DescriptionView { get; set; }

        [DisplayName("Цена за 1 место")]
        public int Cost { get; set; }

        public bool CanAssignRooms { get; set; }
        public bool CanManageRooms { get; set; }
    }

    //todo I18n
    public class RoomTypeViewModel : RoomTypeViewModelBase
    {
        [DisplayName("Описание"), UIHint("MarkdownString")]
        public string DescriptionEditable { get; set; }

        public Project Project { get; set; }


        /// <summary>
        /// List of rooms
        /// </summary>
        public IReadOnlyList<RoomViewModel> Rooms { get; }

        public override int RoomsCount
            => Rooms?.Count ?? 0;

        /// <summary>
        /// List of requests sent for this room type
        /// </summary>
        public IReadOnlyList<AccRequestViewModel> Requests { get; set; }

        /// <summary>
        /// List of requests not assigned to any room
        /// </summary>
        public IReadOnlyList<AccRequestViewModel> UnassignedRequests { get; set; }

        public RoomTypeViewModel([NotNull]ProjectAccommodationType entity, int userId)
            : this(entity.Project, userId)
        {
            if (entity.ProjectId == 0 || entity.Id == 0)
            {
                throw new ArgumentException("Entity must be valid object");
            }
            Id = entity.Id;
            Cost = entity.Cost;
            Name = entity.Name;
            Capacity = entity.Capacity;
            IsInfinite = entity.IsInfinite;
            IsPlayerSelectable = entity.IsPlayerSelectable;
            IsAutoFilledAccommodation = entity.IsAutoFilledAccommodation;
            DescriptionEditable = entity.Description.Contents;
            DescriptionView = entity.Description.ToHtmlString();

            // Creating a list of requests associated with this room type
            Requests = entity.Desirous.Select(ar => new AccRequestViewModel(ar)).ToList();

            // Creating a list of requests not assigned to any room
            List<AccRequestViewModel> ua = Requests.Where(ar => ar.RoomId == 0).ToList();
            ua.Sort((x, y) =>
            {
                int result = x.Persons - y.Persons;
                if (result == 0)
                    result = x.FeeToPay - y.FeeToPay;
                if (result == 0)
                    result = string.Compare(x.PersonsList, y.PersonsList, StringComparison.CurrentCultureIgnoreCase);
                return result;
            });
            UnassignedRequests = ua;

            // Creating a list of rooms contained in this room type
            List<RoomViewModel> rl = entity.ProjectAccommodations.Select(acc => new RoomViewModel(acc, this)).ToList();
            rl.Sort((x, y) =>
            {
                if (x.Occupancy == y.Occupancy)
                {
                    if (int.TryParse(x.Name, out int xn) && int.TryParse(y.Name, out int yn))
                        return xn - yn;
                    return string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
                }
                if (x.Occupancy == x.Capacity)
                    return 1;
                if (y.Occupancy == y.Capacity)
                    return -1;
                return y.Occupancy - x.Occupancy;
            });
            Rooms = rl;
        }

        public RoomTypeViewModel(Project project, int userId)
        {
            Project = project;
            ProjectId = project.ProjectId;
            CanManageRooms = project.HasMasterAccess(userId, acl => acl.CanManageAccommodation);
            CanAssignRooms = project.HasMasterAccess(userId, acl => acl.CanSetPlayersAccommodations);
        }

        public RoomTypeViewModel()
        {
        }

        public ProjectAccommodationType ToEntity()
            => new ProjectAccommodationType
            {
                ProjectId = ProjectId,
                Id = Id,
                Cost = Cost,
                Name = Name,
                Capacity = Capacity,
                Description = new MarkdownString(DescriptionEditable),
                IsInfinite = IsInfinite,
                IsPlayerSelectable = IsPlayerSelectable,
                IsAutoFilledAccommodation = IsAutoFilledAccommodation,
            };
    }
}
