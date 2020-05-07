using Autofac;
using JoinRpg.DI;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using Joinrpg.Web.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;
using System.Globalization;
using JoinRpg.Portal.Infrastructure.Localization;
using Autofac.Core;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Reflection;
using JoinRpg.Web.Models;
using JoinRpg.Portal.Resources;
using Microsoft.AspNetCore.Mvc.DataAnnotations;

namespace JoinRpg.Portal
{
    public class Startup
    {
        private readonly IWebHostEnvironment environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            this.environment = environment;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<RecaptchaOptions>(Configuration.GetSection("Recaptcha"));
            services.Configure<LetsEncryptOptions>(Configuration.GetSection("LetsEncrypt"));

            services.Configure<PasswordHasherOptions>(options => options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2);

            services
                .AddIdentity<JoinIdentityUser, string>(options => options.Password.ConfigureValidation())
                .AddDefaultTokenProviders()
                .AddUserStore<MyUserStore>()
                .AddRoleStore<MyUserStore>();

            services.ConfigureApplicationCookie(AuthenticationConfigurator.SetCookieOptions());

            services.AddLogging();

            services.AddHttpContextAccessor();
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();

            services.AddRouting(options => options.LowercaseUrls = true);

            services.AddSingleton<IValidationAttributeAdapterProvider, BooleanRequiredAttributeAdapterProvider>();
            services.AddSingleton<LocalizationService>();
            services.AddLocalization(o => o.ResourcesPath = "Resources");

            IList<CultureInfo> supportedCultures = new List<CultureInfo>
            {
                new CultureInfo("ru-RU"),
                new CultureInfo("en-US")
            };

            services.Configure<RequestLocalizationOptions>(options => {

                options.DefaultRequestCulture = new RequestCulture("ru-RU");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;

                options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
            });


            var mvc = services
                .AddMvc(options =>
                {
                    if (!environment.IsEnvironment("IntegrationTest"))
                    {
                        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                    }
                    options.Filters.Add(new SetIsProductionFilterAttribute());
                    options.Filters.Add(new TypeFilterAttribute(typeof(SetUserDataFilterAttribute)));
                })
                .AddControllersAsServices()
                .AddViewComponentsAsServices()
                .AddDataAnnotationsLocalization(options =>
                {
                    options.DataAnnotationLocalizerProvider = (type, factory) => LocalizationService.CreateLocalizer(factory);
                });

            if (environment.IsDevelopment())
            {
                //That's make local debug more easy
                mvc.AddRazorRuntimeCompilation();
            }

            services.AddAuthorization();

            services.AddTransient<IAuthorizationPolicyProvider, AuthPolicyProvider>();

            services
                .AddAuthentication()
                .ConfigureJoinExternalLogins(Configuration.GetSection("Authentication"));

            services.AddSwaggerGen(Swagger.ConfigureSwagger);
            services.AddApplicationInsightsTelemetry();
        }

        /// <summary>
        /// Runs after ConfigureServices
        /// </summary>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new JoinrpgMainModule());
            builder.RegisterModule(new JoinRpgPortalModule());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRequestLocalization(options => {

                options.SupportedCultures = new List<CultureInfo> { new CultureInfo("ru-RU") };
                options.SupportedUICultures = new List<CultureInfo> { new CultureInfo("ru-RU") };
                options.DefaultRequestCulture = new RequestCulture("ru-RU");

                options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else if (env.IsEnvironment("IntegrationTest"))
            {
                //need this to ensure that exceptions from controller will fall directly to integration test
            }
            else
            {
                app.UseExceptionHandler("/error");
            }

            app.Use(async (context, next) =>
            {
                await next();
                if (context.Response.StatusCode == 404)
                {
                    context.Request.Path = "/error/404";
                    await next();
                }
            });

            app.UseSwagger(Swagger.Configure);
            app.UseSwaggerUI(Swagger.ConfigureUI);

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseMiddleware<DiscoverProjectMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                //TODO endpoints.MapHealthChecks("/health");
                endpoints.MapControllers();
                endpoints.MapAreaControllerRoute("Admin_default", "Admin", "Admin/{controller}/{action=Index}/{id?}");
                endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
