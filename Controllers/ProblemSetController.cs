using System.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Data;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProblemSets.Models;

namespace ProblemSets.Controllers
{
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
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.TimeSortParm = sortOrder == "Date" ? "date_desc" : "Date";
            ViewBag.TopicSortParm = sortOrder == "Theme" ? "Theme_desc" : "Theme";
            var problemSets = from s in _context.ProblemSets.Where(x=>x.AppUserId==userId)
                select s;
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
            
            
            ViewBag.ProblemCreated = _context.ProblemSets.Count(problem => problem.AppUserId == userId) ;
            ViewBag.ProblemSolved = _context.SolvedProblems.Count(problem => problem.AppUserId == userId);
            return View(problemSets.ToList());
        }
        
        
        
        public async Task<IActionResult> Details(int? id)
        {

            if (User != null && User.Identity.IsAuthenticated)
            {
                ViewBag.Rating=_context.Ratings
                    .FirstOrDefaultAsync(m => m.ProblemSetId == id && m.AppUserId == User.Claims.ToList()[0].Value).Result;
            }
            
            ViewBag.Pictures=await _context.PicturesStores 
                .Where(m => m.ProblemSetId == id).ToArrayAsync();
 
            
            if (id == null)
            {
                return NotFound();
            }

            var joke = await _context.ProblemSets
                .FirstOrDefaultAsync(m => m.Id == id);
            if (joke == null)
            {
                return NotFound();
            }

            return View(joke);
        }

        
        [Authorize]
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

       
        /*[Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ProblemQuestion,ProblemAnswer")] ProblemSet joke)
        {
           
            if (ModelState.IsValid)
            {
                _context.Add(joke);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(joke);
        }*/

       
        [Authorize]
        public async Task<IActionResult> Edit(int? id, string userId)
        {
            ViewBag.Topics  = new List<SelectListItem>
            {
                new SelectListItem { Value = "C#", Text = "C#" },
                new SelectListItem { Value = "Java", Text = "Java" },
                new SelectListItem { Value = "Math", Text = "Math"  },
                new SelectListItem { Value = "Other", Text = "Other"  },
            };
            
            var joke = await _context.ProblemSets.FindAsync(id);
            if (joke.AppUserId!=User.Claims.ToList()[0].Value && !User.IsInRole("Admin"))
            {
                return View("~/Views/Account/AccessDenied.cshtml");
            }
            
            ViewBag.Picture=await _context.PicturesStores 
                .Where(m => m.ProblemSetId == id).ToArrayAsync();
            
            
            if (id == null)
            {
                return NotFound();
            }

            
            if (joke == null)
            {
                return NotFound();
            }
            return View(joke);
        }

       
        [HttpPost]
        /*[ValidateAntiForgeryToken]*/
        public async Task<IActionResult> Edit(int id, ProblemSet problemSet)
        {

            problemSet.ProblemTagWithSpace=string.Join(" ", problemSet.ProblemTag);
            /*problemSet.AppUserId = (await _context.ProblemSets.FindAsync(id)).AppUserId;*/
            _context.Update(problemSet);
          
            var files = HttpContext.Request.Form.Files;
            if (id != problemSet.Id)
            {
                return NotFound();
            }
           
            
            var bucketName =  Environment.GetEnvironmentVariable("S3_BUCKET");

            var credentials =
                new BasicAWSCredentials(Environment.GetEnvironmentVariable("S3_KEY"), Environment.GetEnvironmentVariable("S3_SECRET_KEY"));
            var config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.EUCentral1
            };
            foreach (var file in files)
            {
                var key = new byte[8];

                using (var random = RandomNumberGenerator.Create())
                {
                    random.GetBytes(key);
                }
                    
                using var client = new AmazonS3Client(credentials, config);
                await using var newMemoryStream = new MemoryStream();
                await file.CopyToAsync(newMemoryStream);

                var fileExtension = Path.GetExtension(file.FileName);
                var documentName = $"{BitConverter.ToString(key)}{fileExtension}";

               
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
                //await fileTransferUtility.UploadAsync(uploadRequest);
                
                 await _context.AddAsync(documentStore);
                //_context.PicturesStores.Add(documentStore);
                await _context.SaveChangesAsync();

            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(problemSet);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JokeExists(problemSet.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(problemSet);
        }

       
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var joke = await _context.ProblemSets
                    
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (joke.AppUserId!=User.Claims.ToList()[0].Value && !User.IsInRole("Admin"))
            {
                return View("~/Views/Account/AccessDenied.cshtml");
            }
            return View(joke);
        }
        
        [Authorize]
        public async Task<IActionResult> SendSolution(int id, /*[Bind("Id,ProblemQuestion,ProblemAnswer")] */ProblemSet problem)
        {
            

            ViewBag.Pictures=await _context.PicturesStores
                .Where(m => m.ProblemSetId == id).ToArrayAsync();
            var joke = await _context.ProblemSets.FindAsync(id);
            var solvedProblem = _context.SolvedProblems
                .FirstOrDefaultAsync(m => m.ProblemSetId == id && m.AppUserId == User.Claims.ToList()[0].Value).Result;
            if (solvedProblem!=null)
            {
                TempData.Add("Has Solution",$": {solvedProblem.UserAnswer}");
            }
            else if (joke.ProblemAnswer.Contains(problem.ProblemAnswer[0]))
            {
                _context.Add(new SolvedProblem {ProblemSetId = problem.Id, AppUserId = User.Claims.ToList()[0].Value, UserAnswer = problem.ProblemAnswer[0]} );
                await _context.SaveChangesAsync();
                TempData.Add("Good Alert","");
            }
            
            
            else
            {
                TempData.Add("Bad Alert","");
            }

            return RedirectToAction("Details", joke);
            //return View("Details", problemSet);
        }
        
        
        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var problemSet = await _context.ProblemSets.FindAsync(id);
            _context.ProblemSets.Remove(problemSet);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool JokeExists(int id)
        {
            return _context.ProblemSets.Any(e => e.Id == id);
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
            if (problemSet == null)
            {
                return NotFound();
            }
            return View("Edit", problemSet);
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(ProblemSet joke, string idForAdmin)
        {
            
            
            
            var files = HttpContext.Request.Form.Files;
           
            
            if (ModelState.IsValid)
            {
                joke.ProblemTagWithSpace=string.Join(" ", joke.ProblemTag);
                joke.CreationTime=DateTimeOffset.Now;
                if (joke.AppUserId==null) joke.AppUserId = User.Claims.ToList()[0].Value;
             
                
                _context.Add(joke);
                await _context.SaveChangesAsync();
            }
            
                
                var bucketName = Environment.GetEnvironmentVariable("S3_BUCKET");

                var credentials =
                    new BasicAWSCredentials(Environment.GetEnvironmentVariable("S3_KEY"), Environment.GetEnvironmentVariable("S3_SECRET_KEY"));
                var config = new AmazonS3Config
                {
                    RegionEndpoint = Amazon.RegionEndpoint.EUCentral1
                };

                foreach (var file in files)
                {
                    var key = new byte[8];

                    using (var random = RandomNumberGenerator.Create())
                    {
                        random.GetBytes(key);
                    }
                    
                    using var client = new AmazonS3Client(credentials, config);
                    await using var newMemoryStream = new MemoryStream();
                    await file.CopyToAsync(newMemoryStream);

                    var fileExtension = Path.GetExtension(file.FileName);
                    var documentName = $"{BitConverter.ToString(key)}{fileExtension}";

                    
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
                        ProblemSetId = joke.Id,
                        PicturesType = file.ContentType
                    };
                    
                    var fileTransferUtility = new TransferUtility(client);
                    //await fileTransferUtility.UploadAsync(uploadRequest);
               
                    _context.PicturesStores.Add(documentStore);
                    await _context.SaveChangesAsync();

                }
                
                return Redirect("Index");
                
            

        }
        
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SendRate(int id)
        {
            
            var userRate = Convert.ToInt16(Request.Form["rate"][0]);
            var problem = await _context.ProblemSets.FindAsync(id);
            
                _context.Add(new Rating {ProblemSetId = problem.Id, AppUserId = User.Claims.ToList()[0].Value, UserRating = userRate} );
                await _context.SaveChangesAsync();  
                
             var kek = (double)_context.Ratings.Where(p => p.ProblemSetId == problem.Id).Sum(p => p.UserRating) /
                      _context.Ratings.Count(p => p.ProblemSetId == problem.Id);
             problem.AverageRate = kek;
                
                await _context.SaveChangesAsync();
                
                return RedirectToAction("Details", problem);
                
        }
        
    
    }
}