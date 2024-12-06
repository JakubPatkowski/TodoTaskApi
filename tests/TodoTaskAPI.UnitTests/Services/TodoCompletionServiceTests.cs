using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.DTOs;
using TodoTaskAPI.Application.Services;
using TodoTaskAPI.Core.Entities;
using TodoTaskAPI.Core.Exceptions;
using TodoTaskAPI.Core.Interfaces;

namespace TodoTaskAPI.UnitTests.Services
{
    /// <summary>
    /// Tests for todo completion related service methods
    /// Verifies business logic for completion and done status updates
    /// </summary>
    public class TodoCompletionServiceTests
    {
        private readonly Mock<ITodoRepository> _mockRepository;
        private readonly Mock<ILogger<TodoService>> _mockLogger;
        private readonly TodoService _service;

        public TodoCompletionServiceTests()
        {
            _mockRepository = new Mock<ITodoRepository>();
            _mockLogger = new Mock<ILogger<TodoService>>();
            _service = new TodoService(_mockRepository.Object, _mockLogger.Object);
        }

        /// <summary>
        /// Verifies that completing a todo updates both percentage and done status correctly
        /// Tests the automatic done status update when reaching 100%
        /// </summary>
        [Theory]
        [InlineData(100, true)]  // Should be marked as done
        [InlineData(99, false)]  // Should not be marked as done
        public async Task UpdateTodoCompletionAsync_UpdatesStatusCorrectly(int percentage, bool expectedIsDone)
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var existingTodo = new Todo
            {
                Id = todoId,
                Title = "Test Todo",
                PercentComplete = 0,
                IsDone = false
            };

            var updateDto = new UpdateTodoCompletionDto { PercentComplete = percentage };

            _mockRepository.Setup(r => r.GetByIdAsync(todoId))
                .ReturnsAsync(existingTodo);
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Todo>()))
                .ReturnsAsync((Todo t) => t);

            // Act
            var result = await _service.UpdateTodoCompletionAsync(todoId, updateDto);

            // Assert
            Assert.Equal(percentage, result.PercentComplete);
            Assert.Equal(expectedIsDone, result.IsDone);
            _mockRepository.Verify(r => r.UpdateAsync(
                It.Is<Todo>(t =>
                    t.PercentComplete == percentage &&
                    t.IsDone == expectedIsDone)),
                Times.Once);
        }

        /// <summary>
        /// Verifies that marking a todo as done updates completion percentage correctly
        /// Tests the automatic percentage update when changing done status
        /// </summary>
        [Theory]
        [InlineData(true, 100)]   // Should set to 100%
        [InlineData(false, 0)]    // Should reset to 0%
        public async Task UpdateTodoDoneStatusAsync_UpdatesPercentageCorrectly(bool isDone, int expectedPercentage)
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var existingTodo = new Todo
            {
                Id = todoId,
                Title = "Test Todo",
                PercentComplete = 50,  // Start at 50%
                IsDone = !isDone       // Start with opposite status
            };

            var updateDto = new UpdateTodoDoneStatusDto { IsDone = isDone };

            _mockRepository.Setup(r => r.GetByIdAsync(todoId))
                .ReturnsAsync(existingTodo);
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Todo>()))
                .ReturnsAsync((Todo t) => t);

            // Act
            var result = await _service.UpdateTodoDoneStatusAsync(todoId, updateDto);

            // Assert
            Assert.Equal(expectedPercentage, result.PercentComplete);
            Assert.Equal(isDone, result.IsDone);
            _mockRepository.Verify(r => r.UpdateAsync(
                It.Is<Todo>(t =>
                    t.PercentComplete == expectedPercentage &&
                    t.IsDone == isDone)),
                Times.Once);
        }

        /// <summary>
        /// Verifies that attempting to update a non-existent todo throws NotFoundException
        /// Tests error handling for missing resources
        /// </summary>
        [Fact]
        public async Task UpdateTodoCompletionAsync_WithNonExistentId_ThrowsNotFoundException()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var updateDto = new UpdateTodoCompletionDto { PercentComplete = 50 };

            _mockRepository.Setup(r => r.GetByIdAsync(todoId))
                .ReturnsAsync((Todo?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.UpdateTodoCompletionAsync(todoId, updateDto));
        }

        /// <summary>
        /// Verifies that repository exceptions are properly propagated
        /// Tests error handling for database failures
        /// </summary>
        [Fact]
        public async Task UpdateTodoDoneStatusAsync_WithRepositoryException_PropagatesException()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var updateDto = new UpdateTodoDoneStatusDto { IsDone = true };
            var expectedException = new Exception("Database error");

            _mockRepository.Setup(r => r.GetByIdAsync(todoId))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _service.UpdateTodoDoneStatusAsync(todoId, updateDto));
            Assert.Same(expectedException, exception);
        }
    }
}
