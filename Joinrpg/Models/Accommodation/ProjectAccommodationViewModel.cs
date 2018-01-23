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
    public class ProjectAccommodationViewModel
    {
        public int Id { get; set; }
        public int AccommodationTypeId { get; set; }
        public int ProjectId { get; set; }
        [Required]
        [DisplayName("Название (номер)")]
        public string Name { get; set; }
        public virtual ICollection<ClaimViewModel> Inhabitants { get; set; }




        public ProjectAccommodationViewModel([NotNull]ProjectAccommodation entity)
        {
            if (entity.ProjectId == 0 || entity.Id == 0)
            {
                throw new ArgumentException("Entity must be valid object");
            }
            Id = entity.Id;
            Name = entity.Name;
            ProjectId = entity.ProjectId;
            AccommodationTypeId = entity.AccommodationTypeId;
        }

        public ProjectAccommodationViewModel()
        {
        }

        public ProjectAccommodation GetProjectAccommodationMock()
        {
            return new ProjectAccommodation()
            {
                Id = Id,
                ProjectId = ProjectId,
                Name = Name,
                AccommodationTypeId = AccommodationTypeId
            };
        }

        internal static ICollection<ProjectAccommodationViewModel> NewListCollection([NotNull] ICollection<ProjectAccommodation> projectAccomodations)
        {
            //todo add claim calculation logic 
            return  projectAccomodations.Select(accommodation => new ProjectAccommodationViewModel()
            {
                AccommodationTypeId = accommodation.AccommodationTypeId,
                ProjectId = accommodation.ProjectId,
                Id = accommodation.Id,
                Inhabitants = new List<ClaimViewModel>(),
                Name = accommodation.Name
            }).ToSafeReadOnlyCollection();
            
        }
     
    }
    
}
