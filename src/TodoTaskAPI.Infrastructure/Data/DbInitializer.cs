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
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing")
        {
            return;
        }

        await context.Database.EnsureCreatedAsync();

        // check if data exists in database
        if (!context.Todos.Any())
        {
            var utcNow = DateTime.UtcNow;
            var todos = new[]
            {
                // Today
                new Todo
                {
                    Id = Guid.NewGuid(),
                    Title = "Today's Meeting",
                    Description = "Team sync meeting",
                    ExpiryDateTime = utcNow.Date.AddHours(14),
                    PercentComplete = 0,
                    IsDone = false,
                    CreatedAt = utcNow
                },
                new Todo
                {
                    Id = Guid.NewGuid(),
                    Title = "Today's Deadline",
                    Description = "Submit project proposal",
                    ExpiryDateTime = utcNow.Date.AddHours(17), // Dzisiaj o 17:00
                    PercentComplete = 75,
                    IsDone = false,
                    CreatedAt = utcNow
                },

                // Tomorrow
                new Todo
                {
                    Id = Guid.NewGuid(),
                    Title = "Tomorrow's Presentation",
                    Description = "Client presentation preparation",
                    ExpiryDateTime = utcNow.AddDays(1).Date.AddHours(10), // Jutro o 10:00
                    PercentComplete = 50,
                    IsDone = false,
                    CreatedAt = utcNow
                },
                new Todo
                {
                    Id = Guid.NewGuid(),
                    Title = "Tomorrow's Report",
                    Description = "Monthly report submission",
                    ExpiryDateTime = utcNow.AddDays(1).Date.AddHours(16), // Jutro o 16:00
                    PercentComplete = 25,
                    IsDone = false,
                    CreatedAt = utcNow
                },

                // This week
                new Todo
                {
                    Id = Guid.NewGuid(),
                    Title = "Week's Project Review",
                    Description = "Review project milestones",
                    ExpiryDateTime = utcNow.AddDays(3).Date.AddHours(14), // Za 3 dni
                    PercentComplete = 30,
                    IsDone = false,
                    CreatedAt = utcNow
                },
                new Todo
                {
                    Id = Guid.NewGuid(),
                    Title = "Team Meeting",
                    Description = "Weekly team sync",
                    ExpiryDateTime = utcNow.AddDays(5).Date.AddHours(11), // Za 5 dni
                    PercentComplete = 0,
                    IsDone = false,
                    CreatedAt = utcNow
                },

                // Next Week (Custom range)
                new Todo
                {
                    Id = Guid.NewGuid(),
                    Title = "Next Week Planning",
                    Description = "Plan next sprint",
                    ExpiryDateTime = utcNow.AddDays(8).Date.AddHours(10), // Za 8 dni
                    PercentComplete = 0,
                    IsDone = false,
                    CreatedAt = utcNow
                },
                new Todo
                {
                    Id = Guid.NewGuid(),
                    Title = "Monthly Review",
                    Description = "Monthly performance review",
                    ExpiryDateTime = utcNow.AddDays(10).Date.AddHours(15), // Za 10 dni
                    PercentComplete = 0,
                    IsDone = false,
                    CreatedAt = utcNow
                },

                new Todo
                {
                    Id = Guid.NewGuid(),
                    Title = "Completed Task",
                    Description = "This task is done",
                    ExpiryDateTime = utcNow.AddDays(2).Date.AddHours(12),
                    PercentComplete = 100,
                    IsDone = true,
                    CreatedAt = utcNow
                },
                new Todo
                {
                    Id = Guid.NewGuid(),
                    Title = "In Progress Task",
                    Description = "This task is half done",
                    ExpiryDateTime = utcNow.AddDays(4).Date.AddHours(16),
                    PercentComplete = 50,
                    IsDone = false,
                    CreatedAt = utcNow
                }
            };

            await context.Todos.AddRangeAsync(todos);
            await context.SaveChangesAsync();
        }
    }
}

