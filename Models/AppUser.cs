using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace ProblemSets.Models
{
    public class AppUser : IdentityUser
    {
        public string NameSocialMedia { get; set; }
        //public virtual ICollection<ProblemSet> ProblemSets{ get; set; }
        
        /*public Country Country { get; set; }

        public int Age { get; set; }

        [Required]
        public string Salary { get; set; }*/
    }
}