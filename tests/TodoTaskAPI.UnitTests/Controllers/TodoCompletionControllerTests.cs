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
    /// <summary>
    /// Tests for todo completion and done status related endpoints
    /// Verifies the behavior of percentage completion and done status updates
    /// </summary>
    public class TodoCompletionControllerTests
    {
        private readonly Mock<ITodoService> _mockService;
        private readonly Mock<ILogger<TodosController>> _mockLogger;
        private readonly TodosController _controller;

        public TodoCompletionControllerTests()
        {
            _mockService = new Mock<ITodoService>();
            _mockLogger = new Mock<ILogger<TodosController>>();
            _controller = new TodosController(_mockService.Object, _mockLogger.Object);
        }

        /// <summary>
        /// Verifies that updating completion percentage within valid range succeeds
        /// Tests the happy path for completion percentage updates
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(50)]
        [InlineData(100)]
        public async Task UpdateCompletion_WithValidPercentage_ReturnsOkResult(int percentComplete)
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var updateDto = new UpdateTodoCompletionDto { PercentComplete = percentComplete };
            var expectedTodo = new TodoDto
            {
                Id = todoId,
                PercentComplete = percentComplete,
                IsDone = percentComplete == 100
            };

            _mockService.Setup(s => s.UpdateTodoCompletionAsync(todoId, updateDto))
                .ReturnsAsync(expectedTodo);

            // Act
            var actionResult = await _controller.UpdateCompletion(todoId, updateDto);
            var result = actionResult.Result as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            var response = Assert.IsType<ApiResponseDto<TodoDto>>(result.Value);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.Equal(percentComplete, response.Data?.PercentComplete);
            Assert.Equal(percentComplete == 100, response.Data?.IsDone);
        }

        /// <summary>
        /// Verifies that updating completion percentage with invalid values returns BadRequest
        /// Tests validation of input values
        /// </summary>
        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public async Task UpdateCompletion_WithInvalidPercentage_ReturnsBadRequest(int percentComplete)
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var updateDto = new UpdateTodoCompletionDto { PercentComplete = percentComplete };

            // Add model validation error
            _controller.ModelState.AddModelError("PercentComplete", "Percentage must be between 0 and 100");

            // Act
            var actionResult = await _controller.UpdateCompletion(todoId, updateDto);
            var result = actionResult.Result as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            var response = Assert.IsType<ApiResponseDto<ValidationErrorResponse>>(result.Value);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            Assert.NotNull(response.Data?.Errors);
            Assert.Contains("PercentComplete", response.Data.Errors.Keys);
        }

        /// <summary>
        /// Verifies that updating a non-existent todo returns NotFound
        /// Tests error handling for missing resources
        /// </summary>
        [Fact]
        public async Task UpdateCompletion_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var updateDto = new UpdateTodoCompletionDto { PercentComplete = 50 };

            _mockService.Setup(s => s.UpdateTodoCompletionAsync(todoId, updateDto))
                .ThrowsAsync(new NotFoundException($"Todo with ID {todoId} not found"));

            // Act
            var actionResult = await _controller.UpdateCompletion(todoId, updateDto);
            var result = actionResult.Result as NotFoundObjectResult;

            // Assert
            Assert.NotNull(result);
            var response = Assert.IsType<ApiResponseDto<object>>(result.Value);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            Assert.Contains(todoId.ToString(), response.Message);
        }

        /// <summary>
        /// Verifies that service exceptions are properly handled
        /// Tests error handling for unexpected service failures
        /// </summary>
        [Fact]
        public async Task UpdateCompletion_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var updateDto = new UpdateTodoCompletionDto { PercentComplete = 50 };

            _mockService.Setup(s => s.UpdateTodoCompletionAsync(todoId, updateDto))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var actionResult = await _controller.UpdateCompletion(todoId, updateDto);
            var result = actionResult.Result as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        }

        /// <summary>
        /// Verifies that marking a todo as done returns successful result
        /// Tests the happy path for done status updates
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateDoneStatus_WithValidStatus_ReturnsOkResult(bool isDone)
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var updateDto = new UpdateTodoDoneStatusDto { IsDone = isDone };
            var expectedTodo = new TodoDto
            {
                Id = todoId,
                IsDone = isDone,
                PercentComplete = isDone ? 100 : 0
            };

            _mockService.Setup(s => s.UpdateTodoDoneStatusAsync(todoId, updateDto))
                .ReturnsAsync(expectedTodo);

            // Act
            var actionResult = await _controller.UpdateDoneStatus(todoId, updateDto);
            var result = actionResult.Result as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            var response = Assert.IsType<ApiResponseDto<TodoDto>>(result.Value);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.Equal(isDone, response.Data?.IsDone);
            Assert.Equal(isDone ? 100 : 0, response.Data?.PercentComplete);
        }
    }
}
