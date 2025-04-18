namespace JoinRpg.PrimitiveTypes;

public record AvatarInfo(Uri Uri, int Height, int Width)
{
    public AvatarInfo(Uri uri, int size)
        : this(uri, size, size)
    {

    }

    public static AvatarInfo? FromOptional(string? uri, int size) => uri is null ? null : new AvatarInfo(new Uri(uri), size);
}
