using Microsoft.AspNetCore.Mvc;
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

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var todos = await _todoService.GetAllTodosAsync();
        return Ok(todos);
    }
}
