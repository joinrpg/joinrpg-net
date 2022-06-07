using System.Globalization;
using Autofac;
using JoinRpg.BlobStorage;
using JoinRpg.DI;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using JoinRpg.Portal.Infrastructure.HealthChecks;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JoinRpg.Portal;

public class Startup
{
    private readonly IWebHostEnvironment environment;
    private S3StorageOptions s3StorageOptions;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        this.environment = environment;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        _ = services.Configure<RecaptchaOptions>(Configuration.GetSection("Recaptcha"))
            .Configure<S3StorageOptions>(Configuration.GetSection("S3BlobStorage"))
            .Configure<JwtSecretOptions>(Configuration.GetSection("Jwt"))
            .Configure<JwtBearerOptions>(Configuration.GetSection("Jwt"));

        s3StorageOptions = Configuration.GetSection("S3BlobStorage").Get<S3StorageOptions>();

        _ = services.AddLogging();

        _ = services.AddHttpContextAccessor();
        services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();

        _ = services.AddHttpClient();

        _ = services.AddRouting(options => options.LowercaseUrls = true);
        var mvc = services
            .AddMvc(options =>
            {
                if (!environment.IsEnvironment("IntegrationTest"))
                {
                    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                }
                options.Filters.Add(new SetIsProductionFilterAttribute());
                options.Filters.Add(new TypeFilterAttribute(typeof(SetUserDataFilterAttribute)));

                //TODO need to fix this
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            })
            .AddControllersAsServices()
            .AddViewComponentsAsServices();


        var dataProtection = services.AddDataProtection();

        if (!environment.IsDevelopment())
        {
            var dataProtectionConnectionString = Configuration.GetConnectionString("DataProtection");
            if (!string.IsNullOrWhiteSpace(dataProtectionConnectionString))
            {
                services.AddDbContext<DataProtectionDbContext>(
                    options =>
                    {
                        options.UseNpgsql(dataProtectionConnectionString);
                        options.EnableSensitiveDataLogging(environment.IsDevelopment());
                        options.EnableDetailedErrors(environment.IsDevelopment());
                    });

                services.AddDatabaseDeveloperPageExceptionFilter();
                services
                    .AddHealthChecks()
                    .AddNpgSql(
                        Configuration["ConnectionStrings:DataProtection"],
                        name: "dataprotection-db",
                        failureStatus: HealthStatus.Degraded);
                dataProtection.PersistKeysToDbContext<DataProtectionDbContext>();
            }
        }

        if (environment.IsDevelopment())
        {
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
                tags: new[] { "ready" })

            .AddCheck<HealthCheckLoadProjects>("Project load", tags: new[] { "ready" });

        if (s3StorageOptions.Configured)
        {
            healthChecks.AddCheck<HealthCheckS3Storage>("S3 storage");
        }

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
    }




    /// <summary>
    /// Runs after ConfigureServices
    /// </summary>
    public void ConfigureContainer(ContainerBuilder builder)
    {
        _ = builder.RegisterModule(new JoinrpgMainModule())
            .RegisterModule(new JoinRpgPortalModule())
            .RegisterModule(new BlobStorageModule(s3StorageOptions));
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        _ = app.UseForwardedHeaders();

        _ = app.UseRequestLocalization(options =>
          {
              options.DefaultRequestCulture = new RequestCulture("ru-RU");

              //TODO before adding other cultures, ensure that datetime fields send correct format
              options.SupportedCultures = new CultureInfo[] { new CultureInfo("ru-RU") };
          });

        if (env.IsDevelopment())
        {
            _ = app.UseDeveloperExceptionPage();
            _ = app.UseMigrationsEndPoint();
        }
        else if (env.IsEnvironment("IntegrationTest"))
        {
            //need this to ensure that exceptions from controller will fall directly to integration test
        }
        else
        {
            _ = app.UseExceptionHandler("/error");
        }

        _ = app.Use(async (context, next) =>
          {
              await next();
              if (context.Response.StatusCode == 404)
              {
                  context.Request.Path = "/error/404";
                  await next();
              }
          });

        _ = app
            .UseSwagger(Swagger.Configure)
            .UseSwaggerUI(Swagger.ConfigureUI);

        if (!env.IsDevelopment())
        {
            _ = app.UseHttpsRedirection();
        }

        _ = app.UseStaticFiles()
               .UseBlazorFrameworkFiles();

        _ = app.UseRouting();

        _ = app.UseMiddleware<DiscoverProjectMiddleware>();

        _ = app.UseAuthentication();
        _ = app.UseAuthorization()
            .UseMiddleware<CsrfTokenCookieMiddleware>();

        _ = app.UseEndpoints(endpoints =>
          {
              endpoints.MapJoinHealthChecks();

              _ = endpoints.MapControllers();
              _ = endpoints.MapAreaControllerRoute("Admin_default", "Admin", "Admin/{controller}/{action=Index}/{id?}");
              _ = endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
          });
    }
}
