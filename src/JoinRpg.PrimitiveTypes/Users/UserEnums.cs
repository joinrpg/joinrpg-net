namespace JoinRpg.PrimitiveTypes.Users;

public enum UserProfileAccessReason
{
    NoAccess,
    ItsMe,
    Master,
    CoMaster,
    Administrator,
}

public enum ContactsAccessType : byte
{
    OnlyForMasters = 0,
    Public = 1,
}
