using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.DTOs;
using TodoTaskAPI.Application.Interfaces;
using TodoTaskAPI.Core.Entities;
using TodoTaskAPI.Core.Exceptions;
using TodoTaskAPI.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace TodoTaskAPI.Application.Services;

/// <summary>
/// Service handling business logic for Todo operations.
/// Implements validation, data mapping, and business rules.
/// </summary>
public class TodoService : ITodoService
{
    private readonly ITodoRepository _todoRepository;
    private readonly ILogger<TodoService> _logger;


    /// <summary>
    /// Initializes a new instance of TodoService
    /// </summary>
    /// <param name="todoRepository">Todo repository</param>
    /// <param name="logger">Logger instance</param>
    public TodoService(ITodoRepository todoRepository, ILogger<TodoService> logger)
    {
        _todoRepository = todoRepository;
        _logger = logger;

    }

    /// <inheritdoc/>
    /// <exception cref="Exception">Thrown when repository operation fails</exception>
    public async Task<IEnumerable<TodoDto>> GetAllTodosAsync()
    {
        try
        {
            // Retrieve all todos from repository
            _logger.LogInformation("Retrieving all todos");
            var todos = await _todoRepository.GetAllAsync();

            // Map entities to DTOs before returning
            return todos.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all todos");
            throw;
        }
    }

