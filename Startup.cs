using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Localization;
using ProblemSets.ConfigDataBase;
using ProblemSets.Models;



namespace ProblemSets
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;
        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLocalization(options => options.ResourcesPath = "Resources");

            services.AddEntityFrameworkNpgsql().AddDbContext<AppIdentityDbContext>(options =>
            {
                options.UseNpgsql(Config.SetConfig());
            });
            
            services.AddIdentity<AppUser, IdentityRole>().AddEntityFrameworkStores<AppIdentityDbContext>().AddDefaultTokenProviders();
            services.Configure<IdentityOptions>(opts =>
            {
                opts.User.RequireUniqueEmail = false;
                opts.User.AllowedUserNameCharacters = null;
                opts.Lockout.AllowedForNewUsers = true;
                opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                opts.Lockout.MaxFailedAccessAttempts = 10;
            });

            services.AddAuthorization(opts => {
                opts.AddPolicy("AspManager", policy => {
                    policy.RequireRole("Manager");
                    policy.RequireClaim("Coding-Skill", "ASP.NET Core MVC");
                });
            });

            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = Environment.GetEnvironmentVariable("IDGOOGLE");
                    options.ClientSecret = Environment.GetEnvironmentVariable("SECRETGOOGLE");
                    options.SignInScheme = IdentityConstants.ExternalScheme;
                })
                .AddGitHub(options =>
                {
                options.ClientId = Environment.GetEnvironmentVariable("IDGITHUB");
                options.ClientSecret = Environment.GetEnvironmentVariable("SECRETGITHUB");
                options.SignInScheme = IdentityConstants.ExternalScheme;
                });
            
            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en"),
                    new CultureInfo("ru"),
                };
                options.DefaultRequestCulture = new RequestCulture(culture: "en", uiCulture: "ru");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });
            
            services.AddControllersWithViews().AddDataAnnotationsLocalization().AddViewLocalization();
            services.AddHttpContextAccessor();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, RoleManager<IdentityRole> roleManager,UserManager<AppUser> userManager)
        { 
            
            var supportedCultures = new string[] { "en", "ru" };
           
            app.UseRequestLocalization(options =>
                options
                    .AddSupportedCultures(supportedCultures)
                    .AddSupportedUICultures(supportedCultures)
                    
            );
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseForwardedHeaders();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseForwardedHeaders();
                app.UseHsts();
            }
            
            app.Use((context, next) =>
            {
                context.Request.Scheme = "https";
                return next();
            });
            
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
            Task.Run(()=>this.CreateRoles(roleManager, userManager)).Wait();
            
        }

        private async Task CreateRoles(RoleManager<IdentityRole> roleManager, UserManager<AppUser> userManager)
        {
            List<string> roles = new List<string>(){"Admin"};
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            AppUser user = await userManager.FindByIdAsync(Environment.GetEnvironmentVariable("ADMIN_ID"));
            if (user != null)
            {
                var userRoles = await userManager.GetRolesAsync(user);
                var addedRoles = roles.Except(userRoles);
                await userManager.AddToRolesAsync(user,addedRoles);
            }
           
        }
    }
}