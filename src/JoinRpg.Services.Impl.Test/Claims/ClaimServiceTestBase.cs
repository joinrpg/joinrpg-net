using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Interfaces;
using JoinRpg.Services.Impl.Characters;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Impl.Test.Projects;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test.Claims;

/// <summary>
/// Общая обвязка для тестов claim-контура (ADR014): <see cref="MockedProject"/>, фейковый
/// <see cref="FakeUnitOfWork"/> со счётчиком сохранений, фейки обоих каналов уведомлений
/// (новый — <see cref="FakeClaimNotificationService"/>, легаси-письма — <see cref="FakeEmailService"/>)
/// и фабрика текущего пользователя.
/// </summary>
// Члены, протекающие internal-типы (фейки), помечены private protected, чтобы публичный
// (для обнаружения xUnit) базовый класс не «раскрывал» их наружу сборки.
public abstract class ClaimServiceTestBase
{
    protected readonly MockedProject mock = new();
    private protected readonly FakeUnitOfWork unitOfWork;
    /// <summary>Общий журнал обоих каналов рассылки — нужен, чтобы проверять их взаимный порядок.</summary>
    private readonly List<object> notificationJournal = [];

    private protected readonly FakeClaimNotificationService claimNotifications;
    private protected readonly FakeEmailService emailService;

    private protected readonly FakeProjectMetadataRepository metadataRepository;

    protected ClaimServiceTestBase()
    {
        unitOfWork = new FakeUnitOfWork(mock);
        metadataRepository = new FakeProjectMetadataRepository(mock);
        claimNotifications = new FakeClaimNotificationService(notificationJournal);
        emailService = new FakeEmailService(notificationJournal);
    }

    /// <summary>Генератор значений по умолчанию, который ничего не генерирует.</summary>
    private sealed class NoDefaultsGenerator : IFieldDefaultValueGenerator
    {
        public string? CreateDefaultValue(Claim? claim, FieldWithValue field) => null;
        public string? CreateDefaultValue(Character? character, FieldWithValue field) => null;
    }

    /// <summary>
    /// Собирает боевой <see cref="CharacterPropsService"/> поверх фейков — подделываются только
    /// доступ к данным и каналы уведомлений, сама проверяемая логика настоящая.
    /// </summary>
    private protected CharacterPropsService CreatePropsService(int? currentUserId = null, bool isAdmin = false)
        => CreatePropsService(CreateCurrentUser(currentUserId, isAdmin));

    /// <summary>
    /// То же, но с произвольным текущим пользователем — нужно там, где проверяется поведение при
    /// <b>отсутствии</b> пользователя (платёжные колбэки приходят от банка без нашей сессии).
    /// </summary>
    private protected CharacterPropsService CreatePropsService(ICurrentUserAccessor currentUser)
        => new(
            unitOfWork,
            currentUser,
            metadataRepository,
            CreateFieldSaveHelper(),
            new CommentHelper(currentUser),
            claimNotifications,
            emailService,
            NullLogger<CharacterPropsService>.Instance);

    /// <summary>Записывает вызовы утверждения заявки, которые делает автоприём.</summary>
    private protected readonly FakeClaimApprovalService autoApprovals = new();

    /// <summary>Записывает подмену пользователя, под которой идёт автоприём.</summary>
    private protected readonly FakeImpersonateAccessor impersonateAccessor = new();

    /// <summary>
    /// Боевой автоприём поверх фейков: условия он перечитывает сам, а само утверждение уходит в
    /// <see cref="autoApprovals"/> — так видно, <b>когда</b> его позвали.
    /// </summary>
    private protected ClaimAutoApproveService CreateAutoApproveService()
        => new(
            new FakeClaimsRepository(mock),
            metadataRepository,
            new FakeUserRepository(mock),
            impersonateAccessor,
            autoApprovals,
            NullLogger<ClaimAutoApproveService>.Instance);

    private protected static FieldSaveHelper CreateFieldSaveHelper()
        => new(new NoDefaultsGenerator(), NullLogger<FieldSaveHelper>.Instance);

    protected ProjectIdentification ProjectId => mock.ProjectInfo.ProjectId;

    /// <summary>Сколько раз сервис сохранял изменения. Уведомления обязаны уходить после сохранения.</summary>
    protected int SaveChangesCallCount => unitOfWork.SaveChangesCallCount;

    /// <summary>
    /// Подписка на каждое сохранение — даёт заглянуть в граф между фазами двухфазных операций.
    /// </summary>
    protected Action<int>? OnSaveChanges
    {
        get => unitOfWork.OnSaveChanges;
        set => unitOfWork.OnSaveChanges = value;
    }

    /// <summary>Уведомления нового канала в порядке отправки.</summary>
    private protected IReadOnlyList<object> SentNotifications => claimNotifications.Sent;

    /// <summary>Письма легаси-канала в порядке отправки.</summary>
    protected IReadOnlyList<EmailModelBase> SentEmails => emailService.Sent;

    /// <summary>
    /// Отправленное обоими каналами в общем порядке: уведомления и письма легаси-канала вперемешку.
    /// Нужно, чтобы проверять не только факт отправки, но и то, что письма ушли после уведомлений.
    /// </summary>
    protected IReadOnlyList<object> SentInOrder => notificationJournal;

    private protected FakeCurrentUserAccessor CreateCurrentUser(int? currentUserId = null, bool isAdmin = false)
        => new(currentUserId ?? mock.Master.UserId, isAdmin);

    /// <summary>
    /// Write-репозиторий берётся строго из <see cref="IUnitOfWork"/> — как и в бою, где иначе
    /// мутация трекалась бы в одном <c>DbContext</c>, а сохранение шло в другом (ADR014).
    /// </summary>
    private protected ICharacterAggregateWriteRepository WriteRepository
        => unitOfWork.GetCharacterAggregateWriteRepository();
}
