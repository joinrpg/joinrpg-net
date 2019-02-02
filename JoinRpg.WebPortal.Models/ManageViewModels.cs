using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models
{
    public class IndexViewModel
    {
        public bool HasPassword { get; set; }
        public int LoginsCount { get; set; }

        public string Email { get; set; }
    }

    public class UserLoginInfoViewModel
    {
        public string LoginProvider { get; set; }

        public string ProviderKey { get; set; }
    }

    public class AuthenticationDescriptionViewModel
    {
        public string AuthenticationType { get; set; }

        public string Caption { get; set; }
    }

    public class ManageLoginsViewModel
    {
        public IList<UserLoginInfoViewModel> CurrentLogins { get; set; }
        public IList<AuthenticationDescriptionViewModel> OtherLogins { get; set; }
    }

    public class FactorViewModel
    {
        public string Purpose { get; set; }
    }

  public class SetPasswordViewModel
  {
    [Required]
    [StringLength(100, ErrorMessage = "{0} должен быть не короче {2} символов.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Новый пароль")]
    public string NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Еще раз")]
    [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; }
  }

  public class ChangePasswordViewModel : SetPasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Старый пароль")]
        public string OldPassword { get; set; }
    }
}
