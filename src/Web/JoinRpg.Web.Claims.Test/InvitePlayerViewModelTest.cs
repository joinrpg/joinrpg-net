using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Claims.Test;

/// <summary>
/// Форма «Пригласить игрока» не должна проходить валидацию без выбранного персонажа:
/// раньше <c>[Required]</c> стоял на не-nullable <c>int</c>, ноль проходил, и на сервер
/// уходил <c>CharacterId(projectId-0)</c> (#5292).
/// </summary>
public class InvitePlayerViewModelTest
{
    [Fact]
    public void FormWithoutCharacterIsInvalid()
    {
        var errors = Validate(new InvitePlayerViewModel { ProjectId = 1, UserLinks = ["42"] });

        errors.ShouldContain("Выберите персонажа");
    }

    [Fact]
    public void FormWithCharacterIsValid()
    {
        var errors = Validate(new InvitePlayerViewModel { ProjectId = 1, CharacterId = 5, UserLinks = ["42"] });

        errors.ShouldBeEmpty();
    }

    private static List<string?> Validate(InvitePlayerViewModel model)
    {
        var results = new List<ValidationResult>();
        _ = Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return [.. results.Select(r => r.ErrorMessage)];
    }
}
