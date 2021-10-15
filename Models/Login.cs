using System.ComponentModel.DataAnnotations;

namespace ProblemSets.Models
{
    public class Login
    {
        [Required]
        public string Email { get; set; }

        public string ReturnUrl { get; set; }
    }
}