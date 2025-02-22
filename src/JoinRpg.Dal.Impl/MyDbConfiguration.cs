using System.Data.Entity;

namespace JoinRpg.Dal.Impl;

public class MyDbConfiguration : DbConfiguration
{
    public MyDbConfiguration()
    {
        SetDatabaseLogFormatter(
            (context, writeAction) => new EF6LoggerToMSExtLogging(context, writeAction));
    }
}
