using Bunit;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectMasterTools.Acl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.Acl;

/// <summary>
/// Острова прав мастеров показывают причину отказа сервера, а на сбое — свой текст, не сообщение исключения (#5735).
/// </summary>
public class AclPanelsServerErrorTest : BunitContext
{
    private const string Blocked = "Проект заблокирован на время восстановления данных, изменения в нём сейчас невозможны";
    private const string RawMessage = "Response status code does not indicate success: 502 (Bad Gateway).";

    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly UserIdentification UserId = new(2);

    private readonly FakeMasterClient client = new();

    public AclPanelsServerErrorTest()
    {
        Services.AddSingleton<IMasterAccessClient>(client);
        Services.AddSingleton<IMasterProfileClient>(client);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    [Fact]
    public void AddMasterShowsRefusalReason()
    {
        client.FailWith = new ServerProblemException(Blocked);

        var cut = RenderAddMaster();
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain(Blocked));
    }

    [Fact]
    public void AddMasterHidesUnexpectedError()
    {
        client.FailWith = new HttpRequestException(RawMessage);

        var cut = RenderAddMaster();
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Неизвестная серверная ошибка при добавлении мастера"));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }

    [Fact]
    public void SavePermissionsShowsRefusalReason()
    {
        client.FailWith = new ServerProblemException(Blocked);

        var cut = RenderPermissions();
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain(Blocked));
        cut.Markup.ShouldNotContain("Права доступа сохранены");
    }

    [Fact]
    public void SavePermissionsHidesUnexpectedError()
    {
        client.FailWith = new HttpRequestException(RawMessage);

        var cut = RenderPermissions();
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Неизвестная серверная ошибка при сохранении прав"));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }

    [Fact]
    public void SaveProfileShowsRefusalReason()
    {
        client.FailWith = new ServerProblemException(Blocked);

        var cut = RenderProfile();
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain(Blocked));
        cut.Markup.ShouldNotContain("Профиль мастера сохранён");
    }

    [Fact]
    public void SaveProfileHidesUnexpectedError()
    {
        client.FailWith = new HttpRequestException(RawMessage);

        var cut = RenderProfile();
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Неизвестная серверная ошибка при сохранении"));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }

    private IRenderedComponent<AddMasterPanel> RenderAddMaster()
        => Render<AddMasterPanel>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.UserId, UserId)
            .Add(x => x.MastersUrl, "/1/masters")
            .Add(x => x.UserProfileUrl, "/user/2"));

    private IRenderedComponent<MasterPermissionsPanel> RenderPermissions()
        => Render<MasterPermissionsPanel>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.UserId, UserId));

    private IRenderedComponent<MasterProfilePanel> RenderProfile()
        => Render<MasterProfilePanel>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.UserId, UserId));

    private class FakeMasterClient : IMasterAccessClient, IMasterProfileClient
    {
        public Exception? FailWith { get; set; }

        public Task<AddMasterViewModel> GetAddMaster(ProjectIdentification projectId, UserIdentification userId)
            => Task.FromResult(new AddMasterViewModel
            {
                ProjectId = projectId,
                UserId = userId,
                Role = AddMasterViewModel.DefaultRole,
                Permissions = Permissions(),
            });

        public Task AddMaster(AddMasterViewModel model) => Fail();

        public Task<MasterPermissionsViewModel> GetPermissions(ProjectIdentification projectId, UserIdentification userId)
            => Task.FromResult(new MasterPermissionsViewModel
            {
                ProjectId = projectId,
                UserId = userId,
                Permissions = Permissions(),
            });

        public Task SavePermissions(MasterPermissionsViewModel model) => Fail();

        public Task<MasterProfileViewModel> GetProfile(ProjectIdentification projectId, UserIdentification userId)
            => Task.FromResult(new MasterProfileViewModel
            {
                ProjectId = projectId,
                UserId = userId,
                Role = "Мастер",
            });

        public Task SaveProfile(MasterProfileViewModel model) => Fail();

        private Task Fail() => FailWith is null ? Task.CompletedTask : Task.FromException(FailWith);

        private static List<MasterPermissionValueViewModel> Permissions()
            => [new() { Permission = Permission.CanManageClaims, Value = true }];
    }
}
