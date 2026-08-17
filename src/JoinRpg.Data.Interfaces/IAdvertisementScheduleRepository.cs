namespace JoinRpg.Data.Interfaces;

public interface IAdvertisementScheduleRepository
{
    Task<IReadOnlyCollection<AdvertisementScheduleInfo>> GetActiveSchedules();
}
