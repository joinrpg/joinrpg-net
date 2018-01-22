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
        [DisplayName("Стоимость проживания")]
        [Range(0, Int32.MaxValue)]
        public int Cost { get; set; }

        public int Id { get; set; }
        public int ProjectId { get; set; }
        public Project Project { get; set; }
        [DisplayName("Объем номерного фонда")]
        public int Capacity { get; set; }
        [DisplayName("Проживает")]
        public int UsedSpace { get; set; }

        public  ICollection<ProjectAccommodationVewModel> Accommodations { get; set; }
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
            Accommodations = ProjectAccommodationVewModel.NewListCollection(entity.ProjectAccommodations);
            Capacity = Accommodations.Sum(x=>x.Capacity);
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
                Name = Name
            };
        }
    }
}
