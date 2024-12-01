using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TodoTaskAPI.Application.DTOs;

namespace TodoTaskAPI.Application.Interfaces;

/// <summary>
/// Interface for Todo service operations
/// </summary>
public interface ITodoService
{
    /// <summary>
    /// Gets all todos without pagination
    /// </summary>
    /// <returns>Collection of todo DTOs</returns>
    Task<IEnumerable<TodoDto>> GetAllTodosAsync();

    /// <summary>
    /// Gets paginated todos
    /// </summary>
    /// <param name="parameters">Pagination parameters</param>
    /// <returns>Paginated response with todo DTOs</returns>
    Task<PaginatedResponseDto<TodoDto>> GetAllTodosWithPaginationAsync(PaginationParametersDto parameters);
}
