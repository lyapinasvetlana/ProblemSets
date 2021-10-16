using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProblemSets.Models;

namespace ProblemSets.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppIdentityDbContext _context;
        UserManager<AppUser> _userManager;
        
        public AdminController(AppIdentityDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        
        public IActionResult Index()
        {
            return View(_userManager.Users.ToList());
        }
        
        [HttpPost]
        public async Task<IActionResult> Manage(string userId)
        {
            AppUser user = await _userManager.FindByIdAsync(userId);
                if(user!=null)
                {
                    return RedirectToAction("Index","ProblemSet", new { userId = userId });
                }
 
                return NotFound();
            
            /*return RedirectToAction("Index");*/
        }
    }
}