using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers.Web;

namespace JoinRpg.Web.Models
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "I agree with the rules")]
        [BooleanRequired(ErrorMessage = "You must agree with the rules in order to continue")]
        public bool RulesApproved { get; set; }
    }

    public class ExternalLoginListViewModel
    {
        public string ReturnUrl { get; set; }
        public List<AuthenticationDescriptionViewModel> ExternalLogins { get; set; }
    }

    public class LoginPageViewModel
    {
        public LoginViewModel Login { get; set; }
        public ExternalLoginListViewModel External { get; set; }
    }

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
        public string ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "The field {0} is required")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "The field {0} is required")]
        [StringLength(100,
            ErrorMessage = "{0} should not be shorter than {2} symbols",
            MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "One more time?")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        [Required()]
        [Display(Name = "I agree with the rules")]
        [BooleanRequired(ErrorMessage = "You must agree with the rules in order to continue")]
        public bool RulesApproved { get; set; }

        [Editable(false)]
        public string RecaptchaPublicKey { get; set; }

        [Editable(false)]
        public bool IsRecaptchaConfigured { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100,
            ErrorMessage = "{0} should not be shorter than {2} symbols",
            MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "One more time?")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class EditUserProfileViewModel
    {
        public int UserId { get; set; }

        [Display(Name = "Name")]
        public string BornName { get; set; }

        [Display(Name = "Middle name")]
        public string FatherName { get; set; }

        [Display(Name = "Surname")]
        public string SurName { get; set; }

        [Display(Name = "Nickname", Description ="NicknameDescription")]
        [Required(ErrorMessage = "Enter the nickname")]
        public string PrefferedName { get; set; }

        [Display(Name = "Sex")]
        public Gender Gender { get; set; }

        [Display(Name = "Phone number"), DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }

        [Display(Name = "Skype")]
        public string Skype { get; set; }

        [Display(Name = "LiveJournal")]
        public string Livejournal { get; set; }

        [Display(Name = "VK")]
        public string Vk { get; set; }

        [Display(Name = "Telegram")]
        public string Telegram { get; set; }

        [Display(Name = "All nicknames", Description = "AllNicknamesDescription")]
        public string Nicknames { get; set; }

        [Display(Name = "LARP organiser groups or clubs", Description = "LarpGroupsDescription")]
        public string GroupNames { get; set; }

        public int? LastClaimId { get; set; }
        public int? LastClaimProjectId { get; set; }

        [ReadOnly(true)]
        public bool IsVerifiedFlag { get; set; }

        // ????
        /*[Display(Name="Дата рождения", Description = "Указание даты рождения подтверждает мастерам, что вы совершеннолетний"), Required]
        public DateTime? BirthDate { get; set; }*/
    }
}
