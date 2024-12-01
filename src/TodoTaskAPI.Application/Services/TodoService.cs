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

namespace TodoTaskAPI.Application.Services;

/// <summary>
/// Service implementation for Todo operations
/// </summary>
public class TodoService : ITodoService
{
    private readonly ITodoRepository _todoRepository;

    /// <summary>
    /// Initializes a new instance of TodoService
    /// </summary>
    /// <param name="todoRepository">Todo repository</param>
    public TodoService(ITodoRepository todoRepository)
    {
        _todoRepository = todoRepository;
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
    /// Maps Todo entity to TodoDto
    /// </summary>
    private static TodoDto MapToDto(Todo todo) => new()
    {
        Id = todo.Id,
        Title = todo.Title,
        Description = todo.Description,
        ExpiryDateTime = todo.ExpiryDateTime,
        PercentComplete = todo.PercentComplete,
        IsDone = todo.IsDone,
        CreatedAt = todo.CreatedAt,
        UpdatedAt = todo.UpdatedAt
    };
}
