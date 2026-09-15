using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Data;

public static class DocumentSchemaInitializer
{
    public static async Task EnsureCreatedAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'[Documents]', N'U') IS NULL
BEGIN
    CREATE TABLE [Documents] (
        [DocumentId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Documents] PRIMARY KEY,
        [Title] nvarchar(255) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [Category] nvarchar(100) NOT NULL,
        [Tags] nvarchar(2000) NULL,
        [OriginalFileName] nvarchar(255) NOT NULL,
        [FileType] nvarchar(255) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [StorageReference] nvarchar(500) NOT NULL,
        [Status] int NOT NULL,
        [UploadedByUserId] int NOT NULL,
        [ProjectId] int NULL,
        [TaskId] int NULL,
        [UploadedUtc] datetime2 NOT NULL,
        [UpdatedUtc] datetime2 NULL,
        CONSTRAINT [FK_Documents_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users]([UserId]),
        CONSTRAINT [FK_Documents_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([ProjectId]),
        CONSTRAINT [FK_Documents_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks]([TaskId])
    );
    CREATE INDEX [IX_Documents_UploadedByUserId_Status_UploadedUtc] ON [Documents] ([UploadedByUserId], [Status], [UploadedUtc]);
    CREATE INDEX [IX_Documents_ProjectId_Status] ON [Documents] ([ProjectId], [Status]);
END
IF OBJECT_ID(N'[DocumentShares]', N'U') IS NULL
BEGIN
    CREATE TABLE [DocumentShares] (
        [DocumentShareId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_DocumentShares] PRIMARY KEY,
        [DocumentId] int NOT NULL,
        [SharedWithUserId] int NOT NULL,
        [SharedUtc] datetime2 NOT NULL,
        CONSTRAINT [FK_DocumentShares_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents]([DocumentId]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentShares_Users_SharedWithUserId] FOREIGN KEY ([SharedWithUserId]) REFERENCES [Users]([UserId])
    );
END
IF OBJECT_ID(N'[DocumentActivities]', N'U') IS NULL
BEGIN
    CREATE TABLE [DocumentActivities] (
        [DocumentActivityId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_DocumentActivities] PRIMARY KEY,
        [DocumentId] int NOT NULL,
        [ActivityType] nvarchar(50) NOT NULL,
        [UserId] int NOT NULL,
        [OccurredUtc] datetime2 NOT NULL,
        CONSTRAINT [FK_DocumentActivities_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents]([DocumentId]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentActivities_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([UserId])
    );
END
""");
    }
}
