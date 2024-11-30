using System;
using Microsoft.EntityFrameworkCore;
using TodoTaskAPI.Core.Entities;
using TodoTaskAPI.Core.Interfaces;
using TodoTaskAPI.Infrastructure.Data;

namespace TodoTaskAPI.Infrastructure.Repositories;

public class TodoRepository : ITodoRepository
{
    private readonly ApplicationDbContext _context;

    public TodoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Todo>> GetAllAsync()
    {
        return await _context.Todos.ToListAsync();
    }
}
