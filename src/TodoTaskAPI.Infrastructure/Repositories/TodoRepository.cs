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
}
