using JoinRpg.DomainTypes.Users;

namespace JoinRpg.DomainTypes.Test;

public class UserInfoAgeTest
{
    private static UserInfo BuildUserInfo(DateOnly? birthDate)
        => new UserInfo(
            UserId: new UserIdentification(1),
            Social: new UserSocialNetworks(null, null, null, null, ContactsAccessType.OnlyForMasters),
            ActiveClaims: [],
            ActiveProjects: [],
            AllProjects: [],
            IsAdmin: false,
            SelectedAvatarId: null,
            Email: new Email("user@example.com"),
            EmailConfirmed: true,
            UserFullName: new UserFullName(null, BornName.FromOptional("Иван"), SurName.FromOptional("Иванов"), null),
            VerifiedProfileFlag: false,
            PhoneNumber: null,
            HasPassword: false,
            BirthDate: birthDate);

    [Fact]
    public void NoBirthDate_ReturnsNull()
    {
        BuildUserInfo(null).GetAgeOn(new DateOnly(2026, 1, 1)).ShouldBeNull();
    }

    [Fact]
    public void BirthdayIsToday_AgeIncreasesToday()
    {
        var user = BuildUserInfo(new DateOnly(2008, 6, 15));
        user.GetAgeOn(new DateOnly(2026, 6, 15)).ShouldBe(18);
    }

    [Fact]
    public void DayBeforeBirthday_AgeNotYetIncreased()
    {
        var user = BuildUserInfo(new DateOnly(2008, 6, 15));
        user.GetAgeOn(new DateOnly(2026, 6, 14)).ShouldBe(17);
    }

    [Fact]
    public void DayAfterBirthday_AgeAlreadyIncreased()
    {
        var user = BuildUserInfo(new DateOnly(2008, 6, 15));
        user.GetAgeOn(new DateOnly(2026, 6, 16)).ShouldBe(18);
    }

    [Fact]
    public void LeapYearBirthday_HandledOnNonLeapYear()
    {
        var user = BuildUserInfo(new DateOnly(2008, 2, 29));
        user.GetAgeOn(new DateOnly(2027, 3, 1)).ShouldBe(19);
        user.GetAgeOn(new DateOnly(2027, 2, 28)).ShouldBe(18);
    }
}
