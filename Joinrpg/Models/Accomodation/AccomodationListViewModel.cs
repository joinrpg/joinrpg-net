using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace JoinRpg.Web.Models.Accomodation
{
    public class AccomodationListViewModel
    {
        public int ProjectId { get; set; }
        [DisplayName("Проект")]
        public string ProjectName { get; set; }
        [DisplayName("Типы проживания")]
        public ICollection<AccomomodationTypeViewModel> AccomomodationTypes { get; set; }
    }
}
