using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ProblemSets.Models;
using ProblemSets.ViewModels;

namespace ProblemSets.Controllers
{
    public class RolesController : Controller
    {
        public const string SiteAdmin = "Admin";
        public const string id = "3ec51dc1-6dea-4ab5-add9-eeebf25ef1c3";
        RoleManager<IdentityRole> _roleManager;
        UserManager<AppUser> _userManager;
        

        public RolesController(RoleManager<IdentityRole> roleManager, UserManager<AppUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            
        }
        
        public async void CreateBaseRoles()
        {
            string id = "3ec51dc1-6dea-4ab5-add9-eeebf25ef1c3";
            
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            var user = await _userManager.FindByIdAsync(id);
            var userRoles = await _userManager.GetRolesAsync(user);
            var addedRoles = userRoles.Except(userRoles);
            await _userManager.AddToRolesAsync(user, addedRoles);
            
        }
        public IActionResult Index() => View(_roleManager.Roles.ToList());
 
        public IActionResult Create() => View();
        [HttpPost]
        public async Task<IActionResult> Create(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                IdentityResult result = await _roleManager.CreateAsync(new IdentityRole(name));
                if (result.Succeeded)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            return View(name);
        }
         
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            IdentityRole role = await _roleManager.FindByIdAsync(id);
            if (role != null)
            {
                IdentityResult result = await _roleManager.DeleteAsync(role);
            }
            return RedirectToAction("Index");
        }
 
        public IActionResult UserList() => View(_userManager.Users.ToList());
 
        public async Task<IActionResult> Edit(string userId)
        {
            AppUser user = await _userManager.FindByIdAsync(userId);
            if(user!=null)
            {
              
                var userRoles = await _userManager.GetRolesAsync(user);
                var allRoles = _roleManager.Roles.ToList();
                ChangeRoleViewModel model = new ChangeRoleViewModel
                {
                    UserId = user.Id,
                    UserRoles = userRoles,
                    AllRoles = allRoles
                };
                return View(model);
            }
 
            return NotFound();
        }
        [HttpPost]
        public async Task<IActionResult> Edit(string userId, List<string> roles)
        {

            AppUser user = await _userManager.FindByIdAsync(userId);
            if(user!=null)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                
                var allRoles = _roleManager.Roles.ToList();
                
                var addedRoles = roles.Except(userRoles);
                
                var removedRoles = userRoles.Except(roles);
 
                await _userManager.AddToRolesAsync(user, addedRoles);
 
                await _userManager.RemoveFromRolesAsync(user, removedRoles);
 
                return RedirectToAction("UserList");
            }
 
            return NotFound();
        }
    }
}