using System.Net;
using System.Text;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Finances;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.ProjectMetadata.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Search;
using JoinRpg.Web.AdminTools.PscbPayments;
using JoinRpg.WebPortal.Managers.AdminTools;
using PscbApi;

namespace JoinRpg.WebPortal.Managers.Test.AdminTools.Pscb;

/// <summary>
/// Подставные данные списка платежей ПСКБ: банк подменён тем же приёмом, что и в
/// <c>PscbApi.Test</c> — к сети тесты не ходят.
/// </summary>
internal static class PscbPaymentsTestData
{
    internal static readonly DateTimeOffset From = new(2026, 10, 1, 0, 0, 0, TimeSpan.FromHours(3));

    public static PscbPaymentsQuery Query() => new(From, From.AddDays(5), null, null, false);

    public static string TicketPurpose(string projectName)
        => PscbPaymentViewModelBuilder.TicketPurposePrefix + projectName;

    public static string Payment(
        string orderId,
        string? account,
        string? details,
        string? email = null,
        string? refunds = null,
        string state = "end",
        string amount = "100.0",
        string? statusDate = null)
        => $$"""{"orderId":"{{orderId}}","paymentId":"11","amount":{{amount}},"state":"{{state}}","account":{{Quote(account)}},"email":{{Quote(email)}},"details":{{Quote(details)}},"refunds":{{refunds ?? "null"}},"stateDate":{{Quote(statusDate ?? DefaultStatusDate)}}}""";

    /// <summary>
    /// Дата статуса по умолчанию: банк всегда присылает её, а <c>CurrentStatusDate</c> не
    /// допускает <c>null</c> — сериализатор на null бросает.
    /// </summary>
    private const string DefaultStatusDate = "2026-05-10T15:00:00+03:00";

    public static string Quote(string? value) => value is null ? "null" : "\"" + value + "\"";

    public static PscbFakeUserRepository Users(params int[] existingIds)
    {
        var users = new PscbFakeUserRepository();
        foreach (var id in existingIds)
        {
            users.AddExisting(id);
        }

        return users;
    }
}

internal sealed class FakeBankSecrets : IBankSecretsProvider
{
    public bool Debug => false;

    public bool DebugOutput => false;

    public string ApiEndpoint => "https://bank.example.invalid";

    public string ApiDebugEndpoint => "https://demo.bank.example.invalid";

    public string MerchantId => "47607";

    public string ApiKey => "secret-key";

    public string ApiDebugKey => "debug-secret-key";

    public string BankSystemPaymentUrl => "https://bank.example.invalid/payment/{0}";

    public string BankSystemDebugPaymentUrl => "https://demo.bank.example.invalid/payment/{0}";
}

internal sealed class StubHttpClientFactory(string[] payments) : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
        => new(new SingleResponseHandler($$"""{"payments":[{{string.Join(",", payments)}}]}"""));

    private sealed class SingleResponseHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
    }
}

internal sealed class FakeUriService : IUriService
{
    public string Get(ILinkable link) => GetUri(link).AbsoluteUri;

    public Uri GetUri(ILinkable link) => link.LinkType switch
    {
        LinkType.ResultUser => new($"https://joinrpg.test/user/{link.Identification}", UriKind.Absolute),
        LinkType.Project => new($"https://joinrpg.test/{link.ProjectId}/home", UriKind.Absolute),
        _ => throw new ArgumentOutOfRangeException(nameof(link), link.LinkType, null),
    };
}

internal sealed class FakeSearchService : ISearchService
{
    private readonly List<(int ProjectId, string Name)> projects = [];

    public List<string> Requests { get; } = [];

    public void AddProject(int projectId, string name) => projects.Add((projectId, name));

    public Task<IReadOnlyCollection<SearchResult>> SearchAsync(
        int? currentUserId,
        string searchString,
        IReadOnlyCollection<LinkType>? linkTypes = null,
        ProjectIdentification? projectId = null)
    {
        Requests.Add(searchString);

        // Как и настоящий провайдер, ищем вхождением подстроки — тест обязан показать,
        // что сервис не полагается на точное совпадение со стороны поиска.
        IReadOnlyCollection<SearchResult> found = [.. projects
            .Where(p => p.Name.Contains(searchString, StringComparison.Ordinal))
            .Select(p => new SearchResult
            {
                LinkType = LinkType.Project,
                Name = p.Name,
                Description = new MarkdownDbValue(""),
                Identification = p.ProjectId.ToString(),
                ProjectId = p.ProjectId,
                IsPublic = true,
                IsActive = true,
            })];

        return Task.FromResult(found);
    }
}

