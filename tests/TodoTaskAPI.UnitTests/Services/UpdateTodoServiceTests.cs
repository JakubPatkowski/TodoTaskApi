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
using TodoTaskAPI.Core.Interfaces;

namespace TodoTaskAPI.UnitTests.Services
{
    /// <summary>
    /// Unit tests specifically focused on the Update Todo service logic
    /// Verifies business rules, validation, and service layer behavior
    /// </summary>
    public class UpdateTodoServiceTests
    {
        private readonly Mock<ITodoRepository> _mockRepository;
        private readonly Mock<ILogger<TodoService>> _mockLogger;
        private readonly TodoService _service;

        public UpdateTodoServiceTests()
        {
            _mockRepository = new Mock<ITodoRepository>();
            _mockLogger = new Mock<ILogger<TodoService>>();
            _service = new TodoService(_mockRepository.Object, _mockLogger.Object);
        }

        /// <summary>
        /// Verifies that service correctly updates all fields when provided
        /// Tests the complete mapping and update flow at service level
        /// </summary>
        [Fact]
        public async Task UpdateTodoAsync_WithAllFields_UpdatesSuccessfully()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var existingTodo = new Todo
            {
                Id = todoId,
                Title = "Original Title",
                Description = "Original Description",
                ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                PercentComplete = 0,
                IsDone = false
            };

            var updateDto = new UpdateTodoDto
            {
                Title = "Updated Title",
                Description = "Updated Description",
                ExpiryDateTime = DateTime.UtcNow.AddDays(2),
                PercentComplete = 50,
                IsDone = false
            };

            _mockRepository.Setup(r => r.GetByIdAsync(todoId))
                .ReturnsAsync(existingTodo);
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Todo>()))
                .ReturnsAsync((Todo t) => t);

            // Act
            var result = await _service.UpdateTodoAsync(todoId, updateDto);

            // Assert
            Assert.Equal(updateDto.Title, result.Title);
            Assert.Equal(updateDto.Description, result.Description);
            Assert.Equal(updateDto.PercentComplete, result.PercentComplete);
            _mockRepository.Verify(r => r.UpdateAsync(It.Is<Todo>(t =>
                t.Title == updateDto.Title &&
                t.Description == updateDto.Description)),
                Times.Once);
        }

        /// <summary>
        /// Verifies that the service handles DateTime normalization correctly
        /// Tests time zone conversion and validation at service level
        /// </summary>
        [Fact]
        public async Task UpdateTodoAsync_WithNewExpiryDate_NormalizesToUtc()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var existingTodo = new Todo
            {
                Id = todoId,
                Title = "Test Todo",
                ExpiryDateTime = DateTime.UtcNow.AddDays(1)
            };

            var localDateTime = DateTime.Now.AddDays(2);
            var updateDto = new UpdateTodoDto
            {
                ExpiryDateTime = localDateTime
            };

            _mockRepository.Setup(r => r.GetByIdAsync(todoId))
                .ReturnsAsync(existingTodo);
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Todo>()))
                .ReturnsAsync((Todo t) => t);

            // Act
            var result = await _service.UpdateTodoAsync(todoId, updateDto);

            // Assert - verify date was converted to UTC
            Assert.Equal(localDateTime.ToUniversalTime(), result.ExpiryDateTime);
        }

    }
}
