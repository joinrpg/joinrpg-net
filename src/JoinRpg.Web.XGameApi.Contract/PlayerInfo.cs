namespace JoinRpg.Web.XGameApi.Contract
{
    /// <summary>
    /// Player info
    /// </summary>
    public class PlayerInfo
    {
        /// <summary>
        /// Id
        /// </summary>
        public int PlayerId { get; set; }
        /// <summary>
        /// Nick name
        /// </summary>
        public string NickName { get; set; }
        /// <summary>
        /// Fulll name
        /// </summary>
        public string FullName { get; set; }
        /// <summary>
        /// Other nicks to search
        /// </summary>
        public string OtherNicks { get; set; }
    }
}
