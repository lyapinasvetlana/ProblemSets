using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProblemSets.Models
{
    [Table("PicturesStore")]
    public class PicturesStore
    {
       
        [Key]
        public int PictureId { get; set; }
        public string PictureName { get; set; }
        public DateTime CreatedOn { get; set; }
        public string PicturesType { get; set; }
        
        public int ProblemSetId{ get; set; }
        
        public ProblemSet ProblemSet { get; set; }
        
    }
}