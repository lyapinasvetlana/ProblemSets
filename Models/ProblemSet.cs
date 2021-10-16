using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ProblemSets.Models
{
    public class ProblemSet
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Theme { get; set; }
        
        public DateTimeOffset CreationTime { get; set; }
        public List<string> ProblemTag { get; set; }
        
        public string ProblemTagWithSpace { get; set; }
        public string ProblemQuestion { get; set; }
        public List<string> ProblemAnswer { get; set; }

        [ForeignKey("AppUser")]
        public string AppUserId{ get; set; }
        
        public AppUser AppUser { get; set; }
        
        public double? AverageRate { get; set; }

        public ProblemSet()
        {

        }
    }
}