﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TodoTaskAPI.Application.DTOs;
using TodoTaskAPI.Application.Services;
using TodoTaskAPI.Core.Entities;
using TodoTaskAPI.Core.Exceptions;
using TodoTaskAPI.Core.Interfaces;
using Xunit;

namespace TodoTaskAPI.UnitTests.Services
{
    public class TodoServiceTimePeriodTests
    {
        private readonly Mock<ITodoRepository> _mockRepository;
        private readonly Mock<ILogger<TodoService>> _mockLogger;
        private readonly TodoService _service;

        public TodoServiceTimePeriodTests()
        {
            _mockRepository = new Mock<ITodoRepository>();
            _mockLogger = new Mock<ILogger<TodoService>>();
            _service = new TodoService(_mockRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetTodosByTimePeriod_Today_ReturnsCorrectRange()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;
            var timePeriodDto = new TodoTimePeriodParametersDto { Period = TodoTimePeriodParametersDto.TimePeriod.Today };

            var todos = new List<Todo>
            {
                new() { ExpiryDateTime = today.AddHours(10) }
            };

            _mockRepository.Setup(r => r.GetTodosByDateRangeAsync(today, today))
                .ReturnsAsync(todos);

            // Act
            var result = await _service.GetTodosByTimePeriodAsync(timePeriodDto);

            // Assert
            Assert.Single(result);
            _mockRepository.Verify(r => r.GetTodosByDateRangeAsync(today, today), Times.Once);
        }

        [Fact]
        public async Task GetTodosByTimePeriod_CustomRange_ValidatesDateRange()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(-1); // Invalid past date
            var endDate = DateTime.UtcNow.AddDays(1);
            var timePeriodDto = new TodoTimePeriodParametersDto
            {
                Period = TodoTimePeriodParametersDto.TimePeriod.Custom,
                StartDate = startDate,
                EndDate = endDate
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.GetTodosByTimePeriodAsync(timePeriodDto));
        }

        [Fact]
        public async Task GetTodosByTimePeriod_CurrentWeek_CalculatesCorrectRange()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;
            var endOfWeek = today.AddDays(6 - (int)today.DayOfWeek);
            var timePeriodDto = new TodoTimePeriodParametersDto { Period = TodoTimePeriodParametersDto.TimePeriod.CurrentWeek };

            var todos = new List<Todo>
            {
                new() { ExpiryDateTime = today.AddDays(2) },
                new() { ExpiryDateTime = today.AddDays(4) }
            };

            _mockRepository.Setup(r => r.GetTodosByDateRangeAsync(today, endOfWeek))
                .ReturnsAsync(todos);

            // Act
            var result = await _service.GetTodosByTimePeriodAsync(timePeriodDto);

            // Assert
            Assert.Equal(2, result.Count());
            _mockRepository.Verify(r => r.GetTodosByDateRangeAsync(today, endOfWeek), Times.Once);
        }

        [Fact]
        public async Task GetTodosByTimePeriod_CustomPeriod_WithValidDates_Succeeds()
        {
            // Arrange
            var startDate = DateTime.UtcNow.AddDays(1);
            var endDate = DateTime.UtcNow.AddDays(7);
            var timePeriodDto = new TodoTimePeriodParametersDto
            {
                Period = TodoTimePeriodParametersDto.TimePeriod.Custom,
                StartDate = startDate,
                EndDate = endDate
            };

            var todos = new List<Todo>
            {
                new() { ExpiryDateTime = startDate.AddDays(2) }
            };

            _mockRepository.Setup(r => r.GetTodosByDateRangeAsync(
                startDate.Date, endDate.Date))
                .ReturnsAsync(todos);

            // Act
            var result = await _service.GetTodosByTimePeriodAsync(timePeriodDto);

            // Assert
            Assert.Single(result);
            _mockRepository.Verify(
                r => r.GetTodosByDateRangeAsync(startDate.Date, endDate.Date),
                Times.Once);
        }
    }
}