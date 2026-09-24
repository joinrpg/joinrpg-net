using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Services.Impl.Characters;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test.Claims;

/// <summary>
/// Общая обвязка для тестов claim-контура (ADR014): <see cref="MockedProject"/>, фейковый
/// <see cref="FakeUnitOfWork"/> со счётчиком сохранений, фейки обоих каналов уведомлений
/// (новый — <see cref="FakeClaimNotificationService"/>, легаси-письма — <see cref="FakeEmailService"/>),
/// репозиторий метаданных и фабрика текущего пользователя.
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

    /// <summary>
    /// Собирает боевой <see cref="CharacterPropsService"/> поверх фейков — подделываются только
    /// доступ к данным и каналы уведомлений, сама проверяемая логика настоящая.
    /// </summary>
    private protected CharacterPropsService CreatePropsService(int? currentUserId = null)
        => new(
            unitOfWork,
            CreateCurrentUser(currentUserId),
            metadataRepository,
            CreateFieldSaveHelper(),
            new CommentHelper(CreateCurrentUser(currentUserId)),
            claimNotifications,
            emailService,
            NullLogger<CharacterPropsService>.Instance);

    private protected static FieldSaveHelper CreateFieldSaveHelper()
        => new(new MockedFieldDefaultValueGenerator(), NullLogger<FieldSaveHelper>.Instance);

    protected ProjectIdentification ProjectId => mock.ProjectInfo.ProjectId;

    /// <summary>Сколько раз сервис сохранял изменения. Уведомления обязаны уходить после сохранения.</summary>
    protected int SaveChangesCallCount => unitOfWork.SaveChangesCallCount;

    /// <summary>Уведомления нового канала в порядке отправки.</summary>
    private protected IReadOnlyList<IClaimNotification> SentNotifications => claimNotifications.Sent;

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
