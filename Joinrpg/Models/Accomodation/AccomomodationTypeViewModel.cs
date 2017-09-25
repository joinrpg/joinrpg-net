using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.Accomodation
{
    //todo I18n
    public class AccomomodationTypeViewModel
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
        public int Capacity => 0;
        public int UsedSpace => 0;

        public AccomomodationTypeViewModel([NotNull]ProjectAccomodationType entity)
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
        }

        public AccomomodationTypeViewModel()
        {
        }

        public ProjectAccomodationType GetProjectAccomodationMock()
        {
            return new ProjectAccomodationType()
            {
                ProjectId = ProjectId,
                Id = Id,
                Cost = Cost,
                Name = Name
            };
        }
    }
}
