namespace JoinRpg.Common.BastiliaRatingClient;

public interface IBastiliaRatingClient
{
    Task<int[]> GetBastiliaActualMembers();
    Task UpdateKogdaIgra(int gameId);
}
