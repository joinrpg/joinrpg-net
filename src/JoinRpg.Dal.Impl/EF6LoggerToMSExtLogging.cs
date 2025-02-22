using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Dal.Impl;

public class EF6LoggerToMSExtLogging(DbContext context, Action<string> writeAction) : DatabaseLogFormatter(context, writeAction)
{
    private static readonly Meter meter = new("JoinRpg");
    private static readonly Counter<int> lazyLoadCounter = meter.CreateCounter<int>("joinrpg.mydbcontext.lazy_loads");

    public override void LogCommand<TResult>(
        DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext)
    {
        if (TryGetLogger(interceptionContext) is ILogger logger)
        {
            var sql = command.CommandText;
            logger.LogDebug("SQL started: {sql}", sql);
            if (command.Parameters.Count == 1 && command.Parameters[0].ParameterName == "EntityKeyValue1")
            {
                lazyLoadCounter.Add(1);
                var tableName = TryGetTableNameFromSql(sql) ?? "!unknown_table";
                logger.LogWarning("SQL: Probably lazy load from '{tableName}': {sql}", tableName, sql);
            }
        }
        else
        {
            base.LogCommand(command, interceptionContext);
        }
    }

    public override void LogResult<TResult>(
        DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext)
    {
        if (TryGetLogger(interceptionContext) is ILogger logger)
        {
            if (interceptionContext.Exception != null)
            {
                logger.LogError(interceptionContext.Exception, "Command execution failed.");
            }
            else
            {
                logger.LogDebug("SQL completed: {sql}", command.CommandText);
            }
        }
        else
        {
            base.LogCommand(command, interceptionContext);
        }
    }

    private static ILogger? TryGetLogger<TResult>(DbCommandInterceptionContext<TResult> interceptionContext)
    => interceptionContext.DbContexts.First() is MyDbContext myDbContext && myDbContext.Logger is ILogger logger ? logger : null;

    private static string? TryGetTableNameFromSql(string sql)
    {
        const string fromString = "FROM ";
        var idx = sql.LastIndexOf(fromString);
        if (idx > 0)
        {
            idx += fromString.Length;
            var tail = sql.AsSpan()[idx..];
            idx = tail.IndexOf(" ");
            if (idx > 0)
            {
                return tail[..idx].ToString();
            }
        }

        return null;
    }
}
