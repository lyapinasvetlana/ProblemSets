using System.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using ProblemSets.Models;
using static ProblemSets.Additional_Methods.GetKey;

namespace ProblemSets.Controllers
{
    [Authorize]
    public class ProblemSetController : Controller
    {
        private readonly AppIdentityDbContext _context;

        public ProblemSetController(AppIdentityDbContext context)
        {
            _context = context;
        }
        
        public async Task<IActionResult> Index(string sortOrder, string userId)
        {
            ViewBag.IdForAdmin = userId;
            userId = userId==null? User.Claims.ToList()[0].Value:userId;
            SetSortingOptions(sortOrder);
            var problemSets = _context.ProblemSets.Where(x => x.AppUserId == userId);
            ChooseFilter(sortOrder, problemSets);
            ViewBag.ProblemCreated = _context.ProblemSets.Count(problem => problem.AppUserId == userId) ;
            ViewBag.ProblemSolved = _context.SolvedProblems.Count(problem => problem.AppUserId == userId);
            return View(problemSets.ToList());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (User != null && User.Identity.IsAuthenticated)
            {
                ViewBag.Rating=_context.Ratings.FirstOrDefaultAsync(m => m.ProblemSetId == id && m.AppUserId == User.Claims.ToList()[0].Value).Result;
            }
            ViewBag.Pictures=await _context.PicturesStores.Where(m => m.ProblemSetId == id).ToArrayAsync();
            var problemSet = await _context.ProblemSets.FirstOrDefaultAsync(m => m.Id == id);
            return View(problemSet);
        }
        
        public IActionResult Create(string idForAdmin)
        {
            ViewBag.IdForAdmin = idForAdmin;
            ViewBag.Topics  = new List<SelectListItem>
        {
            new SelectListItem { Value = "C#", Text = "C#" },
            new SelectListItem { Value = "Java", Text = "Java" },
            new SelectListItem { Value = "Math", Text = "Math"  },
            new SelectListItem { Value = "Other", Text = "Other"  },
        };
            
            return View();
        }
        
        public async Task<IActionResult> Edit(int? id, string userId)
        {
            ViewBag.Topics  = new List<SelectListItem>
            {
                new SelectListItem { Value = "C#", Text = "C#" },
                new SelectListItem { Value = "Java", Text = "Java" },
                new SelectListItem { Value = "Math", Text = "Math"  },
                new SelectListItem { Value = "Other", Text = "Other"  },
            };
            var problemSet = await _context.ProblemSets.FindAsync(id);
            if (problemSet==null || problemSet.AppUserId!=User.Claims.ToList()[0].Value && !User.IsInRole("Admin"))
            {
                return View("~/Views/Account/AccessDenied.cshtml");
            }
            ViewBag.Picture=await _context.PicturesStores.Where(m => m.ProblemSetId == id).ToArrayAsync();
            return View(problemSet);
        }

       
        [HttpPost]
        public async Task<IActionResult> EditFile(int id, ProblemSet problemSet)
        {
            problemSet.ProblemTagWithSpace=string.Join(" ", problemSet.ProblemTag);
            _context.Update(problemSet);
            await _context.SaveChangesAsync();
            var files = HttpContext.Request.Form.Files;
            await SendFilesToS3(files, problemSet);
            return Redirect("Index");
        }

        
        public async Task<IActionResult> Delete(int? id)
        {

            var problemSet = await _context.ProblemSets.FirstOrDefaultAsync(m => m.Id == id);
            
            if (problemSet==null || problemSet.AppUserId!=User.Claims.ToList()[0].Value && !User.IsInRole("Admin"))
            {
                return View("~/Views/Account/AccessDenied.cshtml");
            }
            return View(problemSet);
        }
        
       
        public async Task<IActionResult> SendSolution(int id, ProblemSet problem)
        {
            ViewBag.Pictures=await _context.PicturesStores.Where(m => m.ProblemSetId == id).ToArrayAsync();
            var problemSet = await _context.ProblemSets.FindAsync(id);
            var solvedProblem = _context.SolvedProblems.FirstOrDefaultAsync(m => m.ProblemSetId == id && m.AppUserId == User.Claims.ToList()[0].Value).Result;
            await SendSolution(solvedProblem, problemSet, problem);
            return View("Details", problemSet);
        }

    
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var problemSet = await _context.ProblemSets.FindAsync(id);
            _context.ProblemSets.Remove(problemSet);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        
        
