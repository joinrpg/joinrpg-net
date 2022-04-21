namespace JoinRpg.Services.Interfaces;

public interface IMailGunConfig
{
    string ApiDomain { get; }
    string ApiKey { get; }
    string ServiceEmail { get; }
}
