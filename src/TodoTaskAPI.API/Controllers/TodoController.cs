using Microsoft.AspNetCore.Mvc;
using TodoTaskAPI.Application.DTOs;
using TodoTaskAPI.Application.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class TodosController : ControllerBase
{
    private readonly ITodoService _todoService;

    public TodosController(ITodoService todoService)
    {
        _todoService = todoService;
    }

    /// <summary>
    /// Retrieves a paginated list of todos
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 10, max: 50)</param>
    /// <returns>A paginated list of todos with metadata</returns>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponseDto<TodoDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var parameters = new PaginationParametersDto
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var paginatedTodos = await _todoService.GetAllTodosAsync(parameters);
        return Ok(paginatedTodos);
    }
}
