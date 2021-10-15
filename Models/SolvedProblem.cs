using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProblemSets.Models
{
    public class SolvedProblem
    {
        public int ProblemSetId{ get; set; }
        
        public ProblemSet ProblemSet { get; set; }

        [ForeignKey("AppUser")]
         public string AppUserId{ get; set; }
                
         public AppUser AppUser { get; set; }
         public string UserAnswer{ get; set; }
    }
}