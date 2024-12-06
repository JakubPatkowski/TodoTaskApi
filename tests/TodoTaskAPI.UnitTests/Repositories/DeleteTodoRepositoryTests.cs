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
    public class DeleteTodoRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TodoRepository _repository;
        private readonly Mock<ILogger<TodoRepository>> _mockLogger;

        public DeleteTodoRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _mockLogger = new Mock<ILogger<TodoRepository>>();
            _repository = new TodoRepository(_context, _mockLogger.Object);
        }

        [Fact]
        public async Task DeleteAsync_RemovesExistingTodo()
        {
            // Arrange
            var todo = new Todo { Id = Guid.NewGuid() };
            await _context.Todos.AddAsync(todo);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(todo);

            // Assert
            Assert.DoesNotContain(_context.Todos, t => t.Id == todo.Id);
        }

        [Fact]
        public async Task DeleteAsync_WithException_PropagatesException()
        {
            // Arrange
            _context.Database.EnsureDeleted();

            // Arrange
            _context.Database.EnsureDeleted();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _repository.DeleteAsync(new Todo()));
            Assert.IsType<Exception>(exception);
            Assert.Contains("Wystąpił błąd aktualizacji concurrency podczas usuwania todo.", exception.Message);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
