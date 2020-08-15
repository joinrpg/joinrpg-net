using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.Accommodation
{
    public class AccomodationReportListItemViewModel : ILinkable
    {

        [Display(Name = "Игрок")]
        public string DisplayName { get; set; }

        [Display(Name = "ФИО")]
        public string FullName { get; set; }

        public int ClaimId { get; set; }

        public int ProjectId { get; set; }

        LinkType ILinkable.LinkType => LinkType.Claim;

        string ILinkable.Identification => ClaimId.ToString();

        int? ILinkable.ProjectId => ProjectId;
        [Display(Name = "Тип поселения")]
        public string AccomodationType { get; set; }
        [Display(Name = "Комната")]
        public string RoomName { get; set; }
        [Display(Name = "Телефон")]
        public string Phone { get; set; }
    }
}
