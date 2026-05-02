using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.Models;

public class Post
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(220)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(500)]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public bool Published { get; set; }
    public bool IsFeatured { get; set; }

    [StringLength(500)]
    public string? ThumbnailImageUrl { get; set; }
}
