using System.ComponentModel.DataAnnotations;

namespace TripPacking.Entities;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Avatar { get; set; }

    public UserStatus Status { get; set; }

    public PasswordHashVersion PasswordHashVersion { get; set; } = PasswordHashVersion.Sha256;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
