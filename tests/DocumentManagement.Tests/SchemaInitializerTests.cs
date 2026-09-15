using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;

namespace DocumentManagement.Tests;

public sealed class SchemaInitializerTests
{
    [Fact]
    public async Task Additive_initializer_is_idempotent_and_preserves_seeded_rows_and_ids()
    {
        await using var database = await TestDatabase.CreateAsync();
        var context = database.Context;
        var users = await context.Users.AsNoTracking().Select(user => new { user.UserId, user.Email }).ToListAsync();
        var projects = await context.Projects.AsNoTracking().Select(project => new { project.ProjectId, project.Name }).ToListAsync();
        var tasks = await context.Tasks.AsNoTracking().Select(task => new { task.TaskId, task.Title }).ToListAsync();

        await DocumentSchemaInitializer.EnsureCreatedAsync(context);

        Assert.Equal(users, await context.Users.AsNoTracking().Select(user => new { user.UserId, user.Email }).ToListAsync());
        Assert.Equal(projects, await context.Projects.AsNoTracking().Select(project => new { project.ProjectId, project.Name }).ToListAsync());
        Assert.Equal(tasks, await context.Tasks.AsNoTracking().Select(task => new { task.TaskId, task.Title }).ToListAsync());

        var tables = await context.Database.SqlQueryRaw<string>(
            "SELECT TABLE_NAME AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME IN ('Documents','DocumentShares','DocumentActivities') ORDER BY TABLE_NAME")
            .ToListAsync();
        Assert.Equal(["DocumentActivities", "Documents", "DocumentShares"], tables);
    }
}
