using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
public class PostsController : AdminBaseController
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private const long MaxImageBytes = 5 * 1024 * 1024;

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public PostsController(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var posts = await _context.Posts
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(posts);
    }

    [HttpGet]
    public IActionResult Create() => View(new CmsPostViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CmsPostViewModel vm)
    {
        vm.Title = vm.Title.Trim();
        vm.Slug = vm.Slug.Trim().ToLowerInvariant();
        vm.Summary = vm.Summary.Trim();
        vm.Content = vm.Content.Trim();

        if (await _context.Posts.AnyAsync(p => p.Slug == vm.Slug))
            ModelState.AddModelError(nameof(vm.Slug), "A post with this slug already exists.");

        string? thumbnailUrl = null;
        if (vm.Thumbnail is { Length: > 0 })
        {
            var validationError = ValidateImage(vm.Thumbnail);
            if (validationError is not null)
                ModelState.AddModelError(nameof(vm.Thumbnail), validationError);
            else
                thumbnailUrl = await SaveThumbnailAsync(vm.Thumbnail);
        }

        if (!ModelState.IsValid)
            return View(vm);

        var post = new Post
        {
            Title = vm.Title,
            Slug = vm.Slug,
            Summary = vm.Summary,
            Content = vm.Content,
            Published = vm.Published,
            IsFeatured = vm.IsFeatured,
            ThumbnailImageUrl = thumbnailUrl,
            CreatedAt = DateTime.UtcNow
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Post created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post is null) return NotFound();

        return View(new CmsPostViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Summary = post.Summary,
            Content = post.Content,
            Published = post.Published,
            IsFeatured = post.IsFeatured,
            ExistingThumbnailUrl = post.ThumbnailImageUrl,
            CreatedAt = post.CreatedAt
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CmsPostViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        var post = await _context.Posts.FindAsync(id);
        if (post is null) return NotFound();

        vm.Title = vm.Title.Trim();
        vm.Slug = vm.Slug.Trim().ToLowerInvariant();
        vm.Summary = vm.Summary.Trim();
        vm.Content = vm.Content.Trim();

        if (await _context.Posts.AnyAsync(p => p.Slug == vm.Slug && p.Id != id))
            ModelState.AddModelError(nameof(vm.Slug), "A post with this slug already exists.");

        if (vm.Thumbnail is { Length: > 0 })
        {
            var validationError = ValidateImage(vm.Thumbnail);
            if (validationError is not null)
                ModelState.AddModelError(nameof(vm.Thumbnail), validationError);
        }

        if (!ModelState.IsValid)
        {
            vm.ExistingThumbnailUrl = post.ThumbnailImageUrl;
            return View(vm);
        }

        post.Title = vm.Title;
        post.Slug = vm.Slug;
        post.Summary = vm.Summary;
        post.Content = vm.Content;
        post.Published = vm.Published;
        post.IsFeatured = vm.IsFeatured;
        post.UpdatedAt = DateTime.UtcNow;

        if (vm.Thumbnail is { Length: > 0 })
        {
            DeleteImage(post.ThumbnailImageUrl);
            post.ThumbnailImageUrl = await SaveThumbnailAsync(vm.Thumbnail);
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Post updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublish(int id)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post is null) return NotFound();

        post.Published = !post.Published;
        post.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = post.Published ? "Post published." : "Post unpublished.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFeatured(int id)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post is null) return NotFound();

        post.IsFeatured = !post.IsFeatured;
        post.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = post.IsFeatured ? "Post marked as featured." : "Post removed from featured.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post is null) return NotFound();

        DeleteImage(post.ThumbnailImageUrl);
        _context.Posts.Remove(post);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Post deleted.";
        return RedirectToAction(nameof(Index));
    }

    private static string? ValidateImage(IFormFile file)
    {
        if (file.Length > MaxImageBytes)
            return "Image must be under 5 MB.";

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            return "Only .jpg, .jpeg, .png, and .webp files are allowed.";

        if (!AllowedMimeTypes.Contains(file.ContentType))
            return "Only JPG, JPEG, PNG, and WebP images are allowed.";

        return null;
    }

    private async Task<string> SaveThumbnailAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{extension}";
        var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "posts");

        Directory.CreateDirectory(uploadsPath);

        var fullPath = Path.Combine(uploadsPath, fileName);
        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/posts/{fileName}";
    }

    private void DeleteImage(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
            return;

        var physicalPath = Path.Combine(
            _env.WebRootPath,
            relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (System.IO.File.Exists(physicalPath))
            System.IO.File.Delete(physicalPath);
    }
}
