namespace JoinRpg.WebComponents;

public interface IMoveClient
{
    Task<string[]> MoveAfterAsync(string selfId, string parentId, string? moveAfterId);
}
