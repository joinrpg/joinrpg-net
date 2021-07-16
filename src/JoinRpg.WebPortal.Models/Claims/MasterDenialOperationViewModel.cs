using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models
{
    public class MasterDenialOperationViewModel : ClaimOperationViewModel
    {
        public ClaimStatusView ClaimStatus { get; set; }

        public bool CharacterAutoCreated { get; set; }
        [Required(ErrorMessage = "Надо указать причину отказа"),
            Display(Name = "Причина отказа", Description = "Причины отклонения заявки будут видны только мастерам")]
        public ClaimDenialStatusView DenialStatus { get; set; }
        [Display(Name = "Персонажа...")]
        public MasterDenialExtraActionViewModel DeleteCharacter { get; set; }
    }
}

