using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Controllers;

public class HomeController : Controller
{
    private const int MaxSearchQueryLength = 100;
    private const int MaxSearchResultsPerSection = 20;

    private readonly ApplicationDbContext _context;
    private readonly IImageService _imageService;

    public HomeController(ApplicationDbContext context, IImageService imageService)
    {
        _context = context;
        _imageService = imageService;
    }

    public async Task<IActionResult> Index()
    {
        var featuredDepartments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Take(6)
            .ToListAsync();

        var latestPosts = await _context.Posts
            .AsNoTracking()
            .Where(p => p.Published)
            .OrderByDescending(p => p.CreatedAt)
            .Take(3)
            .ToListAsync();

        var featuredPosts = await _context.Posts
            .AsNoTracking()
            .Where(p => p.Published && p.IsFeatured)
            .OrderByDescending(p => p.CreatedAt)
            .Take(3)
            .ToListAsync();

        var model = new PublicHomeIndexViewModel
        {
            FeaturedDepartments = featuredDepartments,
            LatestPosts = latestPosts,
            FeaturedPosts = featuredPosts,
            SearchScope = "Entire Site"
        };

        // Get randomized images for gallery and showcase sections (hero is permanent)
        ViewBag.RandomImages = _imageService.GetRandomHospitalImages(5);

        return View(model);
    }

    public IActionResult About()
    {
        ViewBag.RandomImages = _imageService.GetRandomHospitalImages(6);
        return View();
    }

    public async Task<IActionResult> Services()
    {
        var services = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();

        return View(services);
    }

    public async Task<IActionResult> Doctors()
    {
        var doctors = await _context.Doctors
            .AsNoTracking()
            .Include(d => d.Department)
            .OrderBy(d => d.FullName)
            .ToListAsync();

        foreach (var doctor in doctors.Where(d => string.IsNullOrWhiteSpace(d.Slug)))
        {
            doctor.Slug = BuildSlug(doctor.FullName);
        }

        return View(doctors);
    }

    public async Task<IActionResult> Team()
    {
        var doctors = await _context.Doctors
            .AsNoTracking()
            .Include(d => d.Department)
            .OrderBy(d => d.FullName)
            .ToListAsync();

        foreach (var doctor in doctors.Where(d => string.IsNullOrWhiteSpace(d.Slug)))
        {
            doctor.Slug = BuildSlug(doctor.FullName);
        }

        return View(doctors);
    }

    [HttpGet]
    public async Task<IActionResult> DoctorProfile(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        var normalizedSlug = slug.Trim().ToLowerInvariant();

        var doctor = await _context.Doctors
            .AsNoTracking()
            .Include(d => d.Department)
            .FirstOrDefaultAsync(d => d.Slug != null && d.Slug == normalizedSlug);

        if (doctor is null)
        {
            var doctorsWithoutSlug = await _context.Doctors
                .AsNoTracking()
                .Include(d => d.Department)
                .Where(d => d.Slug == null)
                .ToListAsync();

            doctor = doctorsWithoutSlug.FirstOrDefault(d => BuildSlug(d.FullName) == normalizedSlug);
        }

        if (doctor is null)
        {
            return NotFound();
        }

        return View(doctor);
    }

    public IActionResult PatientInformationHub()
    {
        return View();
    }

