using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Services;

namespace DocumentManagement.Tests;

public sealed class UploadTests
{
    [Fact]
    public async Task Accepts_exactly_25000000_bytes()
    {
        await using var database = await TestDatabase.CreateAsync();
        var context = database.Context;
        var storage = new TestStorage();
        var service = new DocumentService(context, storage, new TestSafetyValidator(true), new TestAuthorization());
        var upload = TestUploads.Create(size: 25_000_000);

        var errors = await service.UploadBatchAsync(4, [upload]);

        Assert.Empty(errors);
        Assert.Single(await service.GetMyDocumentsAsync(4));
        Assert.Single(await context.Documents.ToListAsync());
        Assert.Equal(25_000_000, (await context.Documents.SingleAsync()).FileSizeBytes);
    }

    [Fact]
    public async Task Rejects_25000001_bytes_without_persisting_or_staging()
    {
        await using var database = await TestDatabase.CreateAsync();
        var context = database.Context;
        var storage = new TestStorage();
        var service = new DocumentService(context, storage, new TestSafetyValidator(true), new TestAuthorization());

        var errors = await service.UploadBatchAsync(4, [TestUploads.Create(size: 25_000_001)]);

        Assert.Contains(errors, error => error.Contains("25,000,000", StringComparison.Ordinal));
        Assert.Empty(await context.Documents.ToListAsync());
        Assert.Empty(storage.References);
    }

    [Fact]
    public async Task Rejects_unsupported_type_and_invalid_metadata()
    {
        await using var database = await TestDatabase.CreateAsync();
        var context = database.Context;
        var storage = new TestStorage();
        var service = new DocumentService(context, storage, new TestSafetyValidator(true), new TestAuthorization());

        var errors = await service.UploadBatchAsync(4, [TestUploads.Create("payload.exe", category: "Not a category")]);

        Assert.Contains(errors, error => error.Contains("not supported", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("title and category", StringComparison.Ordinal));
        Assert.Empty(await context.Documents.ToListAsync());
    }

    [Fact]
    public async Task Fails_closed_with_training_only_safety_message()
    {
        await using var database = await TestDatabase.CreateAsync();
        var context = database.Context;
        var storage = new TestStorage();
        var service = new DocumentService(context, storage, new TestSafetyValidator(false), new TestAuthorization());

        var errors = await service.UploadBatchAsync(4, [TestUploads.Create()]);

        Assert.Contains(errors, error => error.Contains("training-only safety simulation", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("not genuine malware detection", StringComparison.Ordinal));
        Assert.Empty(await context.Documents.ToListAsync());
        Assert.Empty(storage.References);
    }

    [Fact]
    public async Task Rolls_back_all_files_when_promotion_fails()
    {
        await using var database = await TestDatabase.CreateAsync();
        var context = database.Context;
        var storage = new TestStorage { FailCommit = true };
        var service = new DocumentService(context, storage, new TestSafetyValidator(true), new TestAuthorization());

        var errors = await service.UploadBatchAsync(4, [TestUploads.Create("one.txt"), TestUploads.Create("two.txt")]);

        Assert.Contains(errors, error => error.Contains("rolled back", StringComparison.Ordinal));
        Assert.Empty(await context.Documents.ToListAsync());
        Assert.Empty(await context.DocumentActivities.ToListAsync());
        Assert.Empty(storage.References);
        Assert.NotEmpty(storage.Deleted);
    }
}
