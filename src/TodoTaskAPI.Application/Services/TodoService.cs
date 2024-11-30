using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTaskAPI.Application.DTOs;
using TodoTaskAPI.Application.Interfaces;
using TodoTaskAPI.Core.Entities;
using TodoTaskAPI.Core.Interfaces;

namespace TodoTaskAPI.Application.Services;

public class TodoService : ITodoService
{
    private readonly ITodoRepository _todoRepository;

    public TodoService(ITodoRepository todoRepository)
    {
        _todoRepository = todoRepository;
    }

    public async Task<IEnumerable<TodoDto>> GetAllTodosAsync()
    {
        var todos = await _todoRepository.GetAllAsync();

        return todos.Select(todo => new TodoDto
        {
            Id = todo.Id,
            Title = todo.Title,
            Description = todo.Description,
            ExpiryDateTime = todo.ExpiryDateTime,
            PercentComplete = todo.PercentComplete,
            IsDone = todo.IsDone,
            CreatedAt = todo.CreatedAt,
            UpdatedAt = todo.UpdatedAt, 
        });
    }
}
