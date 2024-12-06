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
    public async Task<IEnumerable<TodoDto>> GetAllTodosAsync()
    {
        var todos = await _todoRepository.GetAllAsync();
        return todos.Select(MapToDto);
    }

    /// <inheritdoc/>
    public async Task<PaginatedResponseDto<TodoDto>> GetAllTodosWithPaginationAsync(PaginationParametersDto parameters)
    {
        // Validate parameters are not null
        if (!parameters.PageNumber.HasValue || !parameters.PageSize.HasValue)
        {
            throw new ValidationException("Pagination parameters cannot be null");
        }

        // Get data from repository
        var (todos, totalCount) = await _todoRepository.GetAllWithPaginationAsync(
            parameters.PageNumber.Value,
            parameters.PageSize.Value
        );

        // Calculate total pages
        var totalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize.Value);



        // Create and return paginated response
        return new PaginatedResponseDto<TodoDto>
        {
            Items = todos.Select(MapToDto),
            PageNumber = parameters.PageNumber.Value,
            PageSize = parameters.PageSize.Value,
            TotalPages = totalPages,
            TotalCount = totalCount,
            HasNextPage = parameters.PageNumber < totalPages,
            HasPreviousPage = parameters.PageNumber > 1
        };
    }


    /// <summary>
    /// Finds specific todos based on search parameters with validation
    /// </summary>
    /// <param name="parameters">Search parameters</param>
    /// <returns>Collection of matching todo DTOs</returns>
    /// <exception cref="ValidationException">Thrown when parameters are invalid</exception>
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

    /// <summary>
    /// Creates a new todo item with validation
    /// </summary>
    /// <param name="createTodoDto">Data for creating new todo</param>
    /// <returns>Created todo as DTO</returns>
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
    /// <param name="dto">Todo creation data</param>
    /// <returns>List of validation errors</returns>
    private List<string> ValidateBusinessRules(CreateTodoDto dto)
    {
        var errors = new List<string>();

        // Validate if ExpiryDateTime is in the future
        if (dto.ExpiryDateTime < DateTime.UtcNow)
        {
            errors.Add("ExpiryDateTime must be in the future.");
        }

        return errors;
    }

    /// <summary>
    /// Maps Todo entity to TodoDto ensuring all properties are properly converted
    /// </summary>
    /// <param name="todo">Todo entity to map</param>
    /// <returns>Mapped TodoDto object with consistent property formatting</returns>
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

    /// <summary>
    /// Gets todos within specified time period
    /// </summary>
    /// <param name="timePeriodDto">Time period parameters</param>
    /// <returns>Collection of todos within the specified period</returns>
    /// <exception cref="ValidationException">Thrown when parameters are invalid</exception>
    public async Task<IEnumerable<TodoDto>> GetTodosByTimePeriodAsync(TodoTimePeriodParametersDto timePeriodDto)
    {
        try
        {
            _logger.LogInformation("Getting todos for period: {Period}", timePeriodDto.Period);

            timePeriodDto.Validate();

            var (startDate, endDate) = CalculateDateRange(timePeriodDto);
            var todos = await _todoRepository.GetTodosByDateRangeAsync(startDate, endDate);

            return todos.Select(MapToDto);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting todos by time period");
            throw;
        }
    }

    /// <summary>
    /// Calculates the date range based on the specified time period
    /// </summary>
    /// <param name="timePeriodDto">Time period parameters</param>
    /// <returns>Tuple containing start and end dates</returns>
    private static (DateTime StartDate, DateTime EndDate) CalculateDateRange(TodoTimePeriodParametersDto timePeriodDto)
    {
        var today = DateTime.UtcNow.Date;

        return timePeriodDto.Period switch
        {
            TodoTimePeriodParametersDto.TimePeriod.Today => (today, today),

            TodoTimePeriodParametersDto.TimePeriod.Tomorrow => (
                today.AddDays(1),
                today.AddDays(1)),

            TodoTimePeriodParametersDto.TimePeriod.CurrentWeek => (
                today,
                today.AddDays(6 - (int)today.DayOfWeek)),

            TodoTimePeriodParametersDto.TimePeriod.Custom => (
                timePeriodDto.StartDate!.Value.Date,
                timePeriodDto.EndDate!.Value.Date),

            _ => throw new ValidationException("Invalid time period specified")
        };
    }

    /// <summary>
    /// Updates an existing todo with validation and business rules
    /// </summary>
    /// <param name="id">ID of todo to update</param>
    /// <param name="updateTodoDto">Update data</param>
    /// <returns>Updated todo as DTO</returns>
    /// <exception cref="ValidationException">Thrown when validation fails</exception>
    /// <exception cref="NotFoundException">Thrown when todo is not found</exception>
    public async Task<TodoDto> UpdateTodoAsync(Guid id, UpdateTodoDto updateTodoDto)
    {
        try
        {
            _logger.LogInformation("Starting todo update for ID: {TodoId}", id);

            // Validate that at least one property is being updated
            updateTodoDto.ValidateAtLeastOnePropertySet();

            // Get existing todo
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


    /// <summary>
    /// Delete specific Todo
    /// </summary>
    /// <param name="id">ID todo do usunięcia</param>
    /// <exception cref="NotFoundException">Wyrzucany, gdy todo nie zostanie znalezione</exception>
    public async Task DeleteTodoAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Rozpoczynanie usuwania todo o ID: {TodoId}", id);
            var todo = await _todoRepository.GetByIdAsync(id);
            if (todo == null)
            {
                throw new NotFoundException($"Todo o ID {id} nie został znaleziony.");
            }

            await _todoRepository.DeleteAsync(todo);
            _logger.LogInformation("Pomyślnie usunięto todo o ID: {TodoId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania todo o ID: {TodoId}", id);
            throw;
        }
    }

    public async Task<TodoDto> UpdateTodoCompletionAsync(Guid id, UpdateTodoCompletionDto updateDto)
    {
        try
        {
            _logger.LogInformation("Starting todo completion update for ID: {TodoId}", id);

            var todo = await _todoRepository.GetByIdAsync(id);
            if (todo == null)
            {
                throw new NotFoundException($"Todo with ID {id} not found");
            }

            // Update completion percentage
            todo.PercentComplete = updateDto.PercentComplete;

            // Automatically mark as done if 100% complete
            if (todo.PercentComplete == 100)
            {
                todo.IsDone = true;
            }

            var updatedTodo = await _todoRepository.UpdateAsync(todo);
            _logger.LogInformation("Successfully updated todo completion with ID: {TodoId}", id);

            return MapToDto(updatedTodo);
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("Todo not found during completion update: {TodoId}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating todo completion: {TodoId}", id);
            throw;
        }
    }

    public async Task<TodoDto> UpdateTodoDoneStatusAsync(Guid id, UpdateTodoDoneStatusDto updateDto)
    {
        try
        {
            _logger.LogInformation("Starting todo done status update for ID: {TodoId}", id);

            var todo = await _todoRepository.GetByIdAsync(id);
            if (todo == null)
            {
                throw new NotFoundException($"Todo with ID {id} not found");
            }

            // Update done status and set completion accordingly
            todo.IsDone = updateDto.IsDone;
            todo.PercentComplete = updateDto.IsDone ? 100 : 0;

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

