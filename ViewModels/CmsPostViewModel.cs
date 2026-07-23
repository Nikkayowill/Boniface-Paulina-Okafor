using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.ViewModels;

public class CmsPostViewModel
{
    public int Id { get; set; }

    [Required, StringLength(120, MinimumLength = 5)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(220)]
    [RegularExpression(
        "^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Use lowercase letters, numbers, and single hyphens only.")]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Summary")]
    public string Summary { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
    [MinLength(20, ErrorMessage = "Content must contain at least 20 characters.")]
    [Display(Name = "Content")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Published")]
    public bool Published { get; set; }

    [Display(Name = "Featured")]
    public bool IsFeatured { get; set; }

    [Display(Name = "Thumbnail image")]
    public IFormFile? Thumbnail { get; set; }

    /// <summary>Filled on Edit GET so the view can show the current thumbnail.</summary>
    public string? ExistingThumbnailUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}
