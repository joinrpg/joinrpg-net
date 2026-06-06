namespace JoinRpg.Common.PrimitiveTypes;

public interface IUriLocator<T>
{
    Uri GetUri(T target);
}
