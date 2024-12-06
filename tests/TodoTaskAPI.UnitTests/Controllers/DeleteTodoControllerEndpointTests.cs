using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.DTOs;
using TodoTaskAPI.Application.Interfaces;
using TodoTaskAPI.Core.Exceptions;

namespace TodoTaskAPI.UnitTests.Controllers

{
    public class DeleteTodoControllerEndpointTests
    {
        private readonly Mock<ITodoService> _mockService;
        private readonly Mock<ILogger<TodosController>> _mockLogger;
        private readonly TodosController _controller;

        public DeleteTodoControllerEndpointTests()
        {
            _mockService = new Mock<ITodoService>();
            _mockLogger = new Mock<ILogger<TodosController>>();
            _controller = new TodosController(_mockService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task DeleteTodo_WithExistingId_ReturnsSuccess()
        {
            // Arrange
            var todoId = Guid.NewGuid();

            _mockService.Setup(s => s.DeleteTodoAsync(todoId))
                .Verifiable();

            // Act
            var result = await _controller.DeleteTodo(todoId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponseDto<object>>(okResult.Value);

            Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
            Assert.Equal($"Todo o ID {todoId} został pomyślnie usunięty.", response.Message);
            _mockService.Verify(s => s.DeleteTodoAsync(todoId), Times.Once);
        }

        [Fact]
        public async Task DeleteTodo_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var todoId = Guid.NewGuid();

            _mockService.Setup(s => s.DeleteTodoAsync(todoId))
                .ThrowsAsync(new NotFoundException($"Todo o ID {todoId} nie został znaleziony."));

            // Act
            var result = await _controller.DeleteTodo(todoId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponseDto<object>>(notFoundResult.Value);

            Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
            Assert.Contains($"Todo o ID {todoId} nie został znaleziony.", response.Message);
            _mockService.Verify(s => s.DeleteTodoAsync(todoId), Times.Once);
        }

        [Fact]
        public async Task DeleteTodo_WithUnexpectedException_ReturnsInternalServerError()
        {
            // Arrange
            var todoId = Guid.NewGuid();

            _mockService.Setup(s => s.DeleteTodoAsync(todoId))
                .ThrowsAsync(new Exception("Nieoczekiwany błąd"));

            // Act
            var result = await _controller.DeleteTodo(todoId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);

            var response = Assert.IsType<ApiResponseDto<object>>(statusCodeResult.Value);
            Assert.Equal(StatusCodes.Status500InternalServerError, response.StatusCode);
            Assert.Contains("Wystąpił nieoczekiwany błąd serwera podczas usuwania todo.", response.Message);
            _mockService.Verify(s => s.DeleteTodoAsync(todoId), Times.Once);
        }
    }
}