namespace JoinRpg.WebComponents;
public interface IUriLocator<T>
{
    Uri GetUri(T target);
}
