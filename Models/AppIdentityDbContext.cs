using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ProblemSets.Models
{
    public class AppIdentityDbContext: IdentityDbContext<AppUser>
    {
        public DbSet<ProblemSet> ProblemSets { get; set; }
        public DbSet<PicturesStore> PicturesStores { get; set; }
        public DbSet<SolvedProblem> SolvedProblems { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options) : base(options)
        {
            //Database.EnsureDeleted();
            Database.EnsureCreated();
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SolvedProblem>().HasKey(u => new { u.AppUserId, u.ProblemSetId});
            modelBuilder.Entity<Rating>().HasKey(u => new { u.AppUserId, u.ProblemSetId});
            
            modelBuilder.Entity<ProblemSet>()
                .HasIndex(b => new {  b.Theme, b.ProblemQuestion, b.ProblemTagWithSpace, b.Name})
                .IsTsVectorExpressionIndex("english");

           
        }
    }
}