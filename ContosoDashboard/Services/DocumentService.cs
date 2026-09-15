using System.Security.Claims;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed record DocumentUploadRequest(string Title, string? Description, string Category, string? Tags, int? ProjectId);
public sealed record DocumentListItem(int DocumentId, string Title, string Category, DateTime UploadedUtc, long FileSizeBytes, string? ProjectName);
public sealed record DocumentDownload(Stream Content, string ContentType, string FileName);

public interface IFileStorageService
{
    Task<string> StageAsync(Stream content, string extension, CancellationToken cancellationToken);
    Task<string> CommitAsync(string stagedReference, CancellationToken cancellationToken);
    Task DeleteAsync(string reference, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string reference, CancellationToken cancellationToken);
}

public interface IUploadSafetyValidator
{
    Task<bool> ValidateAsync(Stream content, CancellationToken cancellationToken);
}

public interface IDocumentAuthorizationService
{
    Task<bool> CanAccessAsync(int userId, Document document, CancellationToken cancellationToken);
}

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentListItem>> GetMyDocumentsAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> UploadBatchAsync(int userId, IReadOnlyList<(IBrowserFile File, DocumentUploadRequest Metadata)> uploads, CancellationToken cancellationToken = default);
    Task<DocumentDownload?> OpenDownloadAsync(int userId, int documentId, CancellationToken cancellationToken = default);
}

public sealed class TrainingUploadSafetyValidator : IUploadSafetyValidator
{
    public async Task<bool> ValidateAsync(Stream content, CancellationToken cancellationToken)
    {
        // This is a training-only validation simulation, not genuine malware detection.
        await Task.Yield();
        return content.CanRead;
    }
}

public sealed class DocumentAuthorizationService(ApplicationDbContext context) : IDocumentAuthorizationService
{
    public async Task<bool> CanAccessAsync(int userId, Document document, CancellationToken cancellationToken)
    {
        if (document.UploadedByUserId == userId) return true;
        if (document.ProjectId is null) return false;

        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user?.Role == UserRole.Administrator) return true;
        return await context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId &&
            (p.ProjectManagerId == userId || p.ProjectMembers.Any(m => m.UserId == userId)), cancellationToken);
    }
}

