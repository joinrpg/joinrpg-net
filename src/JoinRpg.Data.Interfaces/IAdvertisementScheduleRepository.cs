namespace JoinRpg.Data.Interfaces;

public interface IAdvertisementScheduleRepository
{
    /// <summary>Расписания, которые фактически активны (см. <see cref="AdvertisementScheduleInfo.IsEffectivelyActive"/>) — для рассылки.</summary>
    Task<IReadOnlyCollection<AdvertisementScheduleInfo>> GetActiveSchedules();

    /// <summary>Все расписания независимо от активности — для админского UI.</summary>
    Task<IReadOnlyCollection<AdvertisementScheduleInfo>> GetAllSchedules();
}
