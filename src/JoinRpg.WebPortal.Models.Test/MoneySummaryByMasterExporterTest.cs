using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Exporters;

namespace JoinRpg.WebPortal.Models.Test;

// Регрессия на переход MasterBalanceViewModel.Master с User (EF-сущность) на UserInfo:
// ComplexElementMemberColumn собирает Expression через несколько .Compile() (см. CombineGetters),
// поэтому ошибка в выражении не всплывёт при dotnet build — только в рантайме на ExtractValue.
public class MoneySummaryByMasterExporterTest
{
    private sealed class StubUriService : IUriService
    {
        public string Get(ILinkable link) => "/stub";
        public Uri GetUri(ILinkable link) => new("/stub", UriKind.Relative);
    }

    private static UserInfo BuildUserInfo(TelegramSocialLink? telegram, string? phoneNumber)
        => new UserInfo(
            UserId: new UserIdentification(1),
            Social: new UserSocialNetworks(telegram, null, null, null, ContactsAccessType.OnlyForMasters),
            ActiveClaims: [],
            ActiveProjects: [],
            AllProjects: [],
            IsAdmin: false,
            SelectedAvatarId: null,
            Email: new Email("master@example.com"),
            EmailConfirmed: true,
            UserFullName: new UserFullName(null, BornName.FromOptional("Иван"), SurName.FromOptional("Иванов"), null),
            VerifiedProfileFlag: false,
            PhoneNumber: phoneNumber,
            HasPassword: false);

    private static MasterBalanceViewModel BuildBalance(UserInfo master)
        => new MasterBalanceViewModel(master, projectId: 1,
            masterOperations: Array.Empty<FinanceOperation>(),
            masterTransfers: Array.Empty<MoneyTransfer>());

    private static Dictionary<string, object?> ExtractRow(MasterBalanceViewModel row)
    {
        var exporter = new MoneySummaryByMasterExporter(new StubUriService());
        return exporter.ParseColumns().ToDictionary(c => c.Name ?? "", c => c.ExtractValue(row));
    }

    [Fact]
    public void ExtractsMasterColumns_WhenTelegramPresent()
    {
        var master = BuildUserInfo(
            telegram: new TelegramSocialLink(new TelegramChatId(123), new PrefferedName("ivan")),
            phoneNumber: "+79001234567");

        var values = ExtractRow(BuildBalance(master));

        values["Мастер.Email"].ShouldBe(master.Email);
        values["Телефон"].ShouldBe("+79001234567");
        values["Telegram"].ShouldBe(master.Social.Telegram!.ToString());
    }

    [Fact]
    public void ExtractsMasterColumns_WhenTelegramAbsent()
    {
        var master = BuildUserInfo(telegram: null, phoneNumber: null);

        var values = ExtractRow(BuildBalance(master));

        values["Телефон"].ShouldBeNull();
        values["Telegram"].ShouldBeNull();
    }
}
