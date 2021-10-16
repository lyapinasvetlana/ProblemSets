using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace ProblemSets.Models
{
    public class AppUser : IdentityUser
    {
        public string NameSocialMedia { get; set; }
        
    }
}