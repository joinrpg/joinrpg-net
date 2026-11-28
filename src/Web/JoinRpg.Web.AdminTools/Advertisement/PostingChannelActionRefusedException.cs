namespace JoinRpg.Web.AdminTools.Advertisement;

/// <summary>
/// Штатный отказ в создании канала рекламы — маппится в 400 с этим сообщением
/// (см. WebApiExceptionHandler), текст уже на русском и показывается в диалоге.
/// </summary>
public class PostingChannelActionRefusedException(string message) : Exception(message);
