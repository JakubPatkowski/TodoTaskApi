using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TodoTaskAPI.Core.Entities;
using TodoTaskAPI.Infrastructure.Data;
using TodoTaskAPI.Infrastructure.Repositories;
using Xunit;

namespace TodoTaskAPI.UnitTests.Repositories
{
    public class TodoRepositoryTimePeriodTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TodoRepository _repository;
        private readonly Mock<ILogger<TodoRepository>> _mockLogger;

        public TodoRepositoryTimePeriodTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _mockLogger = new Mock<ILogger<TodoRepository>>();
            _repository = new TodoRepository(_context, _mockLogger.Object);
        }

        [Fact]
        public async Task GetTodosByDateRange_ReturnsCorrectTodos()
        {
            // Arrange
            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(7);

            var todos = new[]
            {
                new Todo { ExpiryDateTime = startDate.AddDays(1) },
                new Todo { ExpiryDateTime = startDate.AddDays(3) },
                new Todo { ExpiryDateTime = startDate.AddDays(10) } // Outside range
            };

            await _context.Todos.AddRangeAsync(todos);
            await _context.SaveChangesAsync();

            // Act
            var result = (await _repository.GetTodosByDateRangeAsync(startDate, endDate)).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, todo =>
                Assert.True(todo.ExpiryDateTime.Date >= startDate &&
                          todo.ExpiryDateTime.Date <= endDate));
        }

        [Fact]
        public async Task GetTodosByDateRange_OrdersByExpiryDate()
        {
            // Arrange
            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(7);

            var todos = new[]
            {
                new Todo { ExpiryDateTime = startDate.AddDays(3) },
                new Todo { ExpiryDateTime = startDate.AddDays(1) },
                new Todo { ExpiryDateTime = startDate.AddDays(2) }
            };

            await _context.Todos.AddRangeAsync(todos);
            await _context.SaveChangesAsync();

            // Act
            var result = (await _repository.GetTodosByDateRangeAsync(startDate, endDate)).ToList();

            // Assert
            Assert.Equal(3, result.Count);
            for (int i = 0; i < result.Count - 1; i++)
            {
                Assert.True(result[i].ExpiryDateTime <= result[i + 1].ExpiryDateTime);
            }
        }

        [Fact]
        public async Task GetTodosByDateRange_WithNoTodos_ReturnsEmptyList()
        {
            // Arrange
            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(7);

            // Act
            var result = await _repository.GetTodosByDateRangeAsync(startDate, endDate);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetTodosByDateRange_IncludesEndDateTodos()
        {
            // Arrange
            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(1);

            var todos = new[]
            {
                new Todo { ExpiryDateTime = endDate }, // Should be included
                new Todo { ExpiryDateTime = endDate.AddDays(1) } // Should not be included
            };

            await _context.Todos.AddRangeAsync(todos);
            await _context.SaveChangesAsync();

            // Act
            var result = (await _repository.GetTodosByDateRangeAsync(startDate, endDate)).ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal(endDate.Date, result[0].ExpiryDateTime.Date);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}