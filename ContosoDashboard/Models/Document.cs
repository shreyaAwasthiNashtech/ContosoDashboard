using System.ComponentModel.DataAnnotations;

namespace ContosoDashboard.Models;

public enum DocumentStatus
{
    Pending,
    Ready,
    Rejected
}

public class Document
{
    [Key]
    public int DocumentId { get; set; }

    [Required, MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Tags { get; set; }

    [Required, MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string FileType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    [Required, MaxLength(500)]
    public string StorageReference { get; set; } = string.Empty;

    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public int UploadedByUserId { get; set; }
    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }
    public DateTime UploadedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }

    public User UploadedByUser { get; set; } = null!;
    public Project? Project { get; set; }
    public TaskItem? Task { get; set; }
    public ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
    public ICollection<DocumentActivity> Activities { get; set; } = new List<DocumentActivity>();
}

public class DocumentShare
{
    [Key]
    public int DocumentShareId { get; set; }
    public int DocumentId { get; set; }
    public int SharedWithUserId { get; set; }
    public DateTime SharedUtc { get; set; } = DateTime.UtcNow;
    public Document Document { get; set; } = null!;
    public User SharedWithUser { get; set; } = null!;
}

public class DocumentActivity
{
    [Key]
    public int DocumentActivityId { get; set; }
    public int DocumentId { get; set; }
    [Required, MaxLength(50)]
    public string ActivityType { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;
    public Document Document { get; set; } = null!;
    public User User { get; set; } = null!;
}
