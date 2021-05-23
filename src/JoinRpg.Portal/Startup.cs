using System.Globalization;
using Autofac;
using Joinrpg.Web.Identity;
using JoinRpg.BlobStorage;
using JoinRpg.DI;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using JoinRpg.Portal.Infrastructure.HealthChecks;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace JoinRpg.Portal
{
    public class Startup
    {
        private readonly IWebHostEnvironment environment;
        private BlobStorageOptions blobStorageOptions;

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
                .Configure<LetsEncryptOptions>(Configuration.GetSection("LetsEncrypt"))
                .Configure<BlobStorageOptions>(Configuration.GetSection("AzureBlobStorage"));

            blobStorageOptions = Configuration.GetSection("AzureBlobStorage").Get<BlobStorageOptions>();

            _ = services.Configure<PasswordHasherOptions>(options => options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2);

            _ = services
                .AddIdentity<JoinIdentityUser, string>(options => options.Password.ConfigureValidation())
                .AddDefaultTokenProviders()
                .AddUserStore<MyUserStore>()
                .AddRoleStore<MyUserStore>();

            _ = services.ConfigureApplicationCookie(AuthenticationConfigurator.SetCookieOptions());

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

            _ = services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN-HEADERNAME");
            var dataProtection = services.AddDataProtection();
            if (blobStorageOptions.BlobStorageConfigured && !environment.IsDevelopment())
            {
                dataProtection.PersistKeysToAzureBlobStorage(
                    blobStorageOptions.BlobStorageConnectionString,
                    "data-protection-keys", "joinrpg-portal-protection-keys");
            }

            if (environment.IsDevelopment())
            {
                //That's make local debug more easy
                _ = mvc.AddRazorRuntimeCompilation();
            }

            _ = services.AddAuthorization();

            _ = services.AddTransient<IAuthorizationPolicyProvider, AuthPolicyProvider>();

            services
                .AddAuthentication()
                .ConfigureJoinExternalLogins(Configuration.GetSection("Authentication"));

            _ = services.AddSwaggerGen(Swagger.ConfigureSwagger);
            _ = services.AddApplicationInsightsTelemetry();

            _ = services.AddHealthChecks()
                .AddSqlServer(Configuration["ConnectionStrings:DefaultConnection"], tags: new[] { "ready" })
                .AddCheck<HealthCheckLoadProjects>("Project load", tags: new[] { "ready" })
                .AddCheck<HealthCheckBlobStorage>("Blob connect");

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
                .RegisterModule(new BlobStorageModule(blobStorageOptions));
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

            _ = app.UseStaticFiles();

            _ = app.UseRouting();

            _ = app.UseMiddleware<DiscoverProjectMiddleware>();

            _ = app.UseAuthentication();
            _ = app.UseAuthorization();

            _ = app.UseEndpoints(endpoints =>
              {
                  endpoints.MapJoinHealthChecks();

                  _ = endpoints.MapControllers();
                  _ = endpoints.MapAreaControllerRoute("Admin_default", "Admin", "Admin/{controller}/{action=Index}/{id?}");
                  _ = endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
              });
        }
    }
}
