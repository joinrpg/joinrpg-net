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

namespace JoinRpg.Web.Models.Accomodation
{
    public class ProjectAccomodationVewModel
    {
        public int Id { get; set; }
        public int AccomodationTypeId { get; set; }
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
        public bool IsAutofilledAccomodation { get; set; } = false;
        public virtual ICollection<ClaimViewModel> Inhabitants { get; set; }




        public ProjectAccomodationVewModel([NotNull]ProjectAccomodation entity)
        {
            if (entity.ProjectId == 0 || entity.Id == 0)
            {
                throw new ArgumentException("Entity must be valid object");
            }
            Id = entity.Id;
            Name = entity.Name;
            ProjectId = entity.ProjectId;
            AccomodationTypeId = entity.AccomodationTypeId;
            Capacity = entity.Capacity;
            IsPlayerSelectable = entity.IsPlayerSelectable;
            IsAutofilledAccomodation = entity.IsAutofilledAccomodation;
            IsInfinite = entity.IsInfinite;
        }

        public ProjectAccomodationVewModel()
        {
        }

        public ProjectAccomodation GetProjectAccomodationMock()
        {
            return new ProjectAccomodation()
            {
                Id = Id,
                ProjectId = ProjectId,
                Name = Name,
                AccomodationTypeId = AccomodationTypeId,
                Capacity = Capacity,
                IsInfinite = IsInfinite,
                IsPlayerSelectable = IsPlayerSelectable,
                IsAutofilledAccomodation = IsAutofilledAccomodation
            };
        }

        internal static ICollection<ProjectAccomodationVewModel> NewListCollection([NotNull] ICollection<ProjectAccomodation> projectAccomodations)
        {
            //todo add claim calculation logic 
            return  projectAccomodations.Select(accomodation => new ProjectAccomodationVewModel()
            {
                AccomodationTypeId = accomodation.AccomodationTypeId,
                ProjectId = accomodation.ProjectId,
                Capacity = accomodation.Capacity,
                ConfirmedCount = 0,
                NonConfirmedCount = 0,
                Id = accomodation.Id,
                Inhabitants = new List<ClaimViewModel>(),
                IsAutofilledAccomodation = accomodation.IsAutofilledAccomodation,
                IsInfinite = accomodation.IsInfinite,
                IsPlayerSelectable = accomodation.IsPlayerSelectable,
                Name = accomodation.Name
            }).ToSafeReadOnlyCollection();
            
        }
     
    }
    
}
