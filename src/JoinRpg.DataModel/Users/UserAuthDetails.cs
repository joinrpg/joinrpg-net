using System.ComponentModel.DataAnnotations;

namespace JoinRpg.DataModel;

public class UserAuthDetails
{
    public int UserId { get; set; }

    public bool EmailConfirmed { get; set; }

    public DateTime RegisterDate { get; set; }

    public DateTimeOffset? LastLoginDate { get; set; }

    public bool IsAdmin { get; set; }


    // В БД колонка NOT NULL с миграции 201904181355184, а на свойстве атрибута не было —
    // модель расходилась со схемой (ADR016, задача P2).
    // AllowEmptyStrings обязателен: в БД ограничение именно NOT NULL, а пустая строка ему
    // не противоречит — MyUserStore создаёт пользователя с AspNetSecurityStamp = "".
    // Без этого EF6 валидирует строже схемы и режет регистрацию на SaveChanges.
    [Required(AllowEmptyStrings = true)]
    public string AspNetSecurityStamp { get; set; }

    public override string ToString() => $"UserAuthDetails(UserId: {UserId}, EmailConfirmed: {EmailConfirmed}, RegisterDate: {RegisterDate})";
}
