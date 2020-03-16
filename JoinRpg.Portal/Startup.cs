using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Joinrpg.Web.Identity;
using Autofac;
using JoinRpg.DataModel;
using JoinRpg.DI;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JoinRpg.Portal
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<PasswordHasherOptions>(options => options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2);

            services
                .AddIdentity<JoinIdentityUser, string>()
                .AddDefaultTokenProviders()
                .AddUserStore<MyUserStore>()
                .AddRoleStore<MyUserStore>();

            services.AddLogging();

            services.AddHttpContextAccessor();
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();

            services
                .AddMvc(options =>
                {
                    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                    options.Filters.Add(new SetIsProductionFilterAttribute());
                    options.Filters.Add(new TypeFilterAttribute(typeof(SetUserDataFilterAttribute)));
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddAuthorization(options =>
                {
                    options.AddPolicyAsRequirement<AllowPublishRequirement>();
                    options.AddPolicy(PolicyConstants.AllowAdminPolicy,
                        policy => policy.RequireAssertion(context => context.User.IsInRole(Security.AdminRoleName)));
                }
            );

            services.AddTransient<IAuthorizationPolicyProvider, AllowMasterPolicyProvider>();
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
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapAreaRoute("Admin_default", "Admin", "Admin/{controller}/{action}/{id?}");
            });
        }
    }
}