    public async Task<IActionResult> News()
    {
        var posts = await _context.Posts
            .AsNoTracking()
            .Where(p => p.Published)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(posts);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Contact()
    {
        return View(new ContactSubmission());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact([Bind("Name,Email,Subject,Message")] ContactSubmission submission)
    {
        submission.Name = submission.Name?.Trim() ?? string.Empty;
        submission.Email = submission.Email?.Trim() ?? string.Empty;
        submission.Subject = submission.Subject?.Trim() ?? string.Empty;
        submission.Message = submission.Message?.Trim() ?? string.Empty;

        ModelState.Clear();
        TryValidateModel(submission);

        if (!ModelState.IsValid)
        {
            return View(submission);
        }

        submission.SubmittedAt = DateTime.UtcNow;
        _context.ContactSubmissions.Add(submission);
        await _context.SaveChangesAsync();

        TempData["ContactSuccess"] = true;
        return RedirectToAction(nameof(Contact));
    }

    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> NewsDetail(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var post = await _context.Posts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == normalizedSlug && p.Published);

        if (post is null)
            return NotFound();

        var morePosts = await _context.Posts
            .AsNoTracking()
            .Where(p => p.Published && p.Id != post.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Take(3)
            .ToListAsync();

        return View(new PublicPostDetailViewModel
        {
            Post = post,
            MorePosts = morePosts
        });
    }

    [HttpGet]
    public async Task<IActionResult> Search(string? query, string? scope)
    {
        var normalizedQuery = query?.Trim() ?? string.Empty;
        if (normalizedQuery.Length > MaxSearchQueryLength)
        {
            normalizedQuery = normalizedQuery[..MaxSearchQueryLength];
        }

        var normalizedScope = scope?.Trim().ToLowerInvariant() switch
        {
            "doctors" => "doctors",
            "services" => "services",
            "news" => "news",
            "patient-info" or "patient information" => "patient-info",
            _ => "all"
        };

        var result = new PublicSearchResultsViewModel
        {
            Query = normalizedQuery,
            Scope = normalizedScope
        };

        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return View(result);
        }

        var includeAll = normalizedScope == "all";

        if (includeAll || normalizedScope == "doctors")
        {
            result.Doctors = await _context.Doctors
                .AsNoTracking()
                .Include(d => d.Department)
                .Where(d => d.FullName.Contains(normalizedQuery) ||
                            d.Specialty.Contains(normalizedQuery) ||
                            (d.Department != null && d.Department.Name.Contains(normalizedQuery)))
                .OrderBy(d => d.FullName)
                .Take(MaxSearchResultsPerSection)
                .ToListAsync();

            foreach (var doctor in result.Doctors.Where(d => string.IsNullOrWhiteSpace(d.Slug)))
            {
                doctor.Slug = BuildSlug(doctor.FullName);
            }
        }

        if (includeAll || normalizedScope == "services")
        {
            result.Services = await _context.Departments
                .AsNoTracking()
                .Where(d => d.Name.Contains(normalizedQuery) || (d.Description != null && d.Description.Contains(normalizedQuery)))
                .OrderBy(d => d.Name)
                .Take(MaxSearchResultsPerSection)
                .ToListAsync();
        }

        if (includeAll || normalizedScope == "news")
        {
            result.News = await _context.Posts
                .AsNoTracking()
                .Where(p => p.Published && (p.Title.Contains(normalizedQuery) || p.Content.Contains(normalizedQuery)))
                .OrderByDescending(p => p.CreatedAt)
                .Take(MaxSearchResultsPerSection)
                .ToListAsync();
        }

        if (includeAll || normalizedScope == "patient-info")
        {
            result.PatientInformation = GetPatientInformationTopics()
                .Where(t => t.Title.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                            t.Summary.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                .Take(MaxSearchResultsPerSection)
                .ToList();
        }

        return View(result);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult HttpStatus(int code)
    {
        if (code is < 400 or > 599)
        {
            code = StatusCodes.Status404NotFound;
        }

        Response.StatusCode = code;
        return View(code);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static List<PatientInformationTopicViewModel> GetPatientInformationTopics()
    {
        return
        [
            new PatientInformationTopicViewModel
            {
                Title = "Preparing for Your Visit",
                Summary = "What to bring, registration details, and how to check in efficiently at the hospital.",
                LinkText = "Read visit guidance",
                LinkUrl = "/patient-information#preparing"
            },
            new PatientInformationTopicViewModel
            {
                Title = "Patient Rights and Responsibilities",
                Summary = "Understand your rights, privacy protections, and responsibilities during treatment.",
                LinkText = "View policy summary",
                LinkUrl = "/patient-information#rights"
            },
            new PatientInformationTopicViewModel
            {
                Title = "Inpatient and Visitor Information",
                Summary = "Admission process, visiting hours, ward protocols, and family communication guidance.",
                LinkText = "See visitor policy",
                LinkUrl = "/patient-information#visiting-hours"
            },
            new PatientInformationTopicViewModel
            {
                Title = "Discharge and Follow-up Care",
                Summary = "Medication instructions, follow-up planning, and support after discharge.",
                LinkText = "Review discharge steps",
                LinkUrl = "/patient-information#preparing"
            },
            new PatientInformationTopicViewModel
            {
                Title = "Billing and Insurance Guidance",
                Summary = "Understand hospital billing processes and documentation needed for insurance claims.",
                LinkText = "Learn billing basics",
                LinkUrl = "/patient-information#payments"
            },
            new PatientInformationTopicViewModel
            {
                Title = "Health Education Resources",
                Summary = "Access practical resources for preventive care, chronic disease support, and healthy living.",
                LinkText = "Browse resources",
                LinkUrl = "/news"
            }
        ];
    }

    private static string BuildSlug(string source)
    {
        source = source.Trim().ToLowerInvariant();
        source = Regex.Replace(source, "[^a-z0-9\\s-]", string.Empty);
        source = Regex.Replace(source, "\\s+", "-");
        source = Regex.Replace(source, "-+", "-");
        return source.Trim('-');
    }
}
