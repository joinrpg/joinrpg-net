namespace JoinRpg.Web.Claims.Test;

public class RequestedSensitiveDataViewTest
{
    [Theory]
    [InlineData(true, false, false, "паспортным данным")]
    [InlineData(false, false, true, "дате рождения")]
    [InlineData(true, false, true, "паспортным данным и дате рождения")]
    [InlineData(false, true, true, "адресу регистрации и дате рождения")]
    [InlineData(true, true, true, "паспортным данным, адресу регистрации и дате рождения")]
    public void ListsOnlyRequestedData(bool passport, bool address, bool birthDate, string expected)
        => new RequestedSensitiveDataView(passport, address, birthDate).ToDativeList().ShouldBe(expected);

    [Fact]
    public void AllowConfirmationNamesRequestedData()
        => new RequestedSensitiveDataView(Passport: false, RegistrationAddress: true, BirthDate: false).AllowConfirmation()
            .ShouldBe("Мастера игры получат доступ к адресу регистрации из вашего профиля, пока проект активен и ваша заявка не отозвана");
}
