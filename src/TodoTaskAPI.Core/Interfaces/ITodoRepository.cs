﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Core.Entities;

namespace TodoTaskAPI.Core.Interfaces
{
    /// <summary>
    /// Interface defining operations for Todo repository
    /// </summary>
    public interface ITodoRepository
    {
        /// <summary>
        /// Gets all todos without pagination
        /// </summary>
        /// <returns>Collection of todos</returns>
        Task<IEnumerable<Todo>> GetAllAsync();

        /// <summary>
        /// Gets paginated todos with total count
        /// </summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Items per page</param>
        /// <returns>Tuple containing paginated items and total count</returns>
        Task<(IEnumerable<Todo> Items, int TotalCount)> GetAllWithPaginationAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Finds todos matching the specified criteria
        /// </summary>
        /// <param name="id">Optional ID to search by</param>
        /// <param name="title">Optional title to search by</param>
        /// <returns>Collection of matching todos</returns>
        Task<IEnumerable<Todo>> FindTodosAsync(Guid? id = null, string? title = null);

        /// <summary>
        /// Gets todos within specified date range
        /// </summary>
        /// <param name="startDate">Start of the date range</param>
        /// <param name="endDate">End of the date range</param>
        /// <returns>Collection of todos within the date range</returns>
        Task<IEnumerable<Todo>> GetTodosByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Adds a new todo to the database
        /// </summary>
        /// 
        Task<Todo> AddAsync(Todo todo);

        /// <summary>
        /// Updates an existing todo in the database
        /// </summary>
        /// <param name="todo">Todo entity to update</param>
        /// <returns>Updated todo entity</returns>
        Task<Todo> UpdateAsync(Todo todo);

        /// <summary>
        /// Gets a specific todo by ID
        /// </summary>
        /// <param name="id">Todo ID</param>
        /// <returns>Todo entity if found, null if not found</returns>
        Task<Todo?> GetByIdAsync(Guid id);

        /// <summary>
        /// Deletes a specific todo from the database.
        /// </summary>
        /// <param name="todo">The todo entity to delete.</param>x
        Task DeleteAsync(Todo todo);
    }
}

