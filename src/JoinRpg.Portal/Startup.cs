using System.Globalization;
using Autofac;
using JoinRpg.BlobStorage;
using JoinRpg.Common.EmailSending.Impl;
using JoinRpg.Dal.Impl;
using JoinRpg.DI;
using JoinRpg.Domain;
using JoinRpg.Integrations.KogdaIgra;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.Portal.Infrastructure.Authentication.Telegram;
using JoinRpg.Portal.Infrastructure.DailyJobs;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using JoinRpg.Portal.Infrastructure.HealthChecks;
using JoinRpg.Portal.Infrastructure.Logging;
using JoinRpg.Portal.Infrastructure.Logging.Filters;
using JoinRpg.Services.Export;
using JoinRpg.Services.Impl;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Integrations.KogdaIgra;
using JoinRpg.WebPortal.Managers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace JoinRpg.Portal;

public class Startup(IConfiguration configuration, IWebHostEnvironment environment)
{
    public IConfiguration Configuration { get; } = configuration;

    public void ConfigureServices(IJoinServiceCollection services)
    {
        services.AddJoinOpenTelemetry();

        _ = services.Configure<RecaptchaOptions>(Configuration.GetSection("Recaptcha"))
            .Configure<S3StorageOptions>(Configuration.GetSection("S3BlobStorage"))
            .Configure<JwtSecretOptions>(Configuration.GetSection("Jwt"))
            .Configure<JwtBearerOptions>(Configuration.GetSection("Jwt"))
            .Configure<NotificationsOptions>(Configuration.GetSection("Notifications"))
            .Configure<MailGunOptions>(Configuration.GetSection("MailGun"))
            .Configure<DailyJobOptions>(Configuration.GetSection("DailyJob"))
            .Configure<TelegramLoginOptions>(Configuration.GetSection("Telegram"))
            .Configure<KogdaIgraOptions>(Configuration.GetSection("KogdaIgra"));

        services.AddKogdaIgra();

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

        if (environment.IsDevelopment())
        {
            services.AddDatabaseDeveloperPageExceptionFilter();

            //That's make local debug more easy
            _ = mvc.AddRazorRuntimeCompilation();
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

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
            options.KnownProxies.Clear();
            options.KnownNetworks.Clear();
            options.ForwardLimit = 1;
            // Allow nearest proxy server to set X-Forwarded-?? header
            // Do not white-list servers (It's hard to know them specifically proxy server in cloud)
            // It will allow IP-spoofing, if Kestrel is directly exposed to end user
            // But it should never happen anyway (we always should be under at least one proxy)
        });


        _ = services.AddTransient<YandexLogLink>();

        _ = services.AddUserServicesOnly();

        _ = services
            .AddJoinDal()
            .AddJoinExportService()
            .AddJoinManagers()
            .AddJoinBlobStorage();
    }

    /// <summary>
    /// Runs after ConfigureServices
    /// </summary>
    public void ConfigureContainer(ContainerBuilder builder)
    {
        _ = builder.RegisterModule(new JoinrpgMainModule())
            .RegisterModule(new JoinRpgDomainModule())
            .RegisterModule(new JoinRpgPortalModule());
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(WebApplication app, IWebHostEnvironment env)
    {
        app.MapStaticAssets();
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

        _ = app.UseSerilogRequestLogging(opts =>
        {
            opts.EnrichDiagnosticContext = SerilogWebRequestHelper.EnrichFromRequest;
            opts.GetLevel = SerilogWebRequestHelper.ExcludeHealthChecks;
        });

        _ = app
            .UseSwagger(Swagger.Configure)
            .UseSwaggerUI(Swagger.ConfigureUI);

        app.MapJoinHealthChecks();

        if (!env.IsDevelopment())
        {
            _ = app.UseHttpsRedirection();
        }

        _ = app.UseMiddleware<DiscoverProjectMiddleware>();

        _ = app.UseAuthentication();
        _ = app.UseAuthorization()
            .UseMiddleware<CsrfTokenCookieMiddleware>();

        _ = app.MapControllers().WithStaticAssets();
        _ = app.MapAreaControllerRoute("Admin_default", "Admin", "Admin/{controller}/{action=Index}/{id?}");
        _ = app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
        _ = app.MapRazorPages().WithStaticAssets();
    }
}