/// <summary>
/// Пользователи в базе: кто существует по id и кто находится по email. Остальные члены
/// интерфейса шлюзу не нужны.
/// </summary>
internal sealed class PscbFakeUserRepository : IUserRepository
{
    private readonly HashSet<int> existingIds = [];
    private readonly Dictionary<string, int> idsByEmail = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Запрошенные id — в том порядке, в котором их прислал сервис.
    /// </summary>
    public List<int> IdRequests { get; } = [];

    /// <summary>
    /// Email'ы, по которым сервис искал пользователя.
    /// </summary>
    public List<string> EmailRequests { get; } = [];

    public void AddExisting(int userId) => existingIds.Add(userId);

    public void AddByEmail(string email, int userId) => idsByEmail[email] = userId;

    public Task<IReadOnlyCollection<UserInfoHeader>> GetUserInfoHeaders(IReadOnlyCollection<UserIdentification> userIds)
    {
        IdRequests.AddRange(userIds.Select(id => id.Value));

        IReadOnlyCollection<UserInfoHeader> found = [.. userIds
            .Where(id => existingIds.Contains(id.Value))
            .Select(id => new UserInfoHeader(id, new UserDisplayName($"Пользователь {id.Value}", null), Avatar: null))];

        return Task.FromResult(found);
    }

    public Task<UserIdentification?> FindByEmail(string email)
    {
        EmailRequests.Add(email);
        UserIdentification? found = idsByEmail.TryGetValue(email, out var userId) ? new UserIdentification(userId) : null;
        return Task.FromResult(found);
    }

    public Task<User> GetById(int id) => throw new NotImplementedException();
    public Task<User> WithProfile(int userId) => throw new NotImplementedException();
    public Task<User> GetWithSubscribe(int userId) => throw new NotImplementedException();
    public Task<IReadOnlyCollection<UserIdentification>> GetUsersWithoutSelectedAvatar(KeySetPagination pagination) => throw new NotImplementedException();
    public Task<IReadOnlyCollection<User>> GetUsersWithAvatars(IReadOnlyCollection<UserIdentification> userIds) => throw new NotImplementedException();
    public Task<UserAvatar> LoadAvatar(AvatarIdentification userAvatarId) => throw new NotImplementedException();
    public Task<IReadOnlyCollection<UserAvatar>> LoadAvatars(IReadOnlyCollection<AvatarIdentification> avatarIds) => throw new NotImplementedException();
    public Task<UserInfo?> GetUserInfo(UserIdentification userId) => throw new NotImplementedException();
    public Task<IReadOnlyCollection<UserInfo>> GetUserInfos(IReadOnlyCollection<UserIdentification> userIds) => throw new NotImplementedException();
    public Task<IReadOnlyDictionary<UserIdentification, PhoneNumber>> GetPhoneNumbers(IReadOnlyCollection<UserIdentification> userIds) => throw new NotImplementedException();
    public Task<IReadOnlyCollection<UserInfoHeader>> GetAdminUserInfoHeaders() => throw new NotImplementedException();
    public Task<UserIdentification?> FindByVk(string vkId) => throw new NotImplementedException();
    public Task<UserIdentification?> FindByTelegram(string telegramUsername) => throw new NotImplementedException();
}

/// <summary>
/// База глазами сверки: операции и возвраты.
/// </summary>
internal sealed class FakeFinanceOperationsRepository : IFinanceOperationsRepository
{
    private readonly List<ApprovedRefund> refunds = [];
    private readonly List<MatchedOperation> operations = [];

    /// <summary>
    /// Идентификаторы операций, по которым сервис спрашивал возвраты.
    /// </summary>
    public List<int> Requests { get; } = [];

    /// <summary>
    /// Идентификаторы операций, по которым сервис спрашивал сверку.
    /// </summary>
    public List<int> OperationRequests { get; } = [];

    /// <summary>Исключение на каждый вызов — для проверки отказа обогащения списка.</summary>
    public Exception? Throw { get; set; }

