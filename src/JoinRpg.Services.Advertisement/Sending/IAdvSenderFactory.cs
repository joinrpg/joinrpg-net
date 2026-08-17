namespace JoinRpg.Services.Advertisement.Sending;

internal interface IAdvSenderFactory
{
    /// <summary>
    /// Определяет вид канала по <paramref name="channel"/>.Settings и строит подходящего отправителя.
    /// Возвращает null, если вид канала не поддерживается или его настройки некорректны.
    /// </summary>
    ISingleHotRoleSender? Create(AdvertisementChannelInfo channel);
}
