namespace JoinRpg.XGameApi.Contract;

/// <param name="PlayerId"> Id </param>
/// <param name="NickName"> Nick name </param>
/// <param name="FullName"> Fulll name </param>
/// <param name="AvatarUrl"></param>
/// <param name="PlayerContacts"></param>
public record PlayerInfo(int PlayerId, string NickName, string FullName, string AvatarUrl, PlayerContacts PlayerContacts);
