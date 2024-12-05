using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TodoTaskAPI.Application.DTOs;
using TodoTaskAPI.Application.Interfaces;
using Xunit;
using Microsoft.AspNetCore.Http;

namespace TodoTaskAPI.UnitTests.Controllers
{
    /// <summary>
    /// Tests specifically focused on the Find Todo endpoint functionality
    /// </summary>
    public class FindTodoEndpointTests
    {
        private readonly Mock<ITodoService> _mockService;
        private readonly Mock<ILogger<TodosController>> _mockLogger;
        private readonly TodosController _controller;

        public FindTodoEndpointTests()
        {
            _mockService = new Mock<ITodoService>();
            _mockLogger = new Mock<ILogger<TodosController>>();
            _controller = new TodosController(_mockService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task FindTodos_WithValidId_ReturnsTodo()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var parameters = new TodoSearchParametersDto { Id = todoId };
            var expectedTodo = new TodoDto
            {
                Id = todoId,
                Title = "Test Todo"
            };

            _mockService.Setup(s => s.FindTodosAsync(parameters))
                .ReturnsAsync(new[] { expectedTodo });

            // Act
            var actionResult = await _controller.FindTodos(parameters);
            var result = actionResult.Result as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            var response = Assert.IsType<ApiResponseDto<IEnumerable<TodoDto>>>(result.Value);
            Assert.NotNull(response.Data);
            var todoList = response.Data.ToList();
            Assert.Single(todoList); 
            Assert.Equal(todoId, response.Data.First().Id);
            Assert.Equal(200, response.StatusCode);
        }

        [Fact]
        public async Task FindTodos_WithValidTitle_ReturnsTodo()
        {
            // Arrange
            var parameters = new TodoSearchParametersDto { Title = "Test Todo" };
            var expectedTodo = new TodoDto
            {
                Id = Guid.NewGuid(),
                Title = "Test Todo"
            };

            _mockService.Setup(s => s.FindTodosAsync(parameters))
                .ReturnsAsync(new[] { expectedTodo });

            // Act
            var actionResult = await _controller.FindTodos(parameters);
            var result = actionResult.Result as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            var response = Assert.IsType<ApiResponseDto<IEnumerable<TodoDto>>>(result.Value);
            Assert.NotNull(response.Data);
            var todoList = response.Data.ToList();
            Assert.Single(todoList);
            Assert.Equal("Test Todo", response.Data.First().Title);
        }

        [Fact]
        public async Task FindTodos_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var parameters = new TodoSearchParametersDto { Id = Guid.NewGuid() };

            _mockService.Setup(s => s.FindTodosAsync(parameters))
                .ReturnsAsync(Enumerable.Empty<TodoDto>());

            // Act
            var actionResult = await _controller.FindTodos(parameters);
            var result = actionResult.Result as NotFoundObjectResult;

            // Assert
            Assert.NotNull(result);
            var response = Assert.IsType<ApiResponseDto<IEnumerable<TodoDto>>>(result.Value);
            Assert.NotNull(response.Data);
            Assert.Empty(response.Data.ToList());
        }

        [Fact]
        public async Task FindTodos_WithNoParameters_ReturnsBadRequest()
        {
            // Arrange
            var parameters = new TodoSearchParametersDto();

            _mockService.Setup(s => s.FindTodosAsync(parameters))
                .ThrowsAsync(new TodoTaskAPI.Core.Exceptions.ValidationException("At least one search parameter must be provided"));

            // Act
            var actionResult = await _controller.FindTodos(parameters);
            var result = actionResult.Result as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            var response = Assert.IsType<ApiResponseDto<ValidationErrorResponse>>(result.Value);
            Assert.Equal(400, response.StatusCode);
        }

        [Fact]
        public async Task FindTodos_WithInvalidTitle_ReturnsBadRequest()
        {
            // Arrange
            var parameters = new TodoSearchParametersDto { Title = "Invalid@Title#" };

            _mockService.Setup(s => s.FindTodosAsync(parameters))
                .ThrowsAsync(new TodoTaskAPI.Core.Exceptions.ValidationException("Invalid title format"));

            // Act
            var actionResult = await _controller.FindTodos(parameters);
            var result = actionResult.Result as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            var response = Assert.IsType<ApiResponseDto<ValidationErrorResponse>>(result.Value);
            Assert.Equal(400, response.StatusCode);
        }

        [Fact]
        public async Task FindTodos_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var parameters = new TodoSearchParametersDto { Id = Guid.NewGuid() };

            _mockService.Setup(s => s.FindTodosAsync(parameters))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var actionResult = await _controller.FindTodos(parameters);
            var result = actionResult.Result as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
            var response = Assert.IsType<ApiResponseDto<object>>(result.Value);
            Assert.Equal(500, response.StatusCode);
        }
    }
}
