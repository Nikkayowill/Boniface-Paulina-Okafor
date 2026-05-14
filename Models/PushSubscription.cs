using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.Models;

public class PushSubscription
{
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string EndpointHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string P256DH { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Auth { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastUsedAt { get; set; }

    public DateTime? LastSuccessAt { get; set; }

    public DateTime? LastFailureAt { get; set; }

    public int FailureCount { get; set; }

    public ApplicationUser? User { get; set; }
}
