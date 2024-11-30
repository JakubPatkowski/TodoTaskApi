using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TodoTaskAPI.Application.DTOs;

namespace TodoTaskAPI.Application.Interfaces;

public interface ITodoService
{
    Task<IEnumerable<TodoDto>> GetAllTodosAsync();
}
