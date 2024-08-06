namespace JoinRpg.Interfaces.Email;

public interface IMailGunConfig
{
    string ApiDomain { get; }
    string ApiKey { get; }
    string ServiceEmail { get; }
}
