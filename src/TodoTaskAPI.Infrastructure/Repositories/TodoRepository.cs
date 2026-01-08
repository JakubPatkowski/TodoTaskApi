using System;
using Microsoft.EntityFrameworkCore;
using TodoTaskAPI.Core.Entities;
using TodoTaskAPI.Core.Interfaces;
using TodoTaskAPI.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using TodoTaskAPI.Core.Exceptions;

namespace TodoTaskAPI.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Todo entity
/// </summary>
public class TodoRepository : ITodoRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TodoRepository> _logger;

    /// <inheritdoc/>
    public TodoRepository(ApplicationDbContext context, ILogger<TodoRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <exception cref="Exception">Thrown when the database query fails.</exception>
    public async Task<IEnumerable<Todo>> GetAllAsync()
    {
        return await _context.Todos
            .OrderBy(t => t.ExpiryDateTime)
            .ToListAsync();
    }

    /// <inheritdoc/>
    /// <exception cref="ValidationException">Thrown when parameters validations fails</exception>
    /// <exception cref="Exception">Thrown when the database query fails.</exception>
    public async Task<(IEnumerable<Todo> Items, int TotalCount)> GetAllWithPaginationAsync(int pageNumber, int pageSize)
    {
        // Get total count of todos for pagination metadata
        var totalCount = await _context.Todos.CountAsync();

        // Calculate skip count and fetch paginated items
        var items = await _context.Todos
            .OrderBy(t => t.ExpiryDateTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <inheritdoc/>
    /// <exception cref="ValidationException">Thrown when parameters validations fails</exception>
    /// <exception cref="Exception">Thrown when database query fails</exception>
    public async Task<IEnumerable<Todo>> FindTodosAsync(Guid? id = null, string? title = null)
    {
        try
        {
            // Start with base query
            var query = _context.Todos.AsQueryable();

            // Apply ID filter if provide
            if (id.HasValue)
            {
                query = query.Where(t => t.Id == id);
            }

            // Apply case-insensitive title filter if provided
            if (!string.IsNullOrWhiteSpace(title))
            {
                // Use EF.Functions.ILike for case-insensitive comparison that works with international characters
                query = query.Where(t => EF.Functions.ILike(t.Title, title));
            }

            // Execute query with ordering and return results
            return await query
                .OrderByDescending(t => t.ExpiryDateTime)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching for todos. ID: {Id}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    /// <exception cref="ValidationException">Thrown when parameters validations fails</exception>
    /// <exception cref="DbUpdateException">Thrown when database insert fails</exception>
    /// <exception cref="Exception">Thrown when transaction or general error occurs</exception>
    public async Task<Todo> AddAsync(Todo todo)
    {
        try
        {
            // Handle in-memory database scenario differently
            if (_context.Database.ProviderName?.Contains("InMemory") ?? false)
            {
                _context.Todos.Add(todo);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully added todo with ID: {TodoId} using in-memory store", todo.Id);
                return todo;
            }

            // For real database, use transaction for data consistency
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Add and save the todo
                _context.Todos.Add(todo);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Commit transaction if save was successful
                _logger.LogInformation("Successfully added todo with ID: {TodoId}", todo.Id);
                return todo;
            }
            catch (Exception)
            {
                // Rollback transaction on any error
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

    /// <inheritdoc/>
    /// <exception cref="ValidationException">Thrown when parameters validations fails</exception>
    /// <exception cref="Exception">Thrown when database query fails</exception>
    public async Task<IEnumerable<Todo>> GetTodosByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving todos between {StartDate} and {EndDate}",
                startDate, endDate);

            return await _context.Todos
                // Filter by date range, comparing only date parts
                .Where(t => t.ExpiryDateTime.Date >= startDate.Date &&
                           t.ExpiryDateTime.Date <= endDate.Date)
                // Order results by expiry date
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

    /// <inheritdoc/>
    /// <exception cref="ValidationException">Thrown when parameters validations fails</exception>
    /// <exception cref="DbUpdateException">Thrown when database update fails</exception>
    /// <exception cref="Exception">Thrown when transaction rollback fails</exception>
    public async Task<Todo> UpdateAsync(Todo todo)
    {
        try
        {
            // Update the modification timestamp
            todo.UpdatedAt = DateTime.UtcNow;

            // Handle in-memory database scenario
            if (_context.Database.ProviderName?.Contains("InMemory") ?? false)
            {
                _context.Todos.Update(todo);
                await _context.SaveChangesAsync();
                return todo;
            }

            // Use transaction for real database updates
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Todos.Update(todo);
                await _context.SaveChangesAsync();

                // Commit transaction if successful
                await transaction.CommitAsync();
                _logger.LogInformation("Successfully updated todo with ID: {TodoId}", todo.Id);
                return todo;
            }
            catch
            {
                // Rollback on any error
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

    /// <inheritdoc/>
    /// <exception cref="Exception">Thrown when database query fails</exception>
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

    /// <inheritdoc/>
    /// <exception cref="DbUpdateConcurrencyException">Thrown when concurrency conflict occurs</exception>
    /// <exception cref="Exception">Thrown when delete operation fails</exception>
    public async Task DeleteAsync(Todo todo)
    {
        try
        {
            _context.Todos.Remove(todo);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Handle concurrency conflict
            _logger.LogError(ex, "Błąd aktualizacji concurrency podczas usuwania todo o ID: {TodoId}", todo.Id);
            throw new Exception("Wystąpił błąd aktualizacji concurrency podczas usuwania todo.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania todo o ID: {TodoId}", todo.Id);
            throw;
        }
    }
}
