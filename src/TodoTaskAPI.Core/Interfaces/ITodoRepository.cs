using System;
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
        /// Retrieves all todos from the database without pagination, ordered by expiry date.
        /// </summary>
        /// <returns>An ordered collection of all todo items.</returns>
        /// <remarks>
        /// For large datasets, consider using <see cref="GetAllWithPaginationAsync"/> instead to improve performance.
        /// The todos are ordered by expiry date in ascending order.
        /// </remarks>
        /// <exception cref="Exception">Thrown when the database query fails.</exception>
        Task<IEnumerable<Todo>> GetAllAsync();

        /// <summary>
        /// Retrieves a paginated list of todos with total count information.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve (1-based indexing).</param>
        /// <param name="pageSize">Number of items per page (recommended: 10-50).</param>
        /// <returns>
        /// A tuple containing:
        /// - Items: The paginated collection of todos.
        /// - TotalCount: Total number of todos in the database.
        /// </returns>
        /// <remarks>
        /// Results are ordered by expiry date in ascending order.
        /// Use this method for large datasets to improve performance and reduce memory usage.
        /// </remarks>
        /// <exception cref="Exceptions.ValidationException">Thrown when parameters validations fails</exception>
        /// <exception cref="Exception">Thrown when the database query fails.</exception>
        Task<(IEnumerable<Todo> Items, int TotalCount)> GetAllWithPaginationAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Searches for todos based on ID and/or title criteria.
        /// </summary>
        /// <param name="id">Optional GUID to find a specific todo</param>
        /// <param name="title">Optional title to search for (case-insensitive)</param>
        /// <returns>Collection of todos matching the search criteria</returns>
        /// <remarks>
        /// - If both parameters are provided, returns todos matching both criteria
        /// - If no parameters are provided, throws a validation exception
        /// - Title search is case-insensitive and supports international characters
        /// - Results are ordered by expiry date in descending order
        /// </remarks>
        /// <exception cref="Exceptions.ValidationException">Thrown when parameters validations fails</exception>
        /// <exception cref="Exception">Thrown when the database query fails.</exception>
        Task<IEnumerable<Todo>> FindTodosAsync(Guid? id = null, string? title = null);

        /// <summary>
        /// Retrieves todos with expiry dates within the specified date range.
        /// </summary>
        /// <param name="startDate">Start date of the range (inclusive)</param>
        /// <param name="endDate">End date of the range (inclusive)</param>
        /// <returns>Collection of todos with expiry dates within the range</returns>
        /// <remarks>
        /// - Dates are compared based on their date component only (time is ignored)
        /// - Results are ordered by expiry date in ascending order
        /// - Both start and end dates are inclusive in the range
        /// </remarks>
        /// <exception cref="Exceptions.ValidationException">Thrown when parameters validations fails</exception>
        /// <exception cref="Exception">Thrown when database query fails</exception>
        Task<IEnumerable<Todo>> GetTodosByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Creates a new todo item in the database.
        /// </summary>
        /// <param name="todo">The todo entity to add</param>
        /// <returns>The created todo with generated ID and timestamps</returns>
        /// <remarks>
        /// - Automatically sets CreatedAt timestamp to UTC
        /// - Generates a new GUID for the todo ID
        /// - Uses database transaction for data consistency
        /// </remarks>
        /// <exception cref="Exceptions.ValidationException">Thrown when parameters validations fails</exception>
        /// <exception cref="DbUpdateException">Thrown when database insert fails</exception>
        /// <exception cref="Exception">Thrown when transaction or general error occurs</exception>
        Task<Todo> AddAsync(Todo todo);

        /// <summary>
        /// Updates an existing todo in the database.
        /// </summary>
        /// <param name="todo">The todo entity with updated values</param>
        /// <returns>The updated todo entity</returns>
        /// <remarks>
        /// - Automatically updates UpdatedAt timestamp to UTC
        /// - Uses database transaction for data consistency
        /// - Handles both in-memory and real database scenarios
        /// </remarks>
        /// <exception cref="DbUpdateException">Thrown when database update fails</exception>
        /// <exception cref="Exception">Thrown when transaction rollback fails</exception>
        Task<Todo> UpdateAsync(Todo todo);

        /// <summary>
        /// Retrieves a specific todo by its unique identifier.
        /// </summary>
        /// <param name="id">The GUID of the todo to retrieve</param>
        /// <returns>The matching todo entity or null if not found</returns>
        /// <remarks>
        /// This is a direct lookup by primary key and is optimized for performance.
        /// </remarks>
        /// <exception cref="Exception">Thrown when database query fails</exception>
        Task<Todo?> GetByIdAsync(Guid id);

        /// <summary>
        /// Permanently removes a todo from the database.
        /// </summary>
        /// <param name="todo">The todo entity to delete</param>
        /// <remarks>
        /// - This operation cannot be undone
        /// - Uses optimistic concurrency checking
        /// - Throws concurrency exception if the todo was modified by another process
        /// </remarks>
        /// <exception cref="DbUpdateConcurrencyException">Thrown when concurrency conflict occurs</exception>
        /// <exception cref="Exception">Thrown when delete operation fails</exception>
        Task DeleteAsync(Todo todo);
    }
}

