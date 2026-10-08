using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Services.Impl.Accommodation;
using JoinRpg.Services.Impl.Characters;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Impl.Projects.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test.Claims;

/// <summary>
/// Общая обвязка для тестов claim-контура (ADR014): <see cref="MockedProject"/>, фейковый
/// <see cref="FakeUnitOfWork"/> со счётчиком сохранений, фейки обоих каналов уведомлений
/// (уведомления по заявке — <see cref="FakeClaimNotificationService"/>, о проживании —
/// <see cref="FakeAccommodationNotificationService"/>),
/// репозиторий метаданных и фабрика текущего пользователя.
/// </summary>
// Члены, протекающие internal-типы (фейки), помечены private protected, чтобы публичный
// (для обнаружения xUnit) базовый класс не «раскрывал» их наружу сборки.
public abstract class ClaimServiceTestBase
{
    protected readonly MockedProject mock = new();
    private protected readonly FakeUnitOfWork unitOfWork;
    /// <summary>Общий журнал всех каналов рассылки — нужен, чтобы проверять их взаимный порядок.</summary>
    private readonly List<object> notificationJournal = [];

    private protected readonly FakeClaimNotificationService claimNotifications;
    private protected readonly FakeAccommodationNotificationService accommodationNotifications;

    private protected readonly FakeProjectMetadataRepository metadataRepository;

    protected ClaimServiceTestBase()
    {
        unitOfWork = new FakeUnitOfWork(mock);
        metadataRepository = new FakeProjectMetadataRepository(mock);
        claimNotifications = new FakeClaimNotificationService(notificationJournal);
        accommodationNotifications = new FakeAccommodationNotificationService(notificationJournal);
    }

    /// <summary>
    /// Собирает боевой <see cref="CharacterPropsService"/> поверх фейков — подделываются только
    /// доступ к данным и каналы уведомлений, сама проверяемая логика настоящая.
    /// </summary>
    private protected CharacterPropsService CreatePropsService(int? currentUserId = null)
        => new(
            unitOfWork,
            CreateCurrentUser(currentUserId),
            CreateFieldSaveHelper(),
            new CommentHelper(CreateCurrentUser(currentUserId)),
            claimNotifications,
            accommodationNotifications,
            NullLogger<CharacterPropsService>.Instance);

    /// <summary>
    /// Боевой <see cref="FieldSetupServiceImpl"/> поверх боевого <see cref="ProjectPropsService"/>:
    /// отметку полей использованными сервисы заявок и персонажей ставят через него.
    /// </summary>
    private protected FieldSetupServiceImpl CreateFieldSetupService(int? currentUserId = null)
        => new(new ProjectPropsService(
            unitOfWork,
            CreateCurrentUser(currentUserId),
            metadataRepository,
            NullLogger<ProjectPropsService>.Instance));

    /// <summary>
    /// Подмена пользователя, под которой идёт автоприём. Настоящая: текущий пользователь
    /// действительно меняется, поэтому автоприём проходит проверку прав как ответственный мастер.
    /// </summary>
    private protected readonly FakeImpersonateAccessor impersonateAccessor = new();

    /// <summary>
    /// Проверка ссылок на пользователей (ADR017 §7) поверх репозитория мока: существуют игрок и
    /// мастер, любой другой идентификатор считается несуществующим.
    /// </summary>
    private protected UserFieldValidator CreateUserFieldValidator() => new(new FakeUserRepository(mock));

    private protected static FieldSaveHelper CreateFieldSaveHelper()
        => new(new MockedFieldDefaultValueGenerator(), NullLogger<FieldSaveHelper>.Instance);

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
    private protected IReadOnlyList<IClaimNotification> SentNotifications => claimNotifications.Sent;

    /// <summary>Уведомления о проживании в порядке отправки.</summary>
    private protected IReadOnlyList<RoomOccupancyNotification> SentRoomNotifications
        => accommodationNotifications.RoomOccupancy;

    /// <summary>Уведомления о приглашениях к проживанию в порядке отправки.</summary>
    private protected IReadOnlyList<AccommodationInviteNotification> SentInviteNotifications
        => accommodationNotifications.Invites;

    /// <summary>
    /// Отправленное всеми каналами в общем порядке. Нужно, чтобы проверять не только факт отправки,
    /// но и то, что уведомления о проживании ушли после уведомлений по комментариям.
    /// </summary>
    protected IReadOnlyList<object> SentInOrder => notificationJournal;

    private protected FakeCurrentUserAccessor CreateCurrentUser(int? currentUserId = null, bool isAdmin = false)
        => impersonateAccessor.Track(new FakeCurrentUserAccessor(currentUserId ?? mock.Master.UserId, isAdmin));

    /// <summary>
    /// Write-репозиторий берётся строго из <see cref="IUnitOfWork"/> — как и в бою, где иначе
    /// мутация трекалась бы в одном <c>DbContext</c>, а сохранение шло в другом (ADR014).
    /// </summary>
    private protected ICharacterAggregateWriteRepository WriteRepository
        => unitOfWork.GetCharacterAggregateWriteRepository();
}
