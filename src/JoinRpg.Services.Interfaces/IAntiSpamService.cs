namespace JoinRpg.Services.Interfaces
{
    /// <summary>
    /// Joinrpg helps to battle spam
    /// </summary>
    public interface IAntiSpamService
    {
        /// <summary>
        /// Is user a larper? Yes, No, DontKnow
        /// </summary>
        Task<bool> IsLarper(IsLarperRequest request);
    }

    /// <summary>
    /// Request model
    /// </summary>
    public class IsLarperRequest
    {
        /// <summary>
        /// Email for user
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Vk id (https://vk.com/id[001])
        /// </summary>
        public string VkId { get; set; }

        /// <summary>
        /// Vk nickname (https://vk.com/[user])
        /// </summary>
        public string VkNickName { get; set; }

        /// <summary>
        /// Telegram nickname @[username]
        /// </summary>
        public string TelegramNickName { get; set; }
    }
}
