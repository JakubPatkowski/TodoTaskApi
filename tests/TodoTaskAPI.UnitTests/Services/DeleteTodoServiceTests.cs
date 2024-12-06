using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.Services;
using TodoTaskAPI.Core.Entities;
using TodoTaskAPI.Core.Exceptions;
using TodoTaskAPI.Core.Interfaces;

namespace TodoTaskAPI.UnitTests.Services
{
    public class DeleteTodoServiceTests
    {
        private readonly Mock<ITodoRepository> _mockRepository;
        private readonly Mock<ILogger<TodoService>> _mockLogger;
        private readonly TodoService _service;

        public DeleteTodoServiceTests()
        {
            _mockRepository = new Mock<ITodoRepository>();
            _mockLogger = new Mock<ILogger<TodoService>>();
            _service = new TodoService(_mockRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task DeleteTodoAsync_WithExistingTodo_RemovesSuccessfully()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var todo = new Todo { Id = todoId };

            _mockRepository.Setup(r => r.GetByIdAsync(todoId))
                .ReturnsAsync(todo);
            _mockRepository.Setup(r => r.DeleteAsync(todo))
                .Verifiable();

            // Act
            await _service.DeleteTodoAsync(todoId);

            // Assert
            _mockRepository.Verify(r => r.DeleteAsync(todo), Times.Once);
        }

        [Fact]
        public async Task DeleteTodoAsync_WithNonExistentTodo_ThrowsNotFoundException()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            _mockRepository.Setup(r => r.GetByIdAsync(todoId))
                .ReturnsAsync((Todo?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteTodoAsync(todoId));
        }

        [Fact]
        public async Task DeleteTodoAsync_WithException_PropagatesException()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var exception = new Exception("Nieoczekiwany błąd");

            _mockRepository.Setup(r => r.GetByIdAsync(todoId))
                .ReturnsAsync(new Todo { Id = todoId });
            _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<Todo>()))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.DeleteTodoAsync(todoId));
        }
    }
}
