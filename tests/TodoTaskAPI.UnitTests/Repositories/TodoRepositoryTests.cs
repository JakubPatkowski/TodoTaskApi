using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.Services;
using TodoTaskAPI.Core.Entities;
using TodoTaskAPI.Infrastructure.Data;
using TodoTaskAPI.Infrastructure.Repositories;

namespace TodoTaskAPI.UnitTests.Repositories
{
    /// <summary>
    /// Unit tests for TodoRepository using in-memory database
    /// </summary>
    public class TodoRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TodoRepository _repository;
        private readonly Mock<ILogger<TodoRepository>> _mockLogger;


        public TodoRepositoryTests()
        {
            
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _mockLogger = new Mock<ILogger<TodoRepository>>();
            _repository = new TodoRepository(_context, _mockLogger.Object);
        }

        /// <summary>
        /// Verifies that GetAllAsync returns all todos ordered by expiry date
        /// </summary>
        [Fact]
        public async Task GetAllAsync_ReturnsAllTodosOrderedByExpiryDate()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            var todos = result.ToList();
            Assert.Equal(3, todos.Count);
            Assert.True(todos[0].ExpiryDateTime <= todos[1].ExpiryDateTime);
        }

        /// <summary>
        /// Verifies that GetAllWithPaginationAsync returns correct page of results with accurate total count
        /// </summary>
        [Fact]
        public async Task GetAllWithPaginationAsync_ReturnsPaginatedResults()
        {
            // Arrange
            await SeedTestData();
            var pageSize = 2;
            var pageNumber = 1;

            // Act
            var (items, totalCount) = await _repository.GetAllWithPaginationAsync(pageNumber, pageSize);

            // Assert
            Assert.Equal(2, items.Count());
            Assert.Equal(3, totalCount);
        }

        /// <summary>
        /// Seeds test data into the in-memory database
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task SeedTestData()
        {
            var todos = new[]
            {
                new Todo
                {
                    Id = Guid.NewGuid(),
                    Title = "First Todo",
                    ExpiryDateTime = DateTime.UtcNow.AddDays(3),
                    CreatedAt = DateTime.UtcNow
                },
                new Todo
                {
                    Id = Guid.NewGuid(),
                    Title = "Second Todo",
                    ExpiryDateTime = DateTime.UtcNow.AddDays(2),
                    CreatedAt = DateTime.UtcNow
                },
                new Todo
                {
                    Id = Guid.NewGuid(),
                    Title = "Third Todo",
                    ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                    CreatedAt = DateTime.UtcNow
                }
            };

            await _context.Todos.AddRangeAsync(todos);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Verifies that repository correctly sorts todos by expiry date in descending order
        /// </summary>
        [Fact]
        public async Task GetAllAsync_ReturnsTodosOrderedByExpiryDate()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var repository = new TodoRepository(context, _mockLogger.Object);

            var todos = new[]
            {
                new Todo { ExpiryDateTime = DateTime.UtcNow.AddDays(1) },
                new Todo { ExpiryDateTime = DateTime.UtcNow.AddDays(3) },
                new Todo { ExpiryDateTime = DateTime.UtcNow.AddDays(2) }
            };

            await context.Todos.AddRangeAsync(todos);
            await context.SaveChangesAsync();

            // Act
            var result = (await repository.GetAllAsync()).ToList();

            // Assert
            for (int i = 0; i < result.Count - 1; i++)
            {
                Assert.True(result[i].ExpiryDateTime <= result[i + 1].ExpiryDateTime);
            }
        }

        /// <summary>
        /// Verifies that repository handles pagination correctly when requested page is empty
        /// </summary>
        [Fact]
        public async Task GetAllWithPaginationAsync_WhenPageEmpty_ReturnsEmptyListWithCorrectTotal()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var repository = new TodoRepository(context, _mockLogger.Object);

            // Add only 5 items
            var todos = Enumerable.Range(1, 5).Select(i => new Todo
            {
                Title = $"Todo {i}",
                ExpiryDateTime = DateTime.UtcNow.AddDays(i)
            });

            await context.Todos.AddRangeAsync(todos);
            await context.SaveChangesAsync();

            // Act - Request page 4 with page size 2 (should be empty as we only have 5 items total)
            var (items, totalCount) = await repository.GetAllWithPaginationAsync(4, 2);

            // Assert
            Assert.Empty(items);
            Assert.Equal(5, totalCount);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
