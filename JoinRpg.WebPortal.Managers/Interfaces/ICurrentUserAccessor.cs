namespace JoinRpg.WebPortal.Managers.Interfaces
{
    public interface ICurrentUserAccessor
    {
        int? UserIdOrDefault { get; }
        int UserId { get; }
    }
}
