using System;
using Xunit;
using Moq;
using TodoTaskAPI.Core.Interfaces;
using TodoTaskAPI.Application.Services;
using TodoTaskAPI.Core.Entities;
using TodoTaskAPI.Application.DTOs;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TodoTaskAPI.UnitTests.Services
{
    /// <summary>
    /// Unit tests for TodoService
    /// </summary>
    public class TodoServiceTests
    {
        private readonly Mock<ITodoRepository> _mockRepository;
        private readonly Mock<ILogger<TodoService>> _mockLogger;
        private readonly TodoService _service;

        public TodoServiceTests() 
        {
            _mockRepository  = new Mock<ITodoRepository>();
            _mockLogger = new Mock<ILogger<TodoService>>();
            _service = new TodoService(_mockRepository.Object, _mockLogger.Object);
        }

        /// <summary>
        /// Verifies that GetAllTodosAsync correctly maps Todo entities to DTOs
        /// </summary>
        [Fact]
        public async Task GetAllTodosAsync_ReturnsMappedDtos()
        {
            // Arrange
            var todos = new List<Todo>
            {
                new() {
                    Id = Guid.NewGuid(),
                    Title = "Test Todo",
                    Description = "Test Description",
                    ExpiryDateTime = DateTime.UtcNow.AddDays(1),
                    PercentComplete = 50,
                    IsDone = false,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(todos);

            // Act
            var result = await _service.GetAllTodosAsync();

            // Assert
            var todoDto = result.First();
            var todo = todos.First();
            Assert.Equal(todo.Id, todoDto.Id);
            Assert.Equal(todo.Title, todoDto.Title);
            Assert.Equal(todo.Description, todoDto.Description);
            Assert.Equal(todo.ExpiryDateTime, todoDto.ExpiryDateTime);
            Assert.Equal(todo.PercentComplete, todoDto.PercentComplete);
            Assert.Equal(todo.IsDone, todoDto.IsDone);
        }

        // <summary>
        /// Verifies that GetAllTodosWithPaginationAsync returns correct pagination metadata
        /// </summary>
        [Fact]
        public async Task GetAllTodosWithPaginationAsync_ReturnsPaginatedResult()
        {
            // Arrange
            var pageNumber = 1;
            var pageSize = 10;
            var totalCount = 20;

            var todos = Enumerable.Range(1, pageSize)
               .Select(i => new Todo
               {
                   Id = Guid.NewGuid(),
                   Title = $"Todo {i}",
                   ExpiryDateTime = DateTime.UtcNow.AddDays(i)
               })
               .ToList();

            _mockRepository.Setup(r => r.GetAllWithPaginationAsync(pageNumber, pageSize))
                .ReturnsAsync((todos, totalCount));

            var parameters = new PaginationParametersDto
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // Act
            var result = await _service.GetAllTodosWithPaginationAsync(parameters);

            // Assert
            Assert.Equal(pageNumber, result.PageNumber);
            Assert.Equal(pageSize, result.PageSize);
            Assert.Equal(totalCount, result.TotalCount);
            Assert.Equal(2, result.TotalPages); // 20 items / 10 per page = 2 pages
            Assert.True(result.HasNextPage);
            Assert.False(result.HasPreviousPage);
            Assert.Equal(pageSize, result.Items.Count());
        }

        /// <summary>
        /// Verifies that GetAllTodosWithPaginationAsync throws validation exception for null parameters
        /// </summary>
        [Fact]
        public async Task GetAllTodosWithPaginationAsync_ThrowsValidationException_ForNullParameters()
        {
            // Arrange
            var parameters = new PaginationParametersDto();

            // Act & Assert
            await Assert.ThrowsAsync<TodoTaskAPI.Core.Exceptions.ValidationException>(
                () => _service.GetAllTodosWithPaginationAsync(parameters));
        }

        /// <summary>
        /// Verifies that service correctly handles empty result from repository
        /// </summary>
        [Fact]
        public async Task GetAllTodosAsync_WhenNoData_ReturnsEmptyList()
        {
            // Arrange
            var mockRepository = new Mock<ITodoRepository>();
            mockRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Todo>());

            var service = new TodoService(mockRepository.Object, _mockLogger.Object);

            // Act
            var result = await service.GetAllTodosAsync();

            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// Verifies that service correctly calculates pagination metadata for last page
        /// </summary>
        [Fact]
        public async Task GetAllTodosWithPaginationAsync_OnLastPage_SetsCorrectMetadata()
        {
            // Arrange
            var mockRepository = new Mock<ITodoRepository>();
            var totalItems = 15;
            var pageSize = 5;
            var lastPage = 3;

            mockRepository.Setup(r => r.GetAllWithPaginationAsync(lastPage, pageSize))
                .ReturnsAsync((
                    Enumerable.Range(11, 5).Select(i => new Todo
                    {
                        Id = Guid.NewGuid(),
                        Title = $"Todo {i}"
                    }),
                    totalItems
                ));

            var service = new TodoService(mockRepository.Object, _mockLogger.Object);
            var parameters = new PaginationParametersDto
            {
                PageNumber = lastPage,
                PageSize = pageSize
            };

            // Act
            var result = await service.GetAllTodosWithPaginationAsync(parameters);

            // Assert
            Assert.Equal(lastPage, result.PageNumber);
            Assert.Equal(totalItems, result.TotalCount);
            Assert.False(result.HasNextPage);
            Assert.True(result.HasPreviousPage);
        }
    }
}

