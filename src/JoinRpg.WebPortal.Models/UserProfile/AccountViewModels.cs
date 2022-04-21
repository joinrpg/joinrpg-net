using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Helpers.Web;

namespace JoinRpg.Web.Models;

public class ExternalLoginConfirmationViewModel
{
    [Display(Name = "Согласен с правилами")]
    [BooleanRequired(ErrorMessage = "Согласитесь с правилами, чтобы продолжить")]
    public bool RulesApproved { get; set; }

    [ReadOnly(true)]
    public string LoginProviderName { get; set; }

    [ReadOnly(true)]
    public string ReturnUrl { get; set; }
}

public class ExternalLoginListViewModel
{
    public string ReturnUrl { get; set; }
    public List<AuthenticationDescriptionViewModel> ExternalLogins { get; set; }
}

public class AuthenticationDescriptionViewModel
{
    public string AuthenticationType { get; set; }

    public string? Caption { get; set; }
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
    [Display(Name = "Пароль")]
    public string Password { get; set; }
    public string ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Поле Email обязательно для заполнения")]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Поле Пароль обязательно для заполнения")]
    [StringLength(100,
        ErrorMessage = "{0} должен быть не короче {2} символов",
        MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Еще раз?")]
    [Compare("Password", ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; }

    [BooleanRequired(ErrorMessage = "Согласитесь с правилами, чтобы продолжить")]
    [Display(Name = "Согласен с правилами")]
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
        ErrorMessage = "{0} должен быть не короче {2} символов",
        MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Еще раз?")]
    [Compare("Password", ErrorMessage = "Пароли не совпадают")]
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
