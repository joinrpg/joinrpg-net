using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
  public class ExternalLoginConfirmationViewModel
  {
    [Required]
    [Display(Name = "Email")]
    public string Email { get; set; }
  }

  public class ExternalLoginListViewModel
  {
    public string ReturnUrl { get; set; }
  }

  public class SendCodeViewModel
  {
    public string SelectedProvider { get; set; }
    public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
    public string ReturnUrl { get; set; }
    public bool RememberMe { get; set; }
  }

  public class VerifyCodeViewModel
  {
    [Required]
    public string Provider { get; set; }

    [Required]
    [Display(Name = "Code")]
    public string Code { get; set; }

    public string ReturnUrl { get; set; }

    [Display(Name = "Remember this browser?")]
    public bool RememberBrowser { get; set; }

    public bool RememberMe { get; set; }
  }

  public class ForgotViewModel
  {
    [Required]
    [Display(Name = "Email")]
    public string Email { get; set; }
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

    [Display(Name = "Запомнить меня?")]
    public bool RememberMe { get; set; }
  }

  public class RegisterViewModel
  {
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "{0} должен быть не короче {2} символов.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Еще раз?")]
    [Compare("Password", ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; }
  }

  public class ResetPasswordViewModel
  {
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "{0} должен быть не короче {2} символов.", MinimumLength = 6)]
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

  public class EditUserProfileViewModel
  {
    public int UserId { get; set; }

    [Display(Name="Имя")]
    public string BornName { get; set; }

    [Display(Name = "Отчество")]
    public string FatherName { get; set; }

    [Display(Name = "Фамилия")]
    public string SurName { get; set; }

    [Display(Name = "Как называть", Description = "Под каким именем/ником вы хотите быть известны? Рекомендуем ввести ник русскими буквами (или фамилию/имя, если у вас нет ника).")]
    [Required]
    public string PrefferedName { get; set; }

    [Display(Name="Пол")]
    public Gender Gender { get; set; }

    [Display(Name="Номер телефона"),DataType(DataType.PhoneNumber)]
    public string PhoneNumber { get; set; }
    [Display(Name = "Skype")]
    public string Skype { get; set; }
    [Display(Name = "Все ник(и)", Description = "Все ники, через запятую, под которыми вас могут знать. Это позволит находить вас поиском даже тем, кто использует ваш старый или по другому написанный ник")]
    public string Nicknames { get; set; }
    [Display(Name = "МГ/Клубы", Description = "Все мастерские группы/клубы, через запятую, к которым вы себя относите. ")]
    public string GroupNames { get; set; }

    /*[Display(Name="Дата рождения", Description = "Указание даты рождения подтверждает мастерам, что вы совершеннолетний"), Required]
    public DateTime? BirthDate { get; set; }*/
  }

}