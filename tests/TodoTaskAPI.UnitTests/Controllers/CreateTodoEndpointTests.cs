using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TodoTaskAPI.Application.DTOs;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace TodoTaskAPI.UnitTests.Controllers
{
    /// <summary>
    /// Tests specifically focused on the Create Todo endpoint functionality
    /// </summary>
    public class CreateTodoEndpointTests
    {
        private readonly Mock<ITodoService> _mockService;
        private readonly Mock<ILogger<TodosController>> _mockLogger;
        private readonly TodosController _controller;

        public CreateTodoEndpointTests()
        {
            _mockService = new Mock<ITodoService>();
            _mockLogger = new Mock<ILogger<TodosController>>();
            _controller = new TodosController(_mockService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Create_WithValidData_ReturnsCreatedResult()
        {
            var createDto = new CreateTodoDto
            {
                Title = "Test Todo",
                Description = "Test Description",
                ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                PercentComplete = 0
            };

            var expectedTodo = new TodoDto
            {
                Id = Guid.NewGuid(),
                Title = createDto.Title,
                Description = createDto.Description ?? string.Empty,
                ExpiryDateTime = createDto.ExpiryDateTime,
                PercentComplete = createDto.PercentComplete ?? 0,
                CreatedAt = DateTime.UtcNow,
                IsDone = false
            };

            _mockService.Setup(s => s.CreateTodoAsync(createDto))
                .ReturnsAsync(expectedTodo);

            var actionResult = await _controller.Create(createDto);
            var result = actionResult.Result;

            Assert.NotNull(result);
            var createdResult = Assert.IsType<CreatedResult>(result);
            var response = Assert.IsType<ApiResponseDto<TodoDto>>(createdResult.Value);

            Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
            Assert.Equal($"api/todos/{expectedTodo.Id}", createdResult.Location);
            Assert.NotNull(response.Data);
            Assert.Equal(expectedTodo.Id, response.Data.Id);
        }

        [Fact]
        public async Task Create_WithInvalidModelState_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("Title", "Title is required");
            var createDto = new CreateTodoDto
            {
                Title = string.Empty,
                ExpiryDateTime = DateTime.UtcNow.AddDays(1)
            };

            var actionResult = await _controller.Create(createDto);
            var result = actionResult.Result;

            Assert.NotNull(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponseDto<ValidationErrorResponse>>(badRequestResult.Value);

            Assert.NotNull(response.Data);
            Assert.True(response.Data.Errors.ContainsKey("Title"));
        }

        [Fact]
        public async Task Create_WithBusinessValidationError_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new CreateTodoDto
            {
                Title = "Test Todo",
                ExpiryDateTime = DateTime.UtcNow.AddDays(-1) // Nieprawidłowa data
            };

            _mockService.Setup(s => s.CreateTodoAsync(createDto))
                .ThrowsAsync(new Core.Exceptions.ValidationException("Business validation failed"));

            // Act
            var actionResult = await _controller.Create(createDto);
            var result = actionResult.Result;

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponseDto<ValidationErrorResponse>>(badRequestResult.Value);
            Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
            Assert.NotNull(response.Data);
            var todoData = response.Data;
            Assert.NotNull(response.Data);
        }

        [Fact]
        public async Task Create_WithUnexpectedException_ReturnsInternalServerError()
        {
            var createDto = new CreateTodoDto
            {
                Title = "Test Todo",
                ExpiryDateTime = DateTime.UtcNow.AddDays(1)
            };

            _mockService.Setup(s => s.CreateTodoAsync(createDto))
                .ThrowsAsync(new Exception("Unexpected error"));

            var actionResult = await _controller.Create(createDto);
            var result = actionResult.Result;

            Assert.NotNull(result);
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        [Fact]
        public void Create_WithPastExpiryDate_ReturnsBadRequest()
        {
            var createDto = new CreateTodoDto
            {
                Title = "Test Todo",
                ExpiryDateTime = DateTime.UtcNow.AddDays(-1),
                PercentComplete = 0
            };

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(createDto);
            var isValid = Validator.TryValidateObject(createDto, validationContext, validationResults, true);

            Assert.False(isValid);
            Assert.Contains(validationResults, vr => vr.ErrorMessage?.Contains("future") ?? false);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Emoji 🚀 jest super")]
        [InlineData("This is a very long title that exceeds the maximum allowed length limit and should cause a validation error when attempting to create a new todo item in the system bla bla bla bla bla bla bla bla bla bla")]
        [InlineData(null)]
        public void Create_WithInvalidTitle_ReturnsBadRequest(string? title)
        {
            var createDto = new CreateTodoDto
            {
                Title = title ?? string.Empty,  // użyj null coalescing operatora

                ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                PercentComplete = 0
            };

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(createDto);
            var isValid = Validator.TryValidateObject(createDto, validationContext, validationResults, true);

            Assert.False(isValid);
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("Title"));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void Create_WithInvalidPercentComplete_ReturnsBadRequest(int percentComplete)
        {
            var createDto = new CreateTodoDto
            {
                Title = "Test Todo",
                Description = "Test Description",
                ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                PercentComplete = percentComplete
            };

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(createDto);
            var isValid = Validator.TryValidateObject(createDto, validationContext, validationResults, true);

            Assert.False(isValid);
            Assert.Contains(validationResults, vr => vr.MemberNames.Contains("PercentComplete"));
        }

        [Fact]
        public async Task Create_WithInvalidTitle_ReturnsPreciseErrorMessage()
        {
            // Arrange
            var createDto = new CreateTodoDto
            {
                Title = "Title@#$%^&*()", // Invalid characters
                ExpiryDateTime = DateTime.UtcNow.AddDays(1)
            };

            _controller.ModelState.AddModelError("Title", "Title can only contain letters, numbers, spaces, and basic punctuation (!?,:;.-_)");

            // Act
            var actionResult = await _controller.Create(createDto);
            var result = actionResult.Result;

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponseDto<ValidationErrorResponse>>(badRequestResult.Value);

            Assert.NotNull(response.Data?.Errors);
            Assert.Contains("Title", response.Data.Errors.Keys);
            Assert.Contains("Title can only contain letters, numbers, spaces, and basic punctuation (!?,:;.-_)",
                response.Data.Errors["Title"]);
        }

        [Fact]
        public async Task Create_MultipleConcurrentRequests_HandlesCorrectly()
        {
            // Arrange
            var requests = Enumerable.Range(0, 10).Select(_ => new CreateTodoDto
            {
                Title = "Concurrent Todo " + Guid.NewGuid(),
                ExpiryDateTime = DateTime.UtcNow.AddDays(1)
            }).ToList();

            var expectedTodo = new TodoDto
            {
                Id = Guid.NewGuid(),
                Title = "Test Todo",
                ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow
            };

            _mockService.Setup(s => s.CreateTodoAsync(It.IsAny<CreateTodoDto>()))
                .ReturnsAsync(expectedTodo);

            // Act
            var tasks = requests.Select(request => _controller.Create(request));
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, result =>
            {
                var createdResult = Assert.IsType<CreatedResult>(result.Result);
                Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
            });

            _mockService.Verify(s => s.CreateTodoAsync(It.IsAny<CreateTodoDto>()),
                Times.Exactly(requests.Count));
        }

        [Theory]
        [InlineData("Title with @#$")]
        [InlineData("Normal Title")]
        public async Task CreateTodoAsync_WithSpecialCharacters_HandlesCorrectly(string title)
        {
            // Arrange
            var createDto = new CreateTodoDto
            {
                Title = title,
                ExpiryDateTime = DateTime.UtcNow.AddDays(1)
            };

            if (!Regex.IsMatch(title, @"^[a-zA-Z0-9\s\-_.!?,:;()]+$"))
            {
                _controller.ModelState.AddModelError("Title", "Title can only contain letters, numbers, spaces, and basic punctuation (!?,:;.-_)");
            }
            else
            {
                var expectedTodo = new TodoDto
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    ExpiryDateTime = createDto.ExpiryDateTime,
                    CreatedAt = DateTime.UtcNow
                };
                _mockService.Setup(s => s.CreateTodoAsync(It.IsAny<CreateTodoDto>()))
                    .ReturnsAsync(expectedTodo);
            }

            // Act
            var actionResult = await _controller.Create(createDto);

            // Assert
            if (Regex.IsMatch(title, @"^[a-zA-Z0-9\s\-_.!?,:;()]+$"))
            {
                var result = Assert.IsType<CreatedResult>(actionResult.Result);
                Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
            }
            else
            {
                var result = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
                Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
                var response = Assert.IsType<ApiResponseDto<ValidationErrorResponse>>(result.Value);
                Assert.NotNull(response.Data);
                Assert.Contains("Title", response.Data.Errors.Keys);
            }
        }

        [Fact]
        public async Task CreateTodoAsync_WithDifferentTimeZones_NormalizesToUtc()
        {
            // Arrange
            var localDateTime = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Local);
            var createDto = new CreateTodoDto
            {
                Title = "Timezone Test Todo",
                ExpiryDateTime = localDateTime
            };

            var expectedUtcDateTime = localDateTime.ToUniversalTime();
            var expectedTodo = new TodoDto
            {
                Id = Guid.NewGuid(),
                Title = createDto.Title,
                ExpiryDateTime = expectedUtcDateTime,
                CreatedAt = DateTime.UtcNow
            };

            _mockService.Setup(s => s.CreateTodoAsync(It.IsAny<CreateTodoDto>()))
                .ReturnsAsync(expectedTodo);

            // Act
            var actionResult = await _controller.Create(createDto);
            var result = actionResult.Result;

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            var response = Assert.IsType<ApiResponseDto<TodoDto>>(createdResult.Value);
            Assert.NotNull(response.Data);
            var todoData = response.Data;
            Assert.Equal(expectedUtcDateTime, todoData.ExpiryDateTime);
        }
    }
}
