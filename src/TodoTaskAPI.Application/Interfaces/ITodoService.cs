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

    /// <summary>
    /// Finds specific todos based on search parameters
    /// </summary>
    /// <param name="parameters">Search parameters</param>
    /// <returns>Collection of matching todo DTOs</returns>
    Task<IEnumerable<TodoDto>> FindTodosAsync(TodoSearchParametersDto parameters);

    /// <summary>
    /// Creates a new todo item
    /// </summary>
    /// <param name="createTodoDto">Data for creating new todo</param>
    /// <returns>Created todo as DTO</returns>
    /// <exception cref="ValidationException">Thrown when validation fails</exception>
    Task<TodoDto> CreateTodoAsync(CreateTodoDto createTodoDto);

    /// <summary>
    /// Creates a new todo item
    /// </summary>
    /// <param name="TodoTimePeriodParametersDto">Data for creating new todo</param>
    /// <returns>Created todo as DTO</returns>
    /// <exception cref="ValidationException">Thrown when validation fails</exception>
    Task<IEnumerable<TodoDto>> GetTodosByTimePeriodAsync(TodoTimePeriodParametersDto timePeriodDto);

    /// <summary>
    /// Updates an existing todo
    /// </summary>
    /// <param name="id">ID of todo to update</param>
    /// <param name="updateTodoDto">Update data</param>
    /// <returns>Updated todo as DTO</returns>
    Task<TodoDto> UpdateTodoAsync(Guid id, UpdateTodoDto updateTodoDto);


    // <summary>
    /// Deletes a todo item by its ID.
    /// </summary>
    /// <param name="id">The ID of the todo item to delete.</param>
    /// <exception cref="NotFoundException">Thrown when the todo item is not found.</exception>
    Task DeleteTodoAsync(Guid id);

    /// <summary>
    /// Updates the completion percentage of a todo
    /// Automatically marks todo as done when percentage reaches 100
    /// </summary>
    /// <param name="id">ID of todo to update</param>
    /// <param name="updateDto">Update data containing new completion percentage</param>
    /// <returns>Updated todo as DTO</returns>
    Task<TodoDto> UpdateTodoCompletionAsync(Guid id, UpdateTodoCompletionDto updateDto);

    /// <summary>
    /// Updates the done status of a todo
    /// Automatically sets completion percentage based on done status
    /// </summary>
    /// <param name="id">ID of todo to update</param>
    /// <param name="updateDto">Update data containing new done status</param>
    /// <returns>Updated todo as DTO</returns>
    Task<TodoDto> UpdateTodoDoneStatusAsync(Guid id, UpdateTodoDoneStatusDto updateDto);
}
