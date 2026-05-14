namespace JoinRpg.DomainTypes.Users;

/// <summary>
/// Defines source how avatar was obtained
/// </summary>
public enum AvatarSource
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
