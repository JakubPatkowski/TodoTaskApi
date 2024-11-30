using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.DTOs;
using TodoTaskAPI.Application.Interfaces;
using TodoTaskAPI.Core.Entities;
using TodoTaskAPI.Core.Interfaces;

namespace TodoTaskAPI.Application.Services;

public class TodoService : ITodoService
{
    private readonly ITodoRepository _todoRepository;

    public TodoService(ITodoRepository todoRepository)
    {
        _todoRepository = todoRepository;
    }

    // returns paginated response with paginated List of Todos
    public async Task<PaginatedResponseDto<TodoDto>> GetAllTodosAsync(PaginationParametersDto paginationParameters)
    {
        var (todos, totalCount) = await _todoRepository.GetAllAsync(paginationParameters.PageNumber, paginationParameters.PageSize);

        var totalPages = (int)Math.Ceiling(totalCount / (double)paginationParameters.PageSize);

        var todoDtos = todos.Select(todo => new TodoDto
        {
            Id = todo.Id,
            Title = todo.Title,
            Description = todo.Description,
            ExpiryDateTime = todo.ExpiryDateTime,
            PercentComplete = todo.PercentComplete,
            IsDone = todo.IsDone,
            CreatedAt = todo.CreatedAt,
            UpdatedAt = todo.UpdatedAt, 
        });                                

        return new PaginatedResponseDto<TodoDto>
        {
            Items = todoDtos,
            PageNumber = paginationParameters.PageNumber,
            PageSize = paginationParameters.PageSize,
            TotalPages = totalPages,
            TotalCount = totalCount,
            HasNextPage = paginationParameters.PageNumber < totalPages,
            HasPreviousPage = paginationParameters.PageNumber > 1
        };
    }
}
