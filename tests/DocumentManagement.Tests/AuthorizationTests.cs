using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Models;
using ContosoDashboard.Services;

namespace DocumentManagement.Tests;

public sealed class AuthorizationTests
{
    [Fact]
    public async Task Owner_can_list_and_download_but_unrelated_user_cannot()
    {
        await using var database = await TestDatabase.CreateAsync();
        var context = database.Context;
        var storage = new TestStorage();
        var service = new DocumentService(context, storage, new TestSafetyValidator(true), new TestAuthorization());
        Assert.Empty(await service.UploadBatchAsync(4, [TestUploads.Create()]));

        var document = await context.Documents.SingleAsync();
        Assert.Single(await service.GetMyDocumentsAsync(4));
        Assert.Empty(await service.GetMyDocumentsAsync(3));
        Assert.NotNull(await service.OpenDownloadAsync(4, document.DocumentId));
        Assert.Null(await service.OpenDownloadAsync(3, document.DocumentId));
    }
}
