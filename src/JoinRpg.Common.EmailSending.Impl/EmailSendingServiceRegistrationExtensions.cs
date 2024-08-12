using JoinRpg.Interfaces.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JoinRpg.Common.EmailSending.Impl;
public static class EmailSendingServiceRegistrationExtensions
{
    public static IServiceCollection AddJoinEmailSendingService(this IServiceCollection services)
    {
        _ = services.AddOptionsWithValidateOnStart<MailGunOptions>("MailGun");

        return services
            .AddSingleton<IValidateOptions<MailGunOptions>, MailGunOptionsValidator>()
            .AddScoped<MailGunEmailSendingService>()
            .AddScoped<StubEmailSendingService>()
            .AddScoped<IEmailSendingService>(CreateEmailSendingService);

    }
    private static IEmailSendingService CreateEmailSendingService(this IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<MailGunOptions>>();
        if (options.Value.Enabled)
        {
            return sp.GetRequiredService<MailGunEmailSendingService>();
        }
        return sp.GetRequiredService<StubEmailSendingService>();
    }

    private class MailGunOptionsValidator : IValidateOptions<MailGunOptions>
    {
        ValidateOptionsResult IValidateOptions<MailGunOptions>.Validate(string? name, MailGunOptions options)
        {
            if (!options.Enabled)
            {
                return ValidateOptionsResult.Success;
            }

            return new DataAnnotationValidateOptions<MailGunOptions>(null).Validate(name, options);
        }
    }
}