        [HttpPost]
        public async Task<IActionResult> DeleteFiles(int id)
        {
            var idPicture = Request.Form["picture"][0];
            var picture = await _context.PicturesStores.FindAsync(Convert.ToInt32(idPicture));
            if (picture != null)
            {
                _context.PicturesStores.Remove(picture);
                await _context.SaveChangesAsync();
            }

            ViewBag.Picture = await _context.PicturesStores.Where(m => m.ProblemSetId == id).ToArrayAsync();
            var problemSet = await _context.ProblemSets.FindAsync(id);
            return View("Edit", problemSet);
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(ProblemSet problemSet, string idForAdmin)
        {
            var files = HttpContext.Request.Form.Files;
            if (ModelState.IsValid)
            {
                problemSet.ProblemTagWithSpace=string.Join(" ", problemSet.ProblemTag);
                problemSet.CreationTime=DateTimeOffset.Now;
                if (problemSet.AppUserId==null) problemSet.AppUserId = User.Claims.ToList()[0].Value;
                _context.Add(problemSet);
                await _context.SaveChangesAsync();
            }
            await SendFilesToS3(files, problemSet);
            return Redirect("Index");
 
        }
        
        [HttpPost]
        public async Task<IActionResult> SendRate(int id)
        {
            var problem = await _context.ProblemSets.FindAsync(id);
            
            if (Request.Form["rate"].Count !=0 )
            {
                _context.Add(new Rating {ProblemSetId = problem.Id, AppUserId = User.Claims.ToList()[0].Value, UserRating = Convert.ToInt16(Request.Form["rate"][0])});
                await _context.SaveChangesAsync();
                problem.AverageRate = (double) _context.Ratings.Where(p => p.ProblemSetId == problem.Id).Sum(p => p.UserRating) / _context.Ratings.Count(p => p.ProblemSetId == problem.Id);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", problem);
        }


        public async Task<IActionResult> SendFilesToS3(IFormFileCollection files, ProblemSet problemSet)
        {
            var bucketName = Environment.GetEnvironmentVariable("S3_BUCKET");
            var credentials = new BasicAWSCredentials(Environment.GetEnvironmentVariable("S3_KEY"), Environment.GetEnvironmentVariable("S3_SECRET_KEY"));
            var config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.EUCentral1
            };
            
            using var client = new AmazonS3Client(credentials, config);
            await using var newMemoryStream = new MemoryStream();

            foreach (var file in files)
            {
                await file.CopyToAsync(newMemoryStream);
                var fileExtension = Path.GetExtension(file.FileName); 
                var documentName = $"{BitConverter.ToString(GenerateKey())}{fileExtension}";
                var result = $"https://{bucketName}.s3.amazonaws.com/{documentName}";
                
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = newMemoryStream,
                    Key = documentName,
                    BucketName = bucketName,
                    CannedACL = S3CannedACL.PublicRead
                };
                    
                PicturesStore documentStore = new PicturesStore()
                {
                    CreatedOn = DateTime.Now,
                    PictureName = result,
                    ProblemSetId = problemSet.Id,
                    PicturesType = file.ContentType
                };
                
                var fileTransferUtility = new TransferUtility(client);
                await fileTransferUtility.UploadAsync(uploadRequest);   
                _context.PicturesStores.Add(documentStore);
            }
            await _context.SaveChangesAsync();
            return new EmptyResult();
        }
        
        public async Task<IActionResult> SendSolution(SolvedProblem solvedProblem , ProblemSet problemSet, ProblemSet problem)
        {
            if (solvedProblem!=null)
            {
                TempData.Add("Has Solution",$": {solvedProblem.UserAnswer}");
            }
            else if (problemSet.ProblemAnswer.Contains(problem.ProblemAnswer[0]))
            {
                _context.Add(new SolvedProblem {ProblemSetId = problem.Id, AppUserId = User.Claims.ToList()[0].Value, UserAnswer = problem.ProblemAnswer[0]} );
                await _context.SaveChangesAsync();
                ViewBag.Solution="Correct";
            }
            else
            {
                ViewBag.Solution = "Wrong";
            }

            return new EmptyResult();
        }
        
        public IActionResult ChooseFilter(string sortOrder, IQueryable<ProblemSet> problemSets)
        {
            switch (sortOrder)
            {
                case "name_desc":
                    problemSets = problemSets.OrderByDescending(s => s.Name);
                    break;
                case "Date":
                    problemSets = problemSets.OrderBy(s => s.CreationTime);
                    break;
                case "Theme":
                    problemSets = problemSets.OrderBy(s => s.Theme);
                    break;
                case "Theme_desc":
                    problemSets = problemSets.OrderByDescending(s => s.Theme);
                    break;
                case "date_desc":
                    problemSets = problemSets.OrderByDescending(s => s.CreationTime);
                    break;
                default:
                    problemSets = problemSets.OrderBy(s => s.Name);
                    break;
            }
            return new EmptyResult();
        }
        
        public IActionResult SetSortingOptions(string sortOrder)
        {
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.TimeSortParm = sortOrder == "Date" ? "date_desc" : "Date";
            ViewBag.TopicSortParm = sortOrder == "Theme" ? "Theme_desc" : "Theme";
            return new EmptyResult();
        }
    }
}