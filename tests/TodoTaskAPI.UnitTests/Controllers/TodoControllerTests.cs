using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.DTOs;
using TodoTaskAPI.Application.Interfaces;
using TodoTaskAPI.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xunit;
using Microsoft.AspNetCore.Http;


namespace TodoTaskAPI.UnitTests.Controllers
{
    public class TodoControllerTests
    {
        /// <summary>
        /// Unit tests for the TodosController class, validating REST API endpoints functionality
        /// </summary>
        private readonly Mock<ITodoService> _mockService;
        private readonly Mock<ILogger<TodosController>> _mockLogger;
        private readonly TodosController _controller;

        public TodoControllerTests()
        {
            _mockService = new Mock<ITodoService>();
            _mockLogger = new Mock<ILogger<TodosController>>();
            _controller = new TodosController(_mockService.Object, _mockLogger.Object);
        }

        /// <summary>
        /// Verifies that GetAll endpoint returns all todos when pagination is not specified
        /// </summary>
        [Fact]
        public async Task GetAll_WithoutPagination_ReturnsAllTodos()
        {
            // Arrange
            var todos = new List<TodoDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Test Todo" }
            };
            _mockService.Setup(s => s.GetAllTodosAsync())
                .ReturnsAsync(todos);

            // Act
            var result = await _controller.GetAll(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponseDto<IEnumerable<TodoDto>>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.NotNull(response.Data);
            var todoList = response.Data.ToList();
            Assert.Single(todoList);
        }

        /// <summary>
        /// Verifies that GetAll endpoint returns paginated results when pagination parameters are provided
        /// </summary>
        [Fact]
        public async Task GetAll_WithPagination_ReturnsPaginatedTodos()
        {
            // Arrange
            var paginatedResponse = new PaginatedResponseDto<TodoDto>
            {
                Items = new[] { new TodoDto { Id = Guid.NewGuid() } },
                PageNumber = 1,
                PageSize = 10,
                TotalCount = 1,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllTodosWithPaginationAsync(It.IsAny<PaginationParametersDto>()))
                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await _controller.GetAll(1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponseDto<PaginatedResponseDto<TodoDto>>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.NotNull(response.Data);
            Assert.NotNull(response.Data.Items);
            Assert.Single(response.Data.Items);
        }

        /// <summary>
        /// Verifies that GetAll endpoint returns BadRequest for invalid pagination parameters
        /// </summary>
        /// <param name="pageNumber">Invalid page number to test</param>
        /// <param name="pageSize">Invalid page size to test</param>
        [Theory]
        [InlineData(0, 10)]
        [InlineData(1, 0)]
        [InlineData(1, 101)]
        public async Task GetAll_WithInvalidPagination_ReturnsBadRequest(int pageNumber, int pageSize)
        {
            // Act
            var result = await _controller.GetAll(pageNumber, pageSize);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result); // zmienione z ObjectResult
            var response = Assert.IsType<ApiResponseDto<object>>(badRequestResult.Value);
            Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        }
    }
}
