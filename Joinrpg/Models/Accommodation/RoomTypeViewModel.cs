using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.Accommodation
{
    //todo I18n
    public class RoomTypeViewModel
    {
        [DisplayName("Название")]
        [Required]
        public string Name { get; set; }

        [DisplayName("Описание")]
        public string Description { get; set; }

        [DisplayName("Цена за 1 место")]
        public int Cost { get; set; }

        public int Id { get; set; }
        public int ProjectId { get; set; }
        public Project Project { get; set; }

        [DisplayName("Количество мест в номере")]
        public int Capacity { get; set; }

        [DisplayName("Бесконечное поселение")]
        public bool IsInfinite { get; set; } = false;

        [DisplayName("Игроки могут выбрать данный тип проживания")]
        public bool IsPlayerSelectable { get; set; } = true;

        [DisplayName("Автозаполнение")]
        public bool IsAutoFilledAccommodation { get; set; } = false;

        [DisplayName("Объем номерного фонда данного типа")]
        public int TotalCapacity => Capacity * Rooms?.Count ?? 0;

        [DisplayName("Проживает")]
        public int Occupied { get; set; }

        public ICollection<RoomViewModel> Rooms { get; set; }

        public bool CanAssignRooms { get; set; }

        public bool CanManageRooms { get; set; }

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
            Description = entity.Description;
            Rooms = RoomViewModel.NewListCollection(entity.ProjectAccommodations);
            Occupied = Rooms.Sum(rv => rv.Occupancy);
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
                Description = Description,
                IsInfinite = IsInfinite,
                IsPlayerSelectable = IsPlayerSelectable,
                IsAutoFilledAccommodation = IsAutoFilledAccommodation
            };
    }
}
