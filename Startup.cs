using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
/*using WebAppEnd.IdentityPolicy;*/
using System;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using ProblemSets.ConfigDataBase;
using ProblemSets.Models;
/*using Microsoft.AspNetCore.Authorization;
using WebAppEnd.CustomPolicy;*/

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
                opts.Password.RequiredLength = 8;
                opts.Password.RequireNonAlphanumeric = true;
                opts.Password.RequireLowercase = false;
                opts.Password.RequireUppercase = true;
                opts.Password.RequireDigit = true;

                //opts.SignIn.RequireConfirmedEmail = true;

                opts.Lockout.AllowedForNewUsers = true;
                opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                opts.Lockout.MaxFailedAccessAttempts = 3;
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
                    options.ClientId = Environment.GetEnvironmentVariable("IDGOOGLE");;
                    options.ClientSecret = Environment.GetEnvironmentVariable("SECRETGOOGLE");
                    options.SignInScheme = IdentityConstants.ExternalScheme;
                })
                .AddGitHub(options =>
            {
                options.ClientId = Environment.GetEnvironmentVariable("IDGITHUB");
                options.ClientSecret = Environment.GetEnvironmentVariable("SECRETGITHUB");
                options.SignInScheme = IdentityConstants.ExternalScheme;
            });
            services.AddControllersWithViews().AddDataAnnotationsLocalization() // добавляем локализацию аннотаций;
                .AddViewLocalization();
            
            services.AddHttpContextAccessor();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var supportedCultures = new[]
            {
                new CultureInfo("en"),
                new CultureInfo("ru")
            };
            
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("ru"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });
            
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
        }
    }
}