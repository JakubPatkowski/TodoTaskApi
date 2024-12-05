using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Core.Entities;

namespace TodoTaskAPI.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task Initialize(ApplicationDbContext context, bool seedTestData = false)
    {
        await context.Database.EnsureCreatedAsync();

        // Only seed test data if explicitly requested and no data exists
        if (seedTestData && !context.Todos.Any())
        {
            var todos = new[]
            {
                new Todo
                {
                    Id = Guid.NewGuid(),
                    Title = "Dokończyć projekt",
                    Description = "Implementacja REST API w .NET",
                    ExpiryDateTime = DateTime.UtcNow.AddDays(7),
                    PercentComplete = 30,
                    IsDone = false,
                    CreatedAt = DateTime.UtcNow
                },
                new Todo
                {
                    Id = Guid.NewGuid(),
                    Title = "Code review",
                    Description = "Przejrzeć pull requesty",
                    ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                    PercentComplete = 0,
                    IsDone = false,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.Todos.AddRangeAsync(todos);
            await context.SaveChangesAsync();
        }
    }
}

