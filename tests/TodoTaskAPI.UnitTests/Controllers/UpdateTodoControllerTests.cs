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
    /// Unit tests for the Update Todo controller endpoint
    /// Tests HTTP layer behavior, request handling, and response formatting
    /// </summary>
    public class UpdateTodoControllerTests
    {
        private readonly Mock<ITodoService> _mockService;
        private readonly Mock<ILogger<TodosController>> _mockLogger;
        private readonly TodosController _controller;

        public UpdateTodoControllerTests()
        {
            _mockService = new Mock<ITodoService>();
            _mockLogger = new Mock<ILogger<TodosController>>();
            _controller = new TodosController(_mockService.Object, _mockLogger.Object);
        }

        /// <summary>
        /// Verifies that controller returns correct HTTP status and response for successful update
        /// Tests the happy path of controller update logic
        /// </summary>
        [Fact]
        public async Task Update_WithValidData_ReturnsOkResult()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var updateDto = new UpdateTodoDto
            {
                Title = "Updated Title",
                Description = "Updated Description"
            };

            var updatedTodo = new TodoDto
            {
                Id = todoId,
                Title = updateDto.Title,
                Description = updateDto.Description
            };

            _mockService.Setup(s => s.UpdateTodoAsync(todoId, updateDto))
                .ReturnsAsync(updatedTodo);

            // Act
            var actionResult = await _controller.Update(todoId, updateDto);
            var result = actionResult.Result as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            var response = Assert.IsType<ApiResponseDto<TodoDto>>(result.Value);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.Equal(updatedTodo.Title, response.Data?.Title);
        }

        /// <summary>
        /// Verifies that controller returns NotFound for non-existent todos
        /// Tests error handling for missing resources
        /// </summary>
        [Fact]
        public async Task Update_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var updateDto = new UpdateTodoDto
            {
                Title = "Updated Title"
            };

            _mockService.Setup(s => s.UpdateTodoAsync(todoId, updateDto))
                .ThrowsAsync(new NotFoundException($"Todo with ID {todoId} not found"));

            // Act
            var actionResult = await _controller.Update(todoId, updateDto);
            var result = actionResult.Result as NotFoundObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        }

        /// <summary>
        /// Verifies proper handling of validation errors
        /// Tests controller's validation error response formatting
        /// </summary>
        [Fact]
        public async Task Update_WithInvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var updateDto = new UpdateTodoDto(); // Empty DTO
            _controller.ModelState.AddModelError("Title", "Title is required");

            // Act
            var actionResult = await _controller.Update(todoId, updateDto);
            var result = actionResult.Result as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            var response = Assert.IsType<ApiResponseDto<ValidationErrorResponse>>(result.Value);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            Assert.NotNull(response.Data);
            Assert.NotNull(response.Data.Errors);
            Assert.Contains("Title", response.Data.Errors.Keys);
        }
    }
}
