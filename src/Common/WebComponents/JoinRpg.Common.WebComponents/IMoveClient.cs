namespace JoinRpg.Common.WebComponents;

public interface IMoveClient
{
    Task<string[]> MoveAfterAsync(string selfId, string parentId, string? moveAfterId);
}
