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

    // src/TodoTaskAPI.Application/Services/TodoService.cs
    // Add this method to the TodoService class
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
}
