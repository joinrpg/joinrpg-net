namespace JoinRpg.Web.ProjectCommon.ElementMoving;

public interface IMoveClient
{
    Task<string[]> MoveAfterAsync(MoveRequest request);
}
