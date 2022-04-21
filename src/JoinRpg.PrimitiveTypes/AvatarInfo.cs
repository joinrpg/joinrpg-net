namespace JoinRpg.PrimitiveTypes;

public record AvatarInfo(Uri Uri, int Height, int Width)
{
    public AvatarInfo(Uri uri, int size)
        : this(uri, size, size)
    {

    }
}
