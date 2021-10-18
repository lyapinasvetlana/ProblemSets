using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using ProblemSets.Models;

namespace ProblemSets.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        
        private readonly AppIdentityDbContext _context;
        private readonly IStringLocalizer<HomeController> _localizer;
        
        public HomeController(AppIdentityDbContext context, IStringLocalizer<HomeController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }


        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            ViewBag.TagsGrouped=_context.ProblemSets.Select(l => l.ProblemTag).ToList().SelectMany(l => l).GroupBy(v => v);
            return View(await _context.ProblemSets.ToListAsync());
        }

        

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return RedirectToAction("Index");
        }
        

        public async Task<IActionResult> ShowSearchResults(String SearchPhrase)
        {
           
            var npgsql = _context.ProblemSets
                .Where(p => EF.Functions.ToTsVector("english", p.Theme + " " + p.ProblemQuestion)
                    .Matches(SearchPhrase) || EF.Functions.ToTsVector("english", p.ProblemTagWithSpace + " " + p.Name)
                    .Matches(SearchPhrase) )
                .ToList();
            
            return View(npgsql);
        }
        
        [AllowAnonymous]
        public async Task<IActionResult> SetTheme(string theme, string path)
        {
            
            CookieOptions cookies = new CookieOptions();
            cookies.Expires = DateTimeOffset.UtcNow.AddYears(1);
            if (theme != null) Response.Cookies.Append("theme", theme, cookies);
            else Response.Cookies.Append("theme", "off", cookies);
            return Redirect(path);
        }
        
        [AllowAnonymous]
        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true
                }
            );
 
            return LocalRedirect(returnUrl);
        }
        
        

    }
}