    /// <inheritdoc/>
    /// <exception cref="ValidationException">Thrown when pagination parameters are invalid</exception>
    /// <exception cref="Exception">Thrown when database operation fails</exception>
    public async Task<PaginatedResponseDto<TodoDto>> GetAllTodosWithPaginationAsync(PaginationParametersDto parameters)
    {
        try
        {
            // Validate input parameters
            if (!parameters.PageNumber.HasValue || !parameters.PageSize.HasValue)
            {
                _logger.LogWarning("Invalid pagination parameters provided");
                throw new ValidationException("Pagination parameters cannot be null");
            }

            // Retrieve paginated data from repository
            var (todos, totalCount) = await _todoRepository.GetAllWithPaginationAsync(
                parameters.PageNumber.Value,
                parameters.PageSize.Value
            );

            // Calculate pagination metadata
            var totalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize.Value);
            var currentPage = parameters.PageNumber.Value;

            _logger.LogInformation(
                "Retrieved page {CurrentPage} of {TotalPages} (Total items: {TotalCount})",
                currentPage, totalPages, totalCount);

            // Build and return paginated response with mapped DTOs
            return new PaginatedResponseDto<TodoDto>
            {
                Items = todos.Select(MapToDto),
                PageNumber = currentPage,
                PageSize = parameters.PageSize.Value,
                TotalPages = totalPages,
                TotalCount = totalCount,
                HasNextPage = currentPage < totalPages,
                HasPreviousPage = currentPage > 1
            };
        }
        catch (ValidationException)
        {
            // Rethrow validation exceptions for proper handling
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve paginated todos. Page: {Page}, Size: {Size}",
                parameters.PageNumber, parameters.PageSize);
            throw;
        }
    }


    /// <inheritdoc/>
    /// <exception cref="ValidationException">Thrown when parameters are invalid</exception>
    /// <exception cref="Exception">Thrown when database query fails</exception>
    public async Task<IEnumerable<TodoDto>> FindTodosAsync(TodoSearchParametersDto parameters)
    {
        try
        {
            _logger.LogInformation("Starting todo search with parameters: ID: {Id}, Title: {Title}",
                parameters.Id, parameters.Title);

            // Validate parameters
            parameters.ValidateParameters();

            var todos = await _todoRepository.FindTodosAsync(parameters.Id, parameters.Title);
            return todos.Select(MapToDto);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, "Error occurred while searching for todos");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching for todos");
            throw;
        }
    }

    /// <inheritdoc/>
    /// <exception cref="ValidationException">Thrown when business validation fails</exception>
    /// <exception cref="ApplicationException">Thrown when unexpected errors occur during creation</exception>
    public async Task<TodoDto> CreateTodoAsync(CreateTodoDto createTodoDto)
    {
        try
        {
            _logger.LogInformation("Starting business validation for todo: {Title}", createTodoDto.Title);

            // Perform business validation
            var validationErrors = ValidateBusinessRules(createTodoDto);
            if (validationErrors.Any())
            {
                throw new ValidationException(string.Join(Environment.NewLine, validationErrors));
            }

            // Create new todo entity from DTO
            var todo = new Todo
            {
                Id = Guid.NewGuid(),
                Title = createTodoDto.Title.Trim(),
                Description = createTodoDto.Description?.Trim() ?? string.Empty,
                ExpiryDateTime = createTodoDto.ExpiryDateTime.ToUniversalTime(),
                PercentComplete = createTodoDto.PercentComplete ?? 0,
                CreatedAt = DateTime.UtcNow,
                IsDone = false
            };

            _logger.LogInformation("Attempting to save todo to database: {Title}", todo.Title);
            var createdTodo = await _todoRepository.AddAsync(todo);
            _logger.LogInformation("Successfully created new todo with ID: {TodoId}", createdTodo.Id);

            return MapToDto(createdTodo);
        }
        catch (ValidationException)
        {
            // Re-throw validation exceptions to preserve the original message and stack trace
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating todo: {Title}", createTodoDto.Title);
            throw;
        }
    }

    /// <summary>
    /// Validates business rules for todo creation
    /// </summary>
    /// <param name="dto">Todo creation DTO containing data to validate</param>
    /// <returns>List of validation errors, empty if validation passes</returns>
    /// <remarks>
    /// Current business rules:
    /// - ExpiryDateTime must be in the future
    /// - Additional rules can be added here
    /// </remarks>
    private List<string> ValidateBusinessRules(CreateTodoDto dto)
    {
        var errors = new List<string>();

        // Validate if ExpiryDateTime is in the future
        if (dto.ExpiryDateTime < DateTime.UtcNow)
        {
            errors.Add("ExpiryDateTime must be in the future.");
        }

        // You can add more rules

        return errors;
    }

    /// <summary>
    /// Maps a Todo entity to a TodoDto
    /// </summary>
    /// <param name="todo">Todo entity to convert</param>
    /// <returns>Mapped TodoDto with formatted data</returns>
    private static TodoDto MapToDto(Todo todo) => new()
    {
        Id = todo.Id,
        Title = todo.Title.Trim(),
        Description = todo.Description?.Trim() ?? string.Empty,
        ExpiryDateTime = todo.ExpiryDateTime,
        PercentComplete = todo.PercentComplete,
        IsDone = todo.IsDone,
        CreatedAt = todo.CreatedAt,
        UpdatedAt = todo.UpdatedAt
    };

    /// <inheritdoc/>
    /// <exception cref="ValidationException">Thrown when parameters are invalid</exception>
    /// <exception cref="Exception">Thrown when database query fails</exception>
    public async Task<IEnumerable<TodoDto>> GetTodosByTimePeriodAsync(TodoTimePeriodParametersDto timePeriodDto)
    {
        try
        {
            _logger.LogInformation("Getting todos for period: {Period}", timePeriodDto.Period);

            // Validate input parameters
            timePeriodDto.Validate();

            // Calculate date range and retrieve todos
            var (startDate, endDate) = CalculateDateRange(timePeriodDto);
            var todos = await _todoRepository.GetTodosByDateRangeAsync(startDate, endDate);

            // Map and return results
            return todos.Select(MapToDto);
        }
        catch (ValidationException)
        {
            // Let validation exceptions propagate
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting todos by time period");
            throw;
        }
    }

    /// <summary>
    /// Calculates start and end dates based on specified time period
    /// </summary>
    /// <param name="timePeriodDto">Time period specification</param>
    /// <returns>Tuple containing calculated start and end dates</returns>
    /// <remarks>
    /// Date ranges are inclusive of start date and exclusive of end date
    /// All times are normalized to UTC
    /// End date is set to end of day (23:59:59)
    /// </remarks>
    /// <exception cref="ValidationException">Thrown if period is invalid</exception>
    private static (DateTime StartDate, DateTime EndDate) CalculateDateRange(TodoTimePeriodParametersDto timePeriodDto)
    {
        // Normalize to start of day in UTC
        var today = DateTime.UtcNow.Date;

        // Calculate range based on period type
        return timePeriodDto.Period switch
        {
            TodoTimePeriodParametersDto.TimePeriod.Today => (
                today,
                today.AddDays(1).AddSeconds(-1)),

            TodoTimePeriodParametersDto.TimePeriod.Tomorrow => (
                today.AddDays(1),
                today.AddDays(2).AddSeconds(-1)),

            TodoTimePeriodParametersDto.TimePeriod.CurrentWeek => (
                today,
                today.AddDays(7).AddSeconds(-1)),

            TodoTimePeriodParametersDto.TimePeriod.Custom => (
                timePeriodDto.StartDate!.Value.Date,
                timePeriodDto.EndDate!.Value.Date.AddDays(1).AddSeconds(-1)),

            _ => throw new ValidationException("Invalid time period specified")
        };
    }

    /// <inheritdoc/>
    /// <exception cref="ValidationException">Thrown when validation fails</exception>
    /// <exception cref="NotFoundException">Thrown when todo is not found</exception>
    /// <exception cref="Exception">Thrown when database update fails</exception>
    public async Task<TodoDto> UpdateTodoAsync(Guid id, UpdateTodoDto updateTodoDto)
    {
        try
        {
            _logger.LogInformation("Starting todo update for ID: {TodoId}", id);

            // Validate that at least one property is being updated
            updateTodoDto.ValidateAtLeastOnePropertySet();

            // Fetch and verify todo exists
            var todo = await _todoRepository.GetByIdAsync(id);
            if (todo == null)
            {
                throw new NotFoundException($"Todo with ID {id} not found");
            }

            // Update only provided properties
            if (updateTodoDto.Title != null)
            {
                todo.Title = updateTodoDto.Title.Trim();
            }

            if (updateTodoDto.Description != null)
            {
                todo.Description = updateTodoDto.Description.Trim();
            }

            if (updateTodoDto.ExpiryDateTime.HasValue)
            {
                todo.ExpiryDateTime = updateTodoDto.ExpiryDateTime.Value.ToUniversalTime();
            }

            if (updateTodoDto.PercentComplete.HasValue)
            {
                todo.PercentComplete = updateTodoDto.PercentComplete.Value;
            }

            if (updateTodoDto.IsDone.HasValue)
            {
                todo.IsDone = updateTodoDto.IsDone.Value;
                if (todo.IsDone)
                {
                    todo.PercentComplete = 100;
                }
            }

            // Save changes
            var updatedTodo = await _todoRepository.UpdateAsync(todo);
            _logger.LogInformation("Successfully updated todo with ID: {TodoId}", id);

            return MapToDto(updatedTodo);
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("Todo not found during update: {TodoId}", id);
            throw;
        }
        catch (ValidationException)
        {
            _logger.LogWarning("Validation failed during todo update for ID: {TodoId}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating todo: {TodoId}", id);
            throw;
        }
    }


    /// <inheritdoc/>
    /// <exception cref="NotFoundException">Wyrzucany, gdy todo nie zostanie znalezione</exception>
    /// <exception cref="Exception">Thrown when delete operation fails</exception>
    public async Task DeleteTodoAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Rozpoczynanie usuwania todo o ID: {TodoId}", id);

            // Verify todo exists before deletion
            var todo = await _todoRepository.GetByIdAsync(id);
            if (todo == null)
            {
                throw new NotFoundException($"Todo o ID {id} nie został znaleziony.");
            }

            // Perform deletion
            await _todoRepository.DeleteAsync(todo);
            _logger.LogInformation("Pomyślnie usunięto todo o ID: {TodoId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania todo o ID: {TodoId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    /// <exception cref="NotFoundException">Thrown when todo not found</exception>
    /// <exception cref="Exception">Thrown when update fails</exception>
    public async Task<TodoDto> UpdateTodoCompletionAsync(Guid id, UpdateTodoCompletionDto updateDto)
    {
        try
        {
            // Retrieve and verify todo exists
            var todo = await _todoRepository.GetByIdAsync(id);
            if (todo == null)
            {
                throw new NotFoundException($"Todo with ID {id} not found");
            }

            // Update completion status
            todo.PercentComplete = updateDto.PercentComplete;

            // Auto-update done status based on completion
            todo.IsDone = todo.PercentComplete == 100;

            // Save changes
            var updatedTodo = await _todoRepository.UpdateAsync(todo);
            return MapToDto(updatedTodo);
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("Todo not found during done status update: {TodoId}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating todo completion: {TodoId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    /// <exception cref="NotFoundException">Thrown when todo not found</exception>
    /// <exception cref="Exception">Thrown when update fails</exception>
    public async Task<TodoDto> UpdateTodoDoneStatusAsync(Guid id, UpdateTodoDoneStatusDto updateDto)
    {
        try
        {
            _logger.LogInformation("Starting todo done status update for ID: {TodoId}", id);

            // Retrieve and verify todo exists 
            var todo = await _todoRepository.GetByIdAsync(id);
            if (todo == null)
            {
                throw new NotFoundException($"Todo with ID {id} not found");
            }

            // Update done status and set completion percentage
            todo.IsDone = updateDto.IsDone;
            todo.PercentComplete = updateDto.IsDone ? 100 : 0;

            // Save changes
            var updatedTodo = await _todoRepository.UpdateAsync(todo);
            _logger.LogInformation("Successfully updated todo done status with ID: {TodoId}", id);

            return MapToDto(updatedTodo);
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("Todo not found during done status update: {TodoId}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating todo done status: {TodoId}", id);
            throw;
        }
    }
}