    public void AddRefund(int refundedOperationId, DateTime operationDate, int refundCommentId = 9001)
        => refunds.Add(new ApprovedRefund(
            refundedOperationId,
            new FinanceOperationIdentification(42, 7, refundCommentId),
            operationDate));

    /// <summary>
    /// Операция в базе: по умолчанию проведённый онлайн-платёж, то есть то, что сверка
    /// обязана отличить от потерянного. Тип оплаты по умолчанию — №3 в проекте операции.
    /// </summary>
    public void AddOperation(
        int commentId,
        FinanceOperationState state = FinanceOperationState.Approved,
        FinanceOperationType operationType = FinanceOperationType.Online,
        int projectId = 42,
        int claimId = 7,
        PaymentTypeIdentification? paymentTypeId = null,
        DateTime? operationDate = null)
        => operations.Add(new MatchedOperation(
            new FinanceOperationIdentification(projectId, claimId, commentId),
            state,
            operationType,
            paymentTypeId ?? new PaymentTypeIdentification(projectId, 3),
            operationDate ?? new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc)));

    public Task<IReadOnlyCollection<ApprovedRefund>> GetApprovedRefunds(IReadOnlyCollection<int> refundedOperationIds)
    {
        if (Throw is not null)
        {
            throw Throw;
        }

        Requests.AddRange(refundedOperationIds);

        IReadOnlyCollection<ApprovedRefund> found = [.. refunds.Where(refund => refundedOperationIds.Contains(refund.RefundedOperationId))];
        return Task.FromResult(found);
    }

    public Task<IReadOnlyCollection<MatchedOperation>> GetOperationsByCommentIds(IReadOnlyCollection<int> commentIds)
    {
        OperationRequests.AddRange(commentIds);

        IReadOnlyCollection<MatchedOperation> found = [.. operations.Where(operation => commentIds.Contains(operation.OperationId.FinanceOperationId))];
        return Task.FromResult(found);
    }

    public Task<IReadOnlyCollection<FinanceOperationIdentification>> GetUnfinishedOperations(KeySetPagination? pagination = null)
        => throw new NotImplementedException();
}

/// <summary>
/// Метаданные проектов для вердикта восстановления: блокировка (ADR023) и типы оплаты.
/// </summary>
/// <remarks>
/// Проект, которого тест явно не завёл, отдаётся незаблокированным и без типов оплаты — так же,
/// как отсутствие записи в словаре готовности означало «нет типа онлайн-оплаты», когда вердикт
/// читал его из базы напрямую.
/// </remarks>
internal sealed class PscbFakeProjectMetadataRepository : IProjectMetadataRepository
{
    private readonly Dictionary<ProjectIdentification, ProjectInfo> projects = [];

    public Exception? Throw { get; set; }

    /// <summary>
    /// Проекты, по которым сервис спрашивал метаданные.
    /// </summary>
    public List<int> Requests { get; } = [];

    /// <summary>
    /// Проект готов принять восстановленный платёж: не заблокирован, онлайн-тип один и активен.
    /// </summary>
    public void AddReadyProject(int projectId, int onlinePaymentTypeId = 3)
        => AddProject(projectId, isBlocked: false, OnlinePaymentType(projectId, onlinePaymentTypeId));

    public void AddBlockedProject(int projectId)
        => AddProject(projectId, isBlocked: true, OnlinePaymentType(projectId, 3));

    public void AddProjectWithoutOnlinePaymentType(int projectId)
        => AddProject(projectId, isBlocked: false);

    public void AddProjectWithTwoOnlinePaymentTypes(int projectId)
        => AddProject(projectId, isBlocked: false,
            OnlinePaymentType(projectId, 3), OnlinePaymentType(projectId, 4));

    /// <summary>
    /// Онлайн-оплату в проекте с тех пор выключили (тип оплаты удаляется только мягко) —
    /// восстановлению это мешать не должно.
    /// </summary>
    public void AddProjectWithDisabledOnlinePaymentType(int projectId, int onlinePaymentTypeId = 3)
        => AddProject(projectId, isBlocked: false, OnlinePaymentType(projectId, onlinePaymentTypeId, enabled: false));

    public void AddProjectWithTwoDisabledOnlinePaymentTypes(int projectId)
        => AddProject(projectId, isBlocked: false,
            OnlinePaymentType(projectId, 3, enabled: false), OnlinePaymentType(projectId, 4, enabled: false));

