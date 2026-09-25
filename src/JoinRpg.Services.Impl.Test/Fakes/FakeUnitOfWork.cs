using System.Data.Entity;
using JoinRpg.Data.Interfaces;
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
        OnSaveChanges?.Invoke(SaveChangesCallCount);
        return Task.CompletedTask;
    }

    public IProjectMetadataWriteRepository GetProjectMetadataWriteRepository()
        => new FakeProjectMetadataWriteRepository(mock);

    public ICharacterAggregateWriteRepository GetCharacterAggregateWriteRepository()
        => new FakeCharacterAggregateWriteRepository(mock);

    /// <summary>
    /// НЕ ЗАГЛУШКА, НЕ «ЧИНИТЬ». Намеренный детектор: если сервис лезет в <see cref="DbSet{TEntity}"/>
    /// напрямую, минуя write-репозиторий (ADR009/ADR014), тест обязан упасть — иначе мутация
    /// прошла бы мимо проверяемого контура и незаметно для теста.
    /// </summary>
    public DbSet<T> GetDbSet<T>() where T : class => throw new NotSupportedException();

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
