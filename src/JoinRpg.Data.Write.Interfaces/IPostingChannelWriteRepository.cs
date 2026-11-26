using JoinRpg.DomainTypes.Posting;

namespace JoinRpg.Data.Write.Interfaces;

public interface IPostingChannelWriteRepository
{
    /// <summary>
    /// Добавляет канал рекламы в контекст. Сохранение — <see cref="IUnitOfWork.SaveChangesAsync"/>
    /// вызывающим: до сохранения id сущности не существует, поэтому наружу он не возвращается.
    /// </summary>
    void CreateChannel(string name, TelegramChannelSettings settings);
}
