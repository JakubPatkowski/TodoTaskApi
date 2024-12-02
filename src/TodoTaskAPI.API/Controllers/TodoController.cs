using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TodoTaskAPI.Application.DTOs;
using TodoTaskAPI.Application.Interfaces;
using TodoTaskAPI.Core.Exceptions;
using ValidationException = TodoTaskAPI.Core.Exceptions.ValidationException;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TodosController : ControllerBase
{
    private readonly ITodoService _todoService;
    private readonly ILogger<TodosController> _logger;


    /// <summary>
    /// Initializes a new instance of the TodosController
    /// </summary>
    /// <param name="todoService">Todo service instance</param>
    /// <param name="logger">Logger instance</param>
    public TodosController(ITodoService todoService, ILogger<TodosController> logger)
    {
        _todoService = todoService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves todos with optional pagination sorted by expiry date
    /// </summary>
    /// <remarks>
    /// Sample requests:
    /// 
    /// GET /api/todos - Returns all todos, 
    /// 
    /// GET /api/todos?pageNumber=1&amp;pageSize=10 - Returns first page with 10 items
    /// </remarks>
    /// <param name="pageNumber">Optional page number (minimum: 1)</param>
    /// <param name="pageSize">Optional page size (minimum: 1, maximum: 100)</param>
    /// <returns>Standardized API response containin list of todos, either paginated or complete</returns>
    /// <response code="200">Successfully retrieved todos</response>
    /// <response code="200">Successfully retrieved paginated todos</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="429">Too many requests - rate limit exceeded</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<IEnumerable<TodoDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<PaginatedResponseDto<TodoDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<object>>> GetAll(
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        try
        {
            // If no pagination parameters provided, return all todos
            if (!pageNumber.HasValue && !pageSize.HasValue)
            {
                _logger.LogInformation("Getting all todos without pagination");
                var allTodos = await _todoService.GetAllTodosAsync();
                return Ok(ApiResponseDto<IEnumerable<TodoDto>>.Success(
                    allTodos,
                    "Successfully retrieved all todos"));
            }

            // Create and validate pagination parameters
            var parameters = new PaginationParametersDto
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // Validate consistency of pagination parameters
            parameters.ValidateConsistency();



            // Get paginated todos
            _logger.LogInformation(
                "Retrieving paginated todos. Page: {PageNumber}, Size: {PageSize}",
                pageNumber, pageSize);
            var paginatedTodos = await _todoService.GetAllTodosWithPaginationAsync(parameters);

            // Verify the requested page exists
            if (pageNumber > paginatedTodos.TotalPages)
            {
                throw new ValidationException(
                    $"Page number {pageNumber} exceeds total pages {paginatedTodos.TotalPages}");
            }

            return Ok(ApiResponseDto<PaginatedResponseDto<TodoDto>>.Success(
                paginatedTodos,
                "Successfully retrieved paginated todos"));
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error in GetAll endpoint");
            return BadRequest(ApiResponseDto<object>.Failure(
               StatusCodes.Status400BadRequest,
               ex.Message));
        }
        catch (TodoTaskAPI.Core.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error in GetAll endpoint");
            return BadRequest(ApiResponseDto<object>.Failure(
                StatusCodes.Status400BadRequest,
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetAll endpoint");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponseDto<object>.Failure(
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred while retrieving todos"));
        }


    }
}
