using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.Models;

public class PushSubscription
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string P256DH { get; set; } = string.Empty;

    [Required]
    public string Auth { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastUsedAt { get; set; }

    public ApplicationUser? User { get; set; }
}
