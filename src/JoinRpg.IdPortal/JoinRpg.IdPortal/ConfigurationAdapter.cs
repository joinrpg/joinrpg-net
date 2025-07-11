namespace JoinRpg.IdPortal;

public class ConfigurationAdapter(IConfiguration configuration) : Dal.Impl.IJoinDbContextConfiguration
{
    public string ConnectionString => configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}
