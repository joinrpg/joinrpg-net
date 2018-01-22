using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using WebGrease.Css.Extensions;

namespace JoinRpg.Web.Models.Accommodation
{
    public class ProjectAccommodationVewModel
    {
        public int Id { get; set; }
        public int AccommodationTypeId { get; set; }
        public int ProjectId { get; set; }
        [Required]
        [DisplayName("Название (номер)")]
        public string Name { get; set; }
        [DisplayName("Можно выбрать")]
        public bool IsPlayerSelectable { get; set; } = true;
        [DisplayName("Бесконечное")]
        public bool IsInfinite { get; set; } = false;
        [DisplayName("Вместимость")]
        [Range(0, Int32.MaxValue)]
        public int Capacity { get; set; }
        [DisplayName("Подтверждено проживание")]
        public int ConfirmedCount { get; set; }
        [DisplayName("Не подтверждено проживание")]
        public int NonConfirmedCount { get; set; }
        [DisplayName("Автозаполнение")]
        public bool IsAutoFilledAccommodation { get; set; } = false;
        public virtual ICollection<ClaimViewModel> Inhabitants { get; set; }




        public ProjectAccommodationVewModel([NotNull]ProjectAccommodation entity)
        {
            if (entity.ProjectId == 0 || entity.Id == 0)
            {
                throw new ArgumentException("Entity must be valid object");
            }
            Id = entity.Id;
            Name = entity.Name;
            ProjectId = entity.ProjectId;
            AccommodationTypeId = entity.AccommodationTypeId;
            Capacity = entity.Capacity;
            IsPlayerSelectable = entity.IsPlayerSelectable;
            IsAutoFilledAccommodation = entity.IsAutoFilledAccommodation;
            IsInfinite = entity.IsInfinite;
        }

        public ProjectAccommodationVewModel()
        {
        }

        public ProjectAccommodation GetProjectAccommodationMock()
        {
            return new ProjectAccommodation()
            {
                Id = Id,
                ProjectId = ProjectId,
                Name = Name,
                AccommodationTypeId = AccommodationTypeId,
                Capacity = Capacity,
                IsInfinite = IsInfinite,
                IsPlayerSelectable = IsPlayerSelectable,
                IsAutoFilledAccommodation = IsAutoFilledAccommodation
            };
        }

        internal static ICollection<ProjectAccommodationVewModel> NewListCollection([NotNull] ICollection<ProjectAccommodation> projectAccomodations)
        {
            //todo add claim calculation logic 
            return  projectAccomodations.Select(accommodation => new ProjectAccommodationVewModel()
            {
                AccommodationTypeId = accommodation.AccommodationTypeId,
                ProjectId = accommodation.ProjectId,
                Capacity = accommodation.Capacity,
                ConfirmedCount = 0,
                NonConfirmedCount = 0,
                Id = accommodation.Id,
                Inhabitants = new List<ClaimViewModel>(),
                IsAutoFilledAccommodation = accommodation.IsAutoFilledAccommodation,
                IsInfinite = accommodation.IsInfinite,
                IsPlayerSelectable = accommodation.IsPlayerSelectable,
                Name = accommodation.Name
            }).ToSafeReadOnlyCollection();
            
        }
     
    }
    
}
