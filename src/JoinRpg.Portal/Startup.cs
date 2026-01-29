using System.Globalization;
using Autofac;
using Joinrpg.AspNetCore.Helpers;
using JoinRpg.BlobStorage;
using JoinRpg.Common.BastiliaRatingClient;
using JoinRpg.Common.EmailSending.Impl;
using JoinRpg.Common.KogdaIgraClient;
using JoinRpg.Common.Telegram;
using JoinRpg.Common.WebInfrastructure;
using JoinRpg.Common.WebInfrastructure.Logging.Filters;
using JoinRpg.Dal.Impl;
using JoinRpg.Dal.Notifications;
using JoinRpg.Domain;
using JoinRpg.Integrations.KogdaIgra;
using JoinRpg.Interfaces;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.Portal.Infrastructure.DailyJobs;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using JoinRpg.Portal.Infrastructure.HealthChecks;
using JoinRpg.Portal.Infrastructure.Logging;
using JoinRpg.Portal.Infrastructure.Logging.Filters;
using JoinRpg.Portal.Menu;
using JoinRpg.Services.Email;
using JoinRpg.Services.Export;
using JoinRpg.Services.Impl;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Notifications;
using JoinRpg.WebPortal.Managers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal;

public class Startup(IConfiguration configuration, IWebHostEnvironment environment)
{
    public IConfiguration Configuration { get; } = configuration;

    public void ConfigureServices(IJoinServiceCollection services)
    {
        services.AddJoinOpenTelemetry("JoinRpg", BackgroundServiceActivity.ActivitySourceName);

        _ = services.Configure<RecaptchaOptions>(Configuration.GetSection("Recaptcha"))
            .Configure<S3StorageOptions>(Configuration.GetSection("S3BlobStorage"))
            .Configure<JwtSecretOptions>(Configuration.GetSection("Jwt"))
            .Configure<JwtBearerOptions>(Configuration.GetSection("Jwt"))
            .Configure<NotificationsOptions>(Configuration.GetSection("Notifications"))
            .Configure<JoinRpgHostNamesOptions>(Configuration.GetSection("JoinRpgHostNames"))
            .Configure<MailGunOptions>(Configuration.GetSection("MailGun"))
            .Configure<TelegramLoginOptions>(Configuration.GetSection("Telegram"))
            .Configure<DonateOptions>(Configuration.GetSection("Donate"))
            .Configure<KogdaIgraOptions>(Configuration.GetSection("KogdaIgra"));

        services.AddOptions<PostboxOptions>().BindConfiguration("Postbox");
        services.AddOptions<BastiliaRatingOptions>().BindConfiguration("BastiliaRating");

        services
            .AddKogdaIgra()
            .AddBastiliaRatingClient()
            ;

        _ = services.AddRouting(options => options.LowercaseUrls = true);
        var mvc = services
            .AddMvc(options =>
            {
                if (!environment.IsEnvironment("IntegrationTest"))
                {
                    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                }
                options.Filters.Add<RedirectAntiforgeryValidationFailedResultFilter>();
                options.Filters.Add<SerilogMvcFilter>();
                options.Filters.Add<SerilogRazorPagesFilter>();
                options.Filters.Add<SerilogProjectMvcFilter>();
                options.Filters.Add<SerilogProjectRazorPagesFilter>();
                options.Filters.Add(new SetIsProductionFilterAttribute());
                options.Filters.Add(new AddFullUriFilter());

                //TODO need to fix this
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            })
            .AddControllersAsServices()
            .AddViewComponentsAsServices();

        services.AddRazorPages(options =>
        {
            options.Conventions.ConfigureFilter(new DiscoverProjectPageFilterAttribute());
            options.Conventions.ConfigureFilter(new SetIsProductionFilterAttribute());
            options.Conventions.ConfigureFilter(new RedirectAntiforgeryValidationFailedResultFilter());
        });

        services.AddJoinDataProtection(Configuration, environment);

        services.AddJoinDailyJob(Configuration, environment);
        services.AddNotificationsDal(Configuration, environment);

        services.AddJoinDomainServices();

        if (environment.IsDevelopment())
        {
            services.AddDatabaseDeveloperPageExceptionFilter();
        }

        _ = services.AddJoinAuth(
            Configuration.GetSection("Jwt").Get<JwtSecretOptions>(),
            environment,
            Configuration.GetSection("Authentication"));

        _ = services.AddSwaggerGen(Swagger.ConfigureSwagger);

        var healthChecks = services.AddHealthChecks()
            .AddSqlServer(
                Configuration["ConnectionStrings:DefaultConnection"],
                name: "main-sqldb",
                tags: ["ready"])

            .AddCheck<HealthCheckLoadProjects>("Project load", tags: ["ready"]);

        services.AddJoinEmailSendingService();
        services.ConfigureForwardedHeaders();

        _ = services.AddTransient<YandexLogLink>();

        _ = services.AddUserServicesOnly();

        _ = services
            .AddJoinDal()
            .AddJoinExportService()
            .AddJoinManagers()
            .AddJoinNotificationServices()
            .AddJoinNotificationLayerServices()
            .AddJoinTelegram()
            .AddJoinBlobStorage();

        services.AddOptions<DonateOptions>();

        services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
    }

    /// <summary>
    /// Runs after ConfigureServices
    /// </summary>
    public void ConfigureContainer(ContainerBuilder builder)
    {
        _ = builder
            .RegisterModule(new JoinRpgDomainModule())
            .RegisterModule(new JoinRpgPortalModule());
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(WebApplication app, IWebHostEnvironment env)
    {

        app.MapStaticAssets().ShortCircuit();
        app.UseOpenTelemetryPrometheusScrapingEndpoint();

        _ = app.UseForwardedHeaders();

        _ = app.UseRequestLocalization(options =>
          {
              options.DefaultRequestCulture = new RequestCulture("ru-RU");

              //TODO before adding other cultures, ensure that datetime fields send correct format
              options.SupportedCultures = [new CultureInfo("ru-RU")];
          });

        _ = app.UseExceptionHandler("/error");
        _ = app.UseStatusCodePagesWithReExecute("/error/{0}");

        _ = app.UseJoinRequestLogging();

        _ = app
            .UseSwagger(Swagger.Configure)
            .UseSwaggerUI(Swagger.ConfigureUI);

        app.MapJoinHealthChecks();

        _ = app.UseMiddleware<DiscoverProjectMiddleware>();

        _ = app.UseAuthentication();
        _ = app.UseAuthorization()
            .UseMiddleware<CsrfTokenCookieMiddleware>();

        _ = app.MapRazorComponents<JoinRpg.Blazor.Client.Components.App>().AddInteractiveWebAssemblyRenderMode();

        _ = app.MapControllers().WithStaticAssets();
        _ = app.MapAreaControllerRoute("Admin_default", "Admin", "Admin/{controller}/{action=Index}/{id?}");
        _ = app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
        _ = app.MapRazorPages().WithStaticAssets();
    }
}
