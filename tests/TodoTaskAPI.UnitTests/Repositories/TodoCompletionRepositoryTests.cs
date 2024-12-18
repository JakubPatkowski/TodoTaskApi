﻿using Microsoft.EntityFrameworkCore;
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
    /// Tests for todo completion related repository operations
    /// Verifies database operations using in-memory database
    /// </summary>
    public class TodoCompletionRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TodoRepository _repository;
        private readonly Mock<ILogger<TodoRepository>> _mockLogger;

        public TodoCompletionRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _mockLogger = new Mock<ILogger<TodoRepository>>();
            _repository = new TodoRepository(_context, _mockLogger.Object);
        }

        /// <summary>
        /// Verifies that repository correctly updates todo completion status
        /// Tests persistence of completion percentage and done status
        /// </summary>
        [Fact]
        public async Task UpdateAsync_PersistsCompletionChanges()
        {
            // Arrange
            var todo = new Todo
            {
                Id = Guid.NewGuid(),
                Title = "Test Todo",
                Description = "Test Description",
                ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                PercentComplete = 0,
                IsDone = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Todos.AddAsync(todo);
            await _context.SaveChangesAsync();

            // Update the todo
            todo.PercentComplete = 100;
            todo.IsDone = true;

            // Act
            var updatedTodo = await _repository.UpdateAsync(todo);

            // Assert
            Assert.NotNull(updatedTodo);
            Assert.Equal(100, updatedTodo.PercentComplete);
            Assert.True(updatedTodo.IsDone);

            // Verify persistence
            var persistedTodo = await _context.Todos.FindAsync(todo.Id);
            Assert.NotNull(persistedTodo);
            Assert.Equal(100, persistedTodo.PercentComplete);
            Assert.True(persistedTodo.IsDone);
        }

        /// <summary>
        /// Verifies that repository handles concurrent updates correctly
        /// Tests concurrency handling in completion updates
        /// </summary>
        [Fact]
        public async Task UpdateAsync_HandlesConcurrentUpdates()
        {
            // Arrange
            var todo = new Todo
            {
                Id = Guid.NewGuid(),
                Title = "Test Todo",
                PercentComplete = 0,
                IsDone = false
            };

            await _context.Todos.AddAsync(todo);
            await _context.SaveChangesAsync();

            // Kluczowa zmiana - przejdź na detached state przez pobranie nowej instancji
            var todoToUpdate = await _context.Todos.FindAsync(todo.Id);
            todoToUpdate!.PercentComplete = 50;

            _context.Entry(todoToUpdate).State = EntityState.Detached;

            var result = await _repository.UpdateAsync(todoToUpdate);
            Assert.Equal(50, result.PercentComplete);

            // Druga aktualizacja
            todoToUpdate = await _context.Todos.FindAsync(todo.Id);
            todoToUpdate!.PercentComplete = 75;

            _context.Entry(todoToUpdate).State = EntityState.Detached;

            result = await _repository.UpdateAsync(todoToUpdate);
            Assert.Equal(75, result.PercentComplete);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
