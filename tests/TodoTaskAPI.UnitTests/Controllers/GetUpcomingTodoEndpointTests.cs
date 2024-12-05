using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TodoTaskAPI.Application.DTOs;
using TodoTaskAPI.Application.Interfaces;
using TodoTaskAPI.Core.Exceptions;
using Xunit;

namespace TodoTaskAPI.UnitTests.Controllers
{
    public class GetUpcomingTodoEndpointTests
    {
        private readonly Mock<ITodoService> _mockService;
        private readonly Mock<ILogger<TodosController>> _mockLogger;
        private readonly TodosController _controller;

        public GetUpcomingTodoEndpointTests()
        {
            _mockService = new Mock<ITodoService>();
            _mockLogger = new Mock<ILogger<TodosController>>();
            _controller = new TodosController(_mockService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetUpcoming_WithValidPeriod_ReturnsSuccess()
        {
            // Arrange
            var timePeriodDto = new TodoTimePeriodParametersDto { Period = TodoTimePeriodParametersDto.TimePeriod.Today };
            var expectedTodos = new List<TodoDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Test Todo", ExpiryDateTime = DateTime.UtcNow }
            };

            _mockService.Setup(s => s.GetTodosByTimePeriodAsync(timePeriodDto))
                .ReturnsAsync(expectedTodos);

            // Act
            var result = await _controller.GetUpcoming(timePeriodDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponseDto<IEnumerable<TodoDto>>>(okResult.Value);
            Assert.NotNull(response.Data);
            Assert.NotEmpty(response.Data!);
        }

        [Fact]
        public async Task GetUpcoming_WithInvalidPeriod_ReturnsBadRequest()
        {
            // Arrange
            var timePeriodDto = new TodoTimePeriodParametersDto
            {
                Period = TodoTimePeriodParametersDto.TimePeriod.Custom,
                // Missing required custom dates
            };

            _mockService.Setup(s => s.GetTodosByTimePeriodAsync(timePeriodDto))
                .ThrowsAsync(new ValidationException("Custom dates are required"));

            // Act
            var result = await _controller.GetUpcoming(timePeriodDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponseDto<ValidationErrorResponse>>(badRequestResult.Value);
            Assert.Equal(400, response.StatusCode);
            Assert.NotNull(response.Data?.Errors);
        }

        [Fact]
        public async Task GetUpcoming_WithCustomDateRange_ReturnsSuccess()
        {
            // Arrange
            var timePeriodDto = new TodoTimePeriodParametersDto
            {
                Period = TodoTimePeriodParametersDto.TimePeriod.Custom,
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(7)
            };

            var expectedTodos = new List<TodoDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Future Todo", ExpiryDateTime = DateTime.UtcNow.AddDays(3) }
            };

            _mockService.Setup(s => s.GetTodosByTimePeriodAsync(timePeriodDto))
                .ReturnsAsync(expectedTodos);

            // Act
            var result = await _controller.GetUpcoming(timePeriodDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponseDto<IEnumerable<TodoDto>>>(okResult.Value);
            Assert.Equal(200, response.StatusCode);
            Assert.NotNull(response.Data);
            Assert.NotEmpty(response.Data!);
        }

        [Fact]
        public async Task GetUpcoming_WithUnexpectedError_ReturnsInternalServerError()
        {
            // Arrange
            var timePeriodDto = new TodoTimePeriodParametersDto { Period = TodoTimePeriodParametersDto.TimePeriod.Today };

            _mockService.Setup(s => s.GetTodosByTimePeriodAsync(timePeriodDto))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.GetUpcoming(timePeriodDto);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
    }
}