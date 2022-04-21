using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Web.Models.UserProfile;

namespace JoinRpg.Web.Models;

public class EditUserProfileViewModel
{
    public int UserId { get; set; }

    [Display(Name = "Имя")]
    public string BornName { get; set; }

    [Display(Name = "Отчество")]
    public string FatherName { get; set; }

    [Display(Name = "Фамилия")]
    public string SurName { get; set; }

    [Display(Name = "Как называть",
        Description =
            "Под каким именем/ником вы хотите быть известны? Рекомендуем ввести ник русскими буквами (или фамилию/имя, если у вас нет ника).")]
    [Required(ErrorMessage = "Укажите, как вас называть")]
    public string PrefferedName { get; set; }

    [Display(Name = "Пол")]
    public Gender Gender { get; set; }

    [Display(Name = "Номер телефона"), DataType(DataType.PhoneNumber)]
    public string PhoneNumber { get; set; }

    [Display(Name = "Skype")]
    public string Skype { get; set; }

    [Display(Name = "ЖЖ")]
    public string Livejournal { get; set; }

    [Display(Name = "VK")]
    [UIHint("Vkontakte")]
    [ReadOnly(true)]
    public string Vk { get; set; }

    [Display(Name = "Telegram")]
    public string Telegram { get; set; }

    [Display(Name = "Все ник(и)",
        Description =
            "Все ники, через запятую, под которыми вас могут знать. Это позволит находить вас поиском даже тем, кто использует ваш старый или по другому написанный ник")]
    public string Nicknames { get; set; }

    [Display(Name = "МГ/Клубы",
        Description =
            "Все мастерские группы/клубы, через запятую, к которым вы себя относите. ")]
    public string GroupNames { get; set; }

    public int? LastClaimId { get; set; }
    public int? LastClaimProjectId { get; set; }

    [ReadOnly(true)]
    public bool IsVerifiedFlag { get; set; }

    [ReadOnly(true)]
    public bool IsVkVerifiedFlag { get; set; }

    [Display(Name = "Публичность соцсетей")]
    public ContactsAccessTypeView SocialNetworkAccess { get; set; }

    /*[Display(Name="Дата рождения", Description = "Указание даты рождения подтверждает мастерам, что вы совершеннолетний"), Required]
    public DateTime? BirthDate { get; set; }*/

    public IList<UserLoginInfoViewModel> SocialLoginStatus { get; set; }

    public string Email { get; set; }
    public bool HasPassword { get; set; }

    public UserAvatarListViewModel Avatars { get; set; }
    public ManageMessageId? Message { get; set; }
}