public sealed class DocumentService(
    ApplicationDbContext context,
    IFileStorageService storage,
    IUploadSafetyValidator safety,
    IDocumentAuthorizationService authorization) : IDocumentService
{
    private const long MaxFileSize = 25_000_000;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".jpg", ".jpeg", ".png" };
    private static readonly HashSet<string> Categories = new(StringComparer.Ordinal)
        { "Project Documents", "Team Resources", "Personal Files", "Reports", "Presentations", "Other" };

    public async Task<IReadOnlyList<DocumentListItem>> GetMyDocumentsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await context.Documents.AsNoTracking()
            .Where(d => d.Status == DocumentStatus.Ready &&
                (d.UploadedByUserId == userId ||
                 (d.ProjectId != null && (d.Project!.ProjectManagerId == userId || d.Project.ProjectMembers.Any(m => m.UserId == userId)))))
            .OrderByDescending(d => d.UploadedUtc)
            .Select(d => new DocumentListItem(d.DocumentId, d.Title, d.Category, d.UploadedUtc, d.FileSizeBytes, d.Project == null ? null : d.Project.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> UploadBatchAsync(int userId, IReadOnlyList<(IBrowserFile File, DocumentUploadRequest Metadata)> uploads, CancellationToken cancellationToken = default)
    {
        if (uploads.Count == 0) return ["Select at least one file."];

        var errors = new List<string>();
        var staged = new List<string>();
        var committed = new List<string>();
        var documents = new List<Document>();
        var committedSuccessfully = false;
        try
        {
            foreach (var upload in uploads)
            {
                var extension = Path.GetExtension(upload.File.Name);
                if (upload.File.Size > MaxFileSize) errors.Add($"{upload.File.Name}: maximum size is 25,000,000 bytes.");
                if (!AllowedExtensions.Contains(extension)) errors.Add($"{upload.File.Name}: file type is not supported.");
                if (string.IsNullOrWhiteSpace(upload.Metadata.Title) || !Categories.Contains(upload.Metadata.Category))
                    errors.Add($"{upload.File.Name}: title and category are required.");
                if (errors.Count > 0) continue;

                await using var source = upload.File.OpenReadStream(MaxFileSize, cancellationToken);
                await using var memory = new MemoryStream();
                await source.CopyToAsync(memory, cancellationToken);
                memory.Position = 0;
                if (!await safety.ValidateAsync(memory, cancellationToken))
                    errors.Add($"{upload.File.Name}: training-only safety simulation rejected the file; this is not genuine malware detection.");
                if (errors.Count > 0) continue;

                memory.Position = 0;
                var stagedReference = await storage.StageAsync(memory, extension, cancellationToken);
                staged.Add(stagedReference);
                documents.Add(new Document
                {
                    Title = upload.Metadata.Title.Trim(),
                    Description = upload.Metadata.Description,
                    Category = upload.Metadata.Category,
                    Tags = upload.Metadata.Tags,
                    OriginalFileName = Path.GetFileName(upload.File.Name),
                    FileType = string.IsNullOrWhiteSpace(upload.File.ContentType) ? "application/octet-stream" : upload.File.ContentType,
                    FileSizeBytes = upload.File.Size,
                    StorageReference = stagedReference,
                    Status = DocumentStatus.Pending,
                    UploadedByUserId = userId,
                    ProjectId = upload.Metadata.ProjectId,
                    UploadedUtc = DateTime.UtcNow
                });
            }

            if (errors.Count > 0) return errors;

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            for (var i = 0; i < documents.Count; i++)
            {
                documents[i].StorageReference = await storage.CommitAsync(staged[i], cancellationToken);
                committed.Add(documents[i].StorageReference);
                documents[i].Status = DocumentStatus.Ready;
                context.Documents.Add(documents[i]);
            }
            await context.SaveChangesAsync(cancellationToken);
            foreach (var document in documents)
                context.DocumentActivities.Add(new DocumentActivity { DocumentId = document.DocumentId, UserId = userId, ActivityType = "Upload" });
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            committedSuccessfully = true;
            return [];
        }
        catch (Exception ex) when (ex is IOException or DbUpdateException or InvalidOperationException)
        {
            errors.Add("Upload failed and was rolled back. No document was made visible.");
            return errors;
        }
        finally
        {
            foreach (var reference in committedSuccessfully ? Enumerable.Empty<string>() : staged.Concat(committed).Distinct())
            {
                await storage.DeleteAsync(reference, cancellationToken);
            }
        }
    }

    public async Task<DocumentDownload?> OpenDownloadAsync(int userId, int documentId, CancellationToken cancellationToken = default)
    {
        var document = await context.Documents.AsNoTracking().Include(d => d.Project).ThenInclude(p => p!.ProjectMembers)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.Status == DocumentStatus.Ready, cancellationToken);
        if (document is null || !await authorization.CanAccessAsync(userId, document, cancellationToken)) return null;
        var content = await storage.OpenReadAsync(document.StorageReference, cancellationToken);
        return new DocumentDownload(content, document.FileType, document.OriginalFileName);
    }
}

public sealed class LocalFileStorageService(IWebHostEnvironment environment) : IFileStorageService
{
    private string Root => Path.Combine(environment.ContentRootPath, "AppData", "uploads");

    public async Task<string> StageAsync(Stream content, string extension, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Root);
        var reference = Path.Combine("staging", $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");
        var path = Path.Combine(Root, reference);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var output = File.Create(path);
        await content.CopyToAsync(output, cancellationToken);
        return reference.Replace('\\', '/');
    }

    public async Task<string> CommitAsync(string stagedReference, CancellationToken cancellationToken)
    {
        var finalReference = Path.Combine("files", $"{Guid.NewGuid():N}{Path.GetExtension(stagedReference)}").Replace('\\', '/');
        var source = Path.Combine(Root, stagedReference.Replace('/', Path.DirectorySeparatorChar));
        var destination = Path.Combine(Root, finalReference.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Move(source, destination);
        await Task.CompletedTask;
        return finalReference;
    }

    public Task DeleteAsync(string reference, CancellationToken cancellationToken)
    {
        var path = Path.Combine(Root, reference.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<Stream> OpenReadAsync(string reference, CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(Root);
        var path = Path.GetFullPath(Path.Combine(Root, reference.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid storage reference.");
        return Task.FromResult<Stream>(File.OpenRead(path));
    }
}
