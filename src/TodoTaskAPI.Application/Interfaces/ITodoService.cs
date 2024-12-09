using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TodoTaskAPI.Application.DTOs;

namespace TodoTaskAPI.Application.Interfaces;

/// <summary>
/// Interface defining business logic operations for Todo items
/// Handles data validation, business rules, and orchestrates repository operations
/// </summary>
public interface ITodoService
{
    /// <summary>
    /// Retrieves all todos ordered by expiry date
    /// </summary>
    /// <returns>Collection of todo DTOs</returns>
    /// <exception cref="Exception">Thrown when database operation fails</exception>
    Task<IEnumerable<TodoDto>> GetAllTodosAsync();

    /// <summary>
    /// Retrieves paginated collection of todos with total count information
    /// </summary>
    /// <param name="parameters">Pagination settings including page number and size</param>
    /// <returns>Paginated response containing todos and metadata</returns>
    /// <exception cref="Core.Exceptions.ValidationException">Thrown when no search parameters are provided.</exception>
    /// <exception cref="Exception">Thrown when database operation fails</exception>
    Task<PaginatedResponseDto<TodoDto>> GetAllTodosWithPaginationAsync(PaginationParametersDto parameters);

    /// <summary>
    /// Retrieves todos matching specified search criteria
    /// </summary>
    /// <param name="parameters">Search parameters containing ID and/or title</param>
    /// <returns>Collection of matching todo DTOs</returns>
    /// <exception cref="Core.Exceptions.ValidationException">Thrown when search parameters are invalid</exception>
    /// <exception cref="Exception">Thrown when database operation fails</exception>
    Task<IEnumerable<TodoDto>> FindTodosAsync(TodoSearchParametersDto parameters);

    /// <summary>
    /// Creates a new todo item with business validation
    /// </summary>
    /// <param name="createTodoDto">Data for the new todo</param>
    /// <returns>Created todo as DTO with generated ID</returns>
    /// <exception cref="Core.Exceptions.ValidationException">Thrown when validation fails</exception>
    /// <exception cref="Exception">Thrown when create operation fails</exception>
    Task<TodoDto> CreateTodoAsync(CreateTodoDto createTodoDto);

    /// <summary>
    /// Retrieves todos with expiry dates within specified time period
    /// </summary>
    /// <param name="timePeriodDto">Time period specification (Today/Tomorrow/Week/Custom)</param>
    /// <returns>Collection of todos within the period</returns>
    /// <exception cref="Core.Exceptions.ValidationException">Thrown when time period parameters are invalid</exception>
    /// <exception cref="Exception">Thrown when database operation fails</exception>
    Task<IEnumerable<TodoDto>> GetTodosByTimePeriodAsync(TodoTimePeriodParametersDto timePeriodDto);

    /// <summary>
    /// Updates an existing todo with provided changes
    /// </summary>
    /// <param name="id">ID of todo to update</param>
    /// <param name="updateTodoDto">Data containing fields to update</param>
    /// <returns>Updated todo as DTO</returns>
    /// <exception cref="Core.Exceptions.ValidationException">Thrown when validation fails</exception>
    /// <exception cref="Core.Exceptions.NotFoundException">Thrown when todo not found</exception>
    /// <exception cref="Exception">Thrown when update operation fails</exception>
    Task<TodoDto> UpdateTodoAsync(Guid id, UpdateTodoDto updateTodoDto);

    /// <summary>
    /// Permanently deletes a todo item
    /// </summary>
    /// <param name="id">The ID of the todo to delete</param>
    /// <exception cref="Core.Exceptions.NotFoundException">Thrown when todo not found</exception>
    /// <exception cref="Exception">Thrown when delete operation fails</exception>
    Task DeleteTodoAsync(Guid id);

    /// <summary>
    /// Updates todo completion percentage and related status
    /// </summary>
    /// <param name="id">ID of todo to update</param>
    /// <param name="updateDto">New completion percentage (0-100)</param>
    /// <returns>Updated todo with new completion status</returns>
    /// <exception cref="Core.Exceptions.ValidationException">Thrown when percentage is invalid</exception>
    /// <exception cref="Core.Exceptions.NotFoundException">Thrown when todo not found</exception>
    /// <exception cref="Exception">Thrown when update fails</exception>
    Task<TodoDto> UpdateTodoCompletionAsync(Guid id, UpdateTodoCompletionDto updateDto);

    /// <summary>
    /// Changes todo done status and updates completion percentage
    /// </summary>
    /// <param name="id">ID of todo to update</param>
    /// <param name="updateDto">New done status (sets completion to 0/100)</param>
    /// <returns>Updated todo with new status</returns>
    /// <exception cref="Core.Exceptions.ValidationException">Thrown when input is invalid</exception>
    /// <exception cref="Core.Exceptions.NotFoundException">Thrown when todo not found</exception>
    /// <exception cref="Exception">Thrown when update fails</exception>
    Task<TodoDto> UpdateTodoDoneStatusAsync(Guid id, UpdateTodoDoneStatusDto updateDto);
}