namespace JoinRpg.Interfaces;

public record KeySetPagination(int? From, int PageSize = 1000)
{
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

