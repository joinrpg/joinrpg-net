using System.Data.Entity;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.Data.Interfaces.AdminTools;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Interfaces.Finances;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Services.Impl.Test.Projects;

namespace JoinRpg.Services.Impl.Test.Fakes;

internal sealed class FakeUnitOfWork(MockedProject mock) : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    /// <summary>
    /// Вызывается на каждом сохранении с его номером (1-based). Нужен там, где важен не только
    /// счётчик, но и что именно сервис успел сделать к этому моменту: создание заявки обязано
    /// сохранить её раньше, чем создаст комментарий, — иначе комментарий уедет в дискуссию с
    /// <c>CommentDiscussionId == -1</c>.
    /// </summary>
    public Action<int>? OnSaveChanges { get; set; }

    public Task SaveChangesAsync()
    {
        SaveChangesCallCount++;
        // EF6 вызывает DetectChanges до записи — обратные коллекции синхронизируются здесь же.
        FakeCharacterAggregateWriteRepository.DetectAccommodationChanges(mock);
        OnSaveChanges?.Invoke(SaveChangesCallCount);
        return Task.CompletedTask;
    }

    public IProjectMetadataWriteRepository GetProjectMetadataWriteRepository()
        => new FakeProjectMetadataWriteRepository(mock);

    public ICharacterAggregateWriteRepository GetCharacterAggregateWriteRepository()
        => new FakeCharacterAggregateWriteRepository(mock);

    public IRoomCategoryPlanWriteRepository GetRoomCategoryPlanWriteRepository()
        => new FakeRoomCategoryPlanWriteRepository(mock);

    private readonly Dictionary<Type, object> dbSets = [];

    /// <summary>
    /// Разрешает конкретному тесту читать и писать <see cref="DbSet{TEntity}"/> этого типа поверх
    /// коллекции мока. Нужно для сервисов, которые в write-репозитории ещё не переехали (ADR009/ADR014;
    /// сейчас это, например, <c>PaymentsService</c> и <c>PlotServiceImpl</c>): подключать набор
    /// приходится явно, чтобы у всех остальных <see cref="GetDbSet{T}"/> продолжал падать
    /// (см. его описание).
    /// </summary>
    public void UseDbSet<T>(ICollection<T> data) where T : class
        => dbSets[typeof(T)] = new FakeDbSet<T>(data);

    /// <summary>
    /// НЕ ЗАГЛУШКА, НЕ «ЧИНИТЬ». Намеренный детектор: если сервис лезет в <see cref="DbSet{TEntity}"/>
    /// напрямую, минуя write-репозиторий (ADR009/ADR014), тест обязан упасть — иначе мутация
    /// прошла бы мимо проверяемого контура и незаметно для теста. Исключение — набор, который тест
    /// подключил сам через <see cref="UseDbSet{T}"/>.
    /// </summary>
    public DbSet<T> GetDbSet<T>() where T : class
        => dbSets.TryGetValue(typeof(T), out var dbSet)
            ? (DbSet<T>)dbSet
            : throw new NotSupportedException(
                $"Обращение к DbSet<{typeof(T).Name}> не предусмотрено тестом. Если так и задумано — подключи набор через UseDbSet.");

    public IUserRepository GetUsersRepository() => new FakeUserRepository(mock);
    public IProjectRepository GetProjectRepository() => throw new NotSupportedException();
    public IClaimsRepository GetClaimsRepository() => new FakeClaimsRepository(mock);
    public IPlotRepository GetPlotRepository() => throw new NotSupportedException();
    public IForumRepository GetForumRepository() => throw new NotSupportedException();
    public ICharacterRepository GetCharactersRepository() => throw new NotSupportedException();
    public IAccommodationRepository GetAccomodationRepository() => throw new NotSupportedException();
    public IKogdaIgraRepository GetKogdaIgraRepository() => throw new NotSupportedException();
    public IFinanceOperationsRepository GetFinanceOperationsRepositoryRepository() => throw new NotSupportedException();

    public void Dispose() { }

    public Task<int> ExecuteSqlCommandAsync(string sql) => throw new NotImplementedException();
}
