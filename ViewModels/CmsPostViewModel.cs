using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.ViewModels;

public class CmsPostViewModel
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(220)]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Summary")]
    public string Summary { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
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
