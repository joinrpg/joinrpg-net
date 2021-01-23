namespace JoinRpg.DataModel.Users
{
#nullable enable
    /// <summary>
    /// Avatar for user
    /// </summary>
    public class UserAvatar
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int UserAvatarId { get; set; }

        /// <summary>
        /// <see cref="User"/>
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// <see cref="User"/>
        /// </summary>
        public virtual User User { get; set; }

        /// <summary>
        /// Defines source how avatar was obtained
        /// </summary>
        public enum Source
        {
            /// <summary>
            /// https://gravatar.com/
            /// </summary>
            GrAvatar,
            /// <summary>
            /// Using social network provider
            /// </summary>
            SocialNetwork,
        }

        /// <summary>
        /// How avatar was obtained
        /// </summary>
        public Source AvatarSource { get; set; }

        /// <summary>
        /// What provider is it? Google, VKontakte, etc
        /// </summary>
        public string? ProviderId { get; set; }

        /// <summary>
        /// Provider-specified key
        /// </summary>
        public string Uri { get; set; } = null!;

        /// <summary>
        /// Is active or deleted
        /// </summary>
        public bool IsActive { get; set; }
    }
}
