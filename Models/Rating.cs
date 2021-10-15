using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProblemSets.Models
{
    public class Rating
    {
        public int ProblemSetId{ get; set; }
        
        public ProblemSet ProblemSet { get; set; }

        [ForeignKey("AppUser")]
        public string AppUserId{ get; set; }
        public AppUser AppUser { get; set; }
        
        public short UserRating{ get; set; }
        
        
    }
}