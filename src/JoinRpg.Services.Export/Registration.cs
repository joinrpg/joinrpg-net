using JoinRpg.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Services.Export;

public static class Registration
{
    public static IServiceCollection AddJoinExportService(this IServiceCollection services)
    {
        return services.AddSingleton<IExportDataService, ExportDataServiceImpl>();
    }
}
