using System;
using Microsoft.EntityFrameworkCore;
using TodoTaskAPI.Core.Entities;
using TodoTaskAPI.Core.Interfaces;
using TodoTaskAPI.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace TodoTaskAPI.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Todo entity
/// </summary>
public class TodoRepository : ITodoRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TodoRepository> _logger;

    /// <summary>
    /// Initializes a new instance of TodoRepository
    /// </summary>
    /// <param name="context">Database context</param>
    public TodoRepository(ApplicationDbContext context, ILogger<TodoRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Todo>> GetAllAsync()
    {
        return await _context.Todos
            .OrderByDescending(t => t.ExpiryDateTime)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<Todo> Items, int TotalCount)> GetAllWithPaginationAsync(int pageNumber, int pageSize)
    {
        var totalCount = await _context.Todos.CountAsync();

        var items = await _context.Todos
            .OrderByDescending(t => t.ExpiryDateTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <summary>
    /// Finds todos matching the specified criteria with proper error handling
    /// </summary>
    /// <param name="id">Optional ID to search by</param>
    /// <param name="title">Optional title to search by (case-insensitive)</param>
    /// <returns>Collection of matching todos</returns>
    public async Task<IEnumerable<Todo>> FindTodosAsync(Guid? id = null, string? title = null)
    {
        try
        {
            var query = _context.Todos.AsQueryable();

            if (id.HasValue)
            {
                query = query.Where(t => t.Id == id);
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                // Use EF.Functions.ILike for case-insensitive comparison that works with international characters
                query = query.Where(t => EF.Functions.ILike(t.Title, title));
            }

            return await query
                .OrderByDescending(t => t.ExpiryDateTime)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching for todos. ID: {Id}, Title: {Title}", id, title);
            throw;
        }
    }

    /// <summary>
    /// Adds a new todo to the database with proper error handling and transaction management.
    /// </summary>
    /// <param name="todo">Todo entity to be persisted</param>
    /// <returns>The persisted todo entity with generated Id</returns>
    /// <exception cref="DbUpdateException">Thrown when database update fails</exception>
    /// <remarks>
    /// Uses transactions for data consistency when not using in-memory database.
    /// Includes proper logging for debugging and monitoring.
    /// </remarks>
    public async Task<Todo> AddAsync(Todo todo)
    {
        try
        {
            // Check if we're using the in-memory database
            if (_context.Database.ProviderName?.Contains("InMemory") ?? false)
            {
                _context.Todos.Add(todo);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully added todo with ID: {TodoId} using in-memory store", todo.Id);
                return todo;
            }

            // For real database, use transaction
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Todos.Add(todo);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully added todo with ID: {TodoId}", todo.Id);
                return todo;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to add todo to database");
            throw new DbUpdateException("Failed to save todo to database", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while adding todo");
            throw;
        }
    }

    /// <summary>
    /// Gets todos within specified date range with proper error handling
    /// </summary>
    /// <param name="startDate">Start of the date range (inclusive)</param>
    /// <param name="endDate">End of the date range (inclusive)</param>
    /// <returns>Collection of todos within the date range</returns>
    public async Task<IEnumerable<Todo>> GetTodosByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving todos between {StartDate} and {EndDate}",
                startDate, endDate);

            return await _context.Todos
                .Where(t => t.ExpiryDateTime.Date >= startDate.Date &&
                           t.ExpiryDateTime.Date <= endDate.Date)
                .OrderBy(t => t.ExpiryDateTime)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error occurred while retrieving todos by date range. StartDate: {StartDate}, EndDate: {EndDate}",
                startDate, endDate);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing todo with proper error handling and transaction management
    /// </summary>
    /// <param name="todo">Todo entity to update</param>
    /// <returns>Updated todo entity</returns>
    /// <exception cref="DbUpdateException">Thrown when database update fails</exception>
    public async Task<Todo> UpdateAsync(Todo todo)
    {
        try
        {
            todo.UpdatedAt = DateTime.UtcNow;

            if (_context.Database.ProviderName?.Contains("InMemory") ?? false)
            {
                _context.Todos.Update(todo);
                await _context.SaveChangesAsync();
                return todo;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Todos.Update(todo);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully updated todo with ID: {TodoId}", todo.Id);
                return todo;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update todo in database");
            throw;
        }
    }

    /// <summary>
    /// Gets a specific todo by ID with error handling
    /// </summary>
    /// <param name="id">Todo ID to find</param>
    /// <returns>Todo entity if found, null if not found</returns>
    public async Task<Todo?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.Todos.FirstOrDefaultAsync(t => t.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving todo with ID: {TodoId}", id);
            throw;
        }
    }
}
