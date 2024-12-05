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
        /// Adds a new todo to the database
        /// </summary>
        Task<Todo> AddAsync(Todo todo);
    }
}

