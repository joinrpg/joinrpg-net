using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Joinrpg.Markdown;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.Accommodation
{
    public class AccommodationListViewModel
    {
        public int ProjectId { get; set; }

        [DisplayName("Проект")]
        public string ProjectName { get; set; }

        [DisplayName("Типы проживания")]
        public List<RoomTypeListItemViewModel> RoomTypes { get; set; }

        public bool CanAssignRooms { get; set; }
        public bool CanManageRooms { get; set; }

        public AccommodationListViewModel(Project project,
            IReadOnlyCollection<RoomTypeInfoRow> roomTypes,
            int userId)
        {
            ProjectId = project.ProjectId;
            ProjectName = project.ProjectName;
            CanManageRooms = project.HasMasterAccess(userId, acl => acl.CanManageAccommodation);
            CanAssignRooms = project.HasMasterAccess(userId, acl => acl.CanSetPlayersAccommodations);
            RoomTypes = roomTypes.Select(rt => new RoomTypeListItemViewModel(rt, userId)).ToList();
        }
    }

    public class RoomTypeListItemViewModel : RoomTypeViewModelBase
    {
        private RoomTypeInfoRow rt;
        private int userId;

        [DisplayName("Проживает")]
        public int Occupied { get; }

        public override int RoomsCount { get; }

        public RoomTypeListItemViewModel(RoomTypeInfoRow row, int userId)
        {
            var entity = row.RoomType;
            var project = row.RoomType.Project;
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
            DescriptionView = entity.Description.ToHtmlString();
            ProjectId = project.ProjectId;
            CanManageRooms = project.HasMasterAccess(userId, acl => acl.CanManageAccommodation);
            CanAssignRooms = project.HasMasterAccess(userId, acl => acl.CanSetPlayersAccommodations);

            Occupied = row.Occupied;
            RoomsCount = row.RoomsCount;
            ApprovedClaims = row.ApprovedClaims;
        }

        public int ApprovedClaims { get; set; }
    }
}
