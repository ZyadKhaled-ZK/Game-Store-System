namespace GameStore.DAL.Entities;

public enum ApplicationStatus
{
    Pending,
    Approved,
    Rejected
}

public class DeveloperApplication
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? Website { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(500)]
    public string? CvFilePath { get; set; }

    [MaxLength(500)]
    public string? GithubUrl { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
