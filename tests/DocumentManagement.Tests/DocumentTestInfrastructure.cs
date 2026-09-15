using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;

namespace DocumentManagement.Tests;

internal static class TestDatabase
{
    public static async Task<TestDatabaseScope> CreateAsync()
    {
        var databaseName = $"ContosoDashboardTests_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer($@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=true")
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[DocumentActivities]', N'U') IS NOT NULL DROP TABLE [DocumentActivities];
            IF OBJECT_ID(N'[DocumentShares]', N'U') IS NOT NULL DROP TABLE [DocumentShares];
            IF OBJECT_ID(N'[Documents]', N'U') IS NOT NULL DROP TABLE [Documents];
            """);
        await DocumentSchemaInitializer.EnsureCreatedAsync(context);
        return new TestDatabaseScope(context);
    }
}

internal sealed class TestDatabaseScope(ApplicationDbContext context) : IAsyncDisposable
{
    public ApplicationDbContext Context { get; } = context;

    public async ValueTask DisposeAsync()
    {
        await Context.Database.EnsureDeletedAsync();
        await Context.DisposeAsync();
    }
}

internal sealed class TestBrowserFile(string name, long size, string contentType, byte[] content) : IBrowserFile
{
    public string Name { get; } = name;
    public DateTimeOffset LastModified { get; } = DateTimeOffset.UtcNow;
    public long Size { get; } = size;
    public string ContentType { get; } = contentType;

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        if (Size > maxAllowedSize)
            throw new IOException("The file exceeds the configured limit.");
        return new MemoryStream(content, writable: false);
    }
}

internal sealed class TestStorage : IFileStorageService
{
    private readonly ConcurrentDictionary<string, byte[]> files = new();
    private int sequence;
    public List<string> Deleted { get; } = [];
    public bool FailCommit { get; set; }

    public async Task<string> StageAsync(Stream content, string extension, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        var reference = $"staging/{Interlocked.Increment(ref sequence)}{extension}";
        files[reference] = memory.ToArray();
        return reference;
    }

    public Task<string> CommitAsync(string stagedReference, CancellationToken cancellationToken)
    {
        if (FailCommit)
            throw new IOException("Simulated promotion failure.");
        var finalReference = stagedReference.Replace("staging/", "files/", StringComparison.Ordinal);
        if (!files.TryRemove(stagedReference, out var content))
            throw new IOException("Missing staged file.");
        files[finalReference] = content;
        return Task.FromResult(finalReference);
    }

    public Task DeleteAsync(string reference, CancellationToken cancellationToken)
    {
        files.TryRemove(reference, out _);
        Deleted.Add(reference);
        return Task.CompletedTask;
    }

    public Task<Stream> OpenReadAsync(string reference, CancellationToken cancellationToken)
    {
        return Task.FromResult<Stream>(new MemoryStream(files[reference], writable: false));
    }

    public IReadOnlyCollection<string> References => files.Keys.ToArray();
}

internal sealed class TestSafetyValidator(bool result) : IUploadSafetyValidator
{
    public Task<bool> ValidateAsync(Stream content, CancellationToken cancellationToken) => Task.FromResult(result);
}

internal sealed class TestAuthorization : IDocumentAuthorizationService
{
    public Task<bool> CanAccessAsync(int userId, Document document, CancellationToken cancellationToken) =>
        Task.FromResult(document.UploadedByUserId == userId);
}

internal static class TestUploads
{
    public static (IBrowserFile File, DocumentUploadRequest Metadata) Create(string name = "document.txt", long size = 10, string category = "Reports")
    {
        return (new TestBrowserFile(name, size, "text/plain", new byte[Math.Min(size, 1024)]),
            new DocumentUploadRequest("Training document", null, category, null, null));
    }
}
