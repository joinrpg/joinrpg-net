namespace JoinRpg.Web.Claims;

/// <summary>
/// Какие защищённые данные запросил проект (#5706) — чтобы игрок видел, к чему именно даёт доступ.
/// </summary>
public record RequestedSensitiveDataView(bool Passport, bool RegistrationAddress, bool BirthDate)
{
    public static RequestedSensitiveDataView From(ProjectProfileRequirementSettings settings)
        => new(settings.PassportRequested, settings.RegistrationAddressRequested, settings.BirthDateRequested);

    public bool IsAnyRequested() => Passport || RegistrationAddress || BirthDate;

    /// <summary>Перечень в дательном падеже, для «доступ к …»: «паспортным данным и дате рождения».</summary>
    public string ToDativeList()
    {
        var items = new List<string>();
        if (Passport)
        {
            items.Add("паспортным данным");
        }
        if (RegistrationAddress)
        {
            items.Add("адресу регистрации");
        }
        if (BirthDate)
        {
            items.Add("дате рождения");
        }
        return items.Count <= 1
            ? string.Concat(items)
            : string.Join(", ", items[..^1]) + " и " + items[^1];
    }

    /// <summary>Пояснение к галочке согласия в формах заявки и приглашения.</summary>
    public string ConsentDescription()
        => $"Мастера игры просят доступ к {ToDativeList()} из вашего профиля. Вероятно, это нужно для поселения. "
        + "Вы все равно сможете отправить заявку, даже если не предоставите доступ, но возможно, мастера отклонят вашу заявку.";

    /// <summary>Подтверждение на кнопке «Разрешить доступ к защищённым данным».</summary>
    public string AllowConfirmation()
        => $"Мастера игры получат доступ к {ToDativeList()} из вашего профиля, пока проект активен и ваша заявка не отозвана";
}
