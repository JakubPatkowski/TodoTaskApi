using System;
using Microsoft.EntityFrameworkCore;
using TodoTaskAPI.Core.Entities;
using TodoTaskAPI.Core.Interfaces;
using TodoTaskAPI.Infrastructure.Data;

namespace TodoTaskAPI.Infrastructure.Repositories;

public class TodoRepository : ITodoRepository
{
    // Application database context
    private readonly ApplicationDbContext _context;

    // Injection context
    public TodoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Returns a tuple with the paginated items and total count
    public async Task<(IEnumerable<Todo> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize)
    {
        var totalCount = await _context.Todos.CountAsync();

        var items = await _context.Todos
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
