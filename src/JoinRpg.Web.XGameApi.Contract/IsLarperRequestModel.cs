namespace JoinRpg.Web.XGameApi.Contract
{
    /// <summary>
    /// Request "Is X valid Larper"?
    /// Useful for implementing anti-spambots.
    /// You can specify any number of values.
    /// </summary>
    public class IsLarperRequestModel
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
