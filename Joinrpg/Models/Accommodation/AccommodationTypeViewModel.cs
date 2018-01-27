using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.Accommodation
{
    //todo I18n
    public class AccommodationTypeViewModel
    {
        [DisplayName("Название")]
        [Required]
        public string Name { get; set; }

        [DisplayName("Description")]
        public string Description { get; set; }

        [DisplayName("Стоимость проживания")]
        [Range(0, Int32.MaxValue)]
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
        public int TotalCapacity => Capacity * (Accommodations == null ? 0 : Accommodations.Count);

        [DisplayName("Проживает")]
        public int UsedSpace { get; set; }

        public ICollection<ProjectAccommodationViewModel> Accommodations { get; set; }
        public AccommodationTypeViewModel([NotNull]ProjectAccommodationType entity)
        {
            if (entity.ProjectId == 0 || entity.Id == 0)
            {
                throw new ArgumentException("Entity must be valid object");
            }
            ProjectId = entity.ProjectId;
            Project = entity.Project;
            Id = entity.Id;
            Cost = entity.Cost;
            Name = entity.Name;
            Capacity = entity.Capacity;
            IsInfinite = entity.IsInfinite;
            IsPlayerSelectable = entity.IsPlayerSelectable;
            IsAutoFilledAccommodation = entity.IsAutoFilledAccommodation;
            Description = entity.Description;
            Accommodations = ProjectAccommodationViewModel.NewListCollection(entity.ProjectAccommodations);

        }

        public AccommodationTypeViewModel()
        {
        }

        public ProjectAccommodationType GetProjectAccommodationTypeMock()
        {
            return new ProjectAccommodationType()
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
}
