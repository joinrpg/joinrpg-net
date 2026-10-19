namespace JoinRpg.Interfaces;

public enum KeySetDirection
{
    Ascending,
    Descending,
}

/// <summary>
/// Постраничная выборка по ключу (keyset).
/// </summary>
/// <param name="From">Ключ, с которого продолжать, сам он не включается: при <see cref="KeySetDirection.Ascending"/> — ключи больше него,
/// при <see cref="KeySetDirection.Descending"/> — меньше. null — с начала (с наименьшего или с наибольшего ключа).</param>
/// <param name="PageSize">Сколько записей вернуть.</param>
/// <param name="Direction">В каком порядке идти по ключу.</param>
public record KeySetPagination(int? From, int PageSize = 1000, KeySetDirection Direction = KeySetDirection.Ascending)
{
    /// <summary>
    /// Новые (с большим ключом) сверху; <paramref name="before"/> — только ключи меньше него, для страницы «Ранее».
    /// </summary>
    public static KeySetPagination Descending(int? before, int pageSize) => new(before, pageSize, KeySetDirection.Descending);

    public int PageSize { get; } = ValidatePageSize(PageSize);

    private static int ValidatePageSize(int pageSize)
    {
        if (pageSize is <= 0 or > 10000)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 10000");
        }
        return pageSize;
    }

    public KeySetPagination() : this(null, 1000)
    {

    }
}

