using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Core.Entities;
using TodoTaskAPI.Infrastructure.Data;
using TodoTaskAPI.Infrastructure.Repositories;

namespace TodoTaskAPI.UnitTests.Repositories
{
    /// <summary>
    /// Unit tests for Todo repository update operations
    /// Tests data access and persistence logic using in-memory database
    /// </summary>
    public class UpdateTodoRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TodoRepository _repository;
        private readonly Mock<ILogger<TodoRepository>> _mockLogger;

        public UpdateTodoRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _mockLogger = new Mock<ILogger<TodoRepository>>();
            _repository = new TodoRepository(_context, _mockLogger.Object);
        }

        /// <summary>
        /// Verifies that repository correctly persists updates
        /// Tests the actual database update operation
        /// </summary>
        [Fact]
        public async Task UpdateAsync_PersistedSuccessfully()
        {
            // Arrange
            var todo = new Todo
            {
                Id = Guid.NewGuid(),
                Title = "Original Title",
                Description = "Original Description",
                ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                PercentComplete = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Todos.AddAsync(todo);
            await _context.SaveChangesAsync();

            // Modify the todo
            todo.Title = "Updated Title";
            todo.Description = "Updated Description";

            // Act
            var updatedTodo = await _repository.UpdateAsync(todo);

            // Assert
            Assert.NotNull(updatedTodo);
            var persistedTodo = await _context.Todos.FindAsync(todo.Id);
            Assert.NotNull(persistedTodo);
            Assert.Equal("Updated Title", persistedTodo.Title);
            Assert.Equal("Updated Description", persistedTodo.Description);
            Assert.NotNull(persistedTodo.UpdatedAt);
        }

        /// <summary>
        /// Verifies that UpdatedAt timestamp is set correctly
        /// Tests automatic timestamp update functionality
        /// </summary>
        [Fact]
        public async Task UpdateAsync_SetsUpdatedAtTimestamp()
        {
            // Arrange
            var todo = new Todo
            {
                Id = Guid.NewGuid(),
                Title = "Test Todo",
                ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Todos.AddAsync(todo);
            await _context.SaveChangesAsync();

            var beforeUpdate = DateTime.UtcNow;
            todo.Title = "Updated Title";

            // Act
            var updatedTodo = await _repository.UpdateAsync(todo);

            // Assert
            Assert.NotNull(updatedTodo.UpdatedAt);
            Assert.True(updatedTodo.UpdatedAt >= beforeUpdate);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
