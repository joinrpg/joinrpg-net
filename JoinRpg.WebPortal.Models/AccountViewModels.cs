using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers.Web;

namespace JoinRpg.Web.Models
{
    public class ExternalLoginConfirmationViewModel
    {
        private static readonly string DisplayName;
        static ExternalLoginConfirmationViewModel()
        {
            DisplayName = "";
        }

        [Required]
        [Display(Name = "ExternalLoginConfirmationViewModel_Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "ExternalLoginConfirmationViewModel_AgreeWithTheRules")]
        [BooleanRequired(ErrorMessage = "ExternalLoginConfirmationViewModel_MustAgreeWithTheRules")]
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
        [Display(Name = "LoginViewModel_Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "LoginViewModel_Password")]
        public string Password { get; set; }
        public string ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "RegisterViewModel_FieldIsRequired")]
        [EmailAddress]
        [Display(Name = "RegisterViewModel_Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "RegisterViewModel_FieldIsRequired")]
        [StringLength(100,
            ErrorMessage = "RegisterViewModel_FieldShouldNotBeShorterThan",
            MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "RegisterViewModel_Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "RegisterViewModel_OneMoreTime")]
        [Compare("Password", ErrorMessage = "RegisterViewModel_PasswordsDoNotMatch")]
        public string ConfirmPassword { get; set; }

        [Required()]
        [Display(Name = "RegisterViewModel_AgreeWithTheRules")]
        [BooleanRequired(ErrorMessage = "RegisterViewModel_MustAgreeWithTheRules")]
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
        [Display(Name = "ResetPasswordViewModel_Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100,
            ErrorMessage = "ResetPasswordViewModel_FieldShouldNotBeShorterThan",
            MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "ResetPasswordViewModel_Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "ResetPasswordViewModel_OneMoreTime")]
        [Compare("Password", ErrorMessage = "ResetPasswordViewModel_PasswordsDoNotMatch")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "ForgotPasswordViewModel_Email")]
        public string Email { get; set; }
    }

    public class EditUserProfileViewModel
    {
        public int UserId { get; set; }

        [Display(Name = "Имя")]
        public string BornName { get; set; }

        [Display(Name = "EditUserProfileViewModel_MiddleName")]
        public string FatherName { get; set; }

        [Display(Name = "EditUserProfileViewModel_Surname")]
        public string SurName { get; set; }

        [Display(Name = "EditUserProfileViewModel_Nickname", Description = "EditUserProfileViewModel_NicknameDescription")]
        [Required(ErrorMessage = "EditUserProfileViewModel_EnterNickname")]
        public string PrefferedName { get; set; }

        [Display(Name = "EditUserProfileViewModel_Sex")]
        public Gender Gender { get; set; }

        [Display(Name = "EditUserProfileViewModel_PhoneNumber"), DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }

        [Display(Name = "EditUserProfileViewModel_Skype")]
        public string Skype { get; set; }

        [Display(Name = "EditUserProfileViewModel_LiveJournal")]
        public string Livejournal { get; set; }

        [Display(Name = "EditUserProfileViewModel_VK")]
        public string Vk { get; set; }

        [Display(Name = "EditUserProfileViewModel_Telegram")]
        public string Telegram { get; set; }

        [Display(Name = "EditUserProfileViewModel_AllNicknames", Description = "EditUserProfileViewModel_AllNicknamesDescription")]
        public string Nicknames { get; set; }

        [Display(Name = "EditUserProfileViewModel_AllGroups", Description = "EditUserProfileViewModel_AllGroupsDescription")]
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