    private void AddProject(int projectId, bool isBlocked, params PaymentTypeInfo[] paymentTypes)
        => projects[new ProjectIdentification(projectId)] = CreateProjectInfo(new ProjectIdentification(projectId), isBlocked, paymentTypes);

    public Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId, bool ignoreCache = false)
    {
        Requests.Add(projectId.Value);
        if (Throw is { } failure)
        {
            throw failure;
        }
        return Task.FromResult(projects.TryGetValue(projectId, out var info)
            ? info
            : CreateProjectInfo(projectId, isBlocked: false, []));
    }

    public Task<DomainTypes.ProjectMetadata.ProjectDetails> GetProjectDetails(ProjectInfo projectInfo)
        => throw new NotImplementedException();

    public void PrimeCache(ProjectInfo projectInfo) => throw new NotImplementedException();

    /// <summary>Владелец типов оплаты — метаданные несут его, вердикту он не нужен.</summary>
    private static readonly UserInfoHeader PaymentTypeOwner =
        new(new UserIdentification(1), new UserDisplayName("Мастер", null), Avatar: null);

    private static PaymentTypeInfo OnlinePaymentType(int projectId, int paymentTypeId, bool enabled = true)
        => new(
            PaymentTypeKind.Online,
            enabled,
            PaymentTypeOwner,
            new PaymentTypeIdentification(projectId, paymentTypeId),
            "Онлайн",
            IsDefault: false);

    /// <summary>
    /// Минимальный <see cref="ProjectInfo"/>: вердикт читает только статус проекта и его типы
    /// оплаты, остальное заполнено заглушками.
    /// </summary>
    private static ProjectInfo CreateProjectInfo(
        ProjectIdentification projectId,
        bool isBlocked,
        IReadOnlyCollection<PaymentTypeInfo> paymentTypes)
    {
        var rootGroupId = new CharacterGroupIdentification(projectId, 1);
        var rootGroup = new CharacterGroupInfo(
            rootGroupId,
            "Корень",
            IsActive: true,
            IsPublic: true,
            DirectChildGroupIds: [],
            ChildCharactersOrdering: "",
            DirectParentGroupIds: [],
            AllChildGroups: [],
            AllParentGroups: [],
            CharacterGroupType.Root,
            ResponsibleMasterId: null);

        return new ProjectInfo(
            projectId,
            new ProjectName($"Проект {projectId.Value}"),
            ordering: "",
            unsortedFields: [],
            new ProjectFieldSettings(NameField: null, DescriptionField: null),
            new ProjectFinanceSettings(PreferentialFeeEnabled: false, WarnOnOverPayment: false, paymentTypes, FeeSchedule: []),
            new ProjectAccommodationSettings(Enabled: false, PlayerInvitesEnabled: true, Types: [], RoomCategories: []),
            new ProjectGroupTree(rootGroupId, new Dictionary<CharacterGroupIdentification, CharacterGroupInfo> { [rootGroupId] = rootGroup }),
            masters: [],
            mastersOrdering: null,
            publishPlot: false,
            new ProjectCheckInSettings(InProgress: false, AllowSecondRoles: false, ShowCheckInBlockersToPlayers: false),
            isBlocked ? ProjectLifecycleStatus.Blocked : ProjectLifecycleStatus.ActiveClaimsOpen,
            new ProjectScheduleSettings(ScheduleEnabled: false),
            ProjectCloneSettings.CloneDisabled,
            DateOnly.MinValue,
            ProjectProfileRequirementSettings.AllNotRequired,
            new ProjectClaimSettings(DefaultTemplate: null, StrictlyOneCharacter: false, AutoAcceptClaims: false, IsAcceptingClaims: true, IsPublicProject: true),
            projectRolesLists: [],
            defaultRolesListId: null,
            TimeZoneInfo.Utc);
    }
}

internal sealed class FakeFinanceOperationUri : IUriLocator<FinanceOperationIdentification>
{
    public Uri GetUri(FinanceOperationIdentification target)
        => new($"https://joinrpg.test/{target.ProjectId.Value}/finance/{target.FinanceOperationId}", UriKind.Absolute);
}

internal sealed class FakeClaimUri : IUriLocator<ClaimIdentification>
{
    public Uri GetUri(ClaimIdentification target)
        => new($"https://joinrpg.test/{target.ProjectId.Value}/claim/{target.ClaimId}/edit", UriKind.Absolute);
}
