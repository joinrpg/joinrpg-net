using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace JoinRpg.Web.Models.Accommodation
{
    public class AccommodationListViewModel
    {
        public int ProjectId { get; set; }
        [DisplayName("Проект")]
        public string ProjectName { get; set; }
        [DisplayName("Типы проживания")]
        public ICollection<AccommodationTypeViewModel> AccommodationTypes { get; set; }
    }
}
