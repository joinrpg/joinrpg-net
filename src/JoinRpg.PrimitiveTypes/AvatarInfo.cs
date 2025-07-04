namespace JoinRpg.PrimitiveTypes;

public record AvatarInfo(Uri Uri, int Size = 64)
{
    public static AvatarInfo? FromOptional(string? uri, int size = 64) => uri is null ? null : new AvatarInfo(new Uri(uri), size);
}
