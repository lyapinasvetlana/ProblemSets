using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using ProblemSets.Models;

namespace ProblemSets.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private UserManager<AppUser> userManager;
        private SignInManager<AppUser> signInManager;

        public AccountController(UserManager<AppUser> userMgr, SignInManager<AppUser> signinMgr)
        {
            userManager = userMgr;
            signInManager = signinMgr;
        }
        
  
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }
        
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        
        public IActionResult AccessDenied()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult GoogleLogin()
        {
            string redirectUrl = Url.Action("NetWorkResponse", "Account");
            var properties = signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return new ChallengeResult("Google", properties);
        }
        
        [AllowAnonymous]
        public IActionResult GitHubLogin()
        {
            string redirectUrl = Url.Action("NetWorkResponse", "Account");
            var properties = signInManager.ConfigureExternalAuthenticationProperties("GitHub", redirectUrl);
            return new ChallengeResult("GitHub", properties);
        }

        [AllowAnonymous]
        public async Task<IActionResult> NetWorkResponse()
        {
            ExternalLoginInfo info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction(nameof(Login));

            var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false);

            if (result.Succeeded)
                return RedirectToAction("Index", "Home");
            else
            {
                await RegisterNewUser(info);
                return AccessDenied();
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> RegisterNewUser(ExternalLoginInfo info)
        {
            AppUser user = new AppUser
            {
                UserName = info.Principal.FindFirst(ClaimTypes.Name)?.Value,
                NameSocialMedia = info.LoginProvider
            };

            IdentityResult identResult = await userManager.CreateAsync(user);
            if (identResult.Succeeded)
            {
                await userManager.AddLoginAsync(user, info);
                await signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Home");
            }

            return new EmptyResult();
        }
        
    }
}