using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Joinrpg.Dal.Migrate
{
    internal abstract class OneTimeOperationHostedServiceBase : IHostedService
    {
        private CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        private Task task;
        private readonly IHostApplicationLifetime applicationLifetime;
        protected readonly ILogger logger;

        public OneTimeOperationHostedServiceBase(IHostApplicationLifetime applicationLifetime, ILogger<MigrateHostService> logger)
        {
            this.applicationLifetime = applicationLifetime;
            this.logger = logger;
        }

        Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting task");
            task = Task.Run(() =>
            {
                try
                {
                    DoWork();
                }
                finally
                {
                    applicationLifetime.StopApplication();
                }
            });
            return Task.CompletedTask;
        }

        internal abstract void DoWork();

        Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            CancellationTokenSource.Cancel();
            // Defer completion promise, until our application has reported it is done.
            return task;
        }
    }
}
