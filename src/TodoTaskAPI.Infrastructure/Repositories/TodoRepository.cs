using System;
using Microsoft.EntityFrameworkCore;
using TodoTaskAPI.Core.Entities;
using TodoTaskAPI.Core.Interfaces;
using TodoTaskAPI.Infrastructure.Data;

namespace TodoTaskAPI.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Todo entity
/// </summary>
public class TodoRepository : ITodoRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of TodoRepository
    /// </summary>
    /// <param name="context">Database context</param>
    public TodoRepository(ApplicationDbContext context)
    {
        _context = context;
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
}
