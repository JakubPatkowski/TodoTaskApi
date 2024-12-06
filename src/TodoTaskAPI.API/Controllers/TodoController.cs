using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TodoTaskAPI.Application.DTOs;
using TodoTaskAPI.Application.Interfaces;
using TodoTaskAPI.Core.Exceptions;
using ValidationException = TodoTaskAPI.Core.Exceptions.ValidationException;


/// <summary>
/// Controller handling CRUD operations for Todo items.
/// Implements rate limiting, validation, and proper error handling.
/// </summary>
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
            _logger.LogInformation("Starting todos get");

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
        // Validation params exception
        catch (System.ComponentModel.DataAnnotations.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error in GetAll endpoint");
            return BadRequest(ApiResponseDto<object>.Failure(
               StatusCodes.Status400BadRequest,
               ex.Message));
        }
        // Validation DTO exception
        catch (TodoTaskAPI.Core.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error in GetAll endpoint");
            return BadRequest(ApiResponseDto<object>.Failure(
                StatusCodes.Status400BadRequest,
                ex.Message));
        }
        // Unexpected error handling block
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


    /// <summary>
    /// Finds specific todos based on ID or title
    /// </summary>
    /// <remarks>
    /// Sample requests:
    /// 
    ///     GET /api/todos/search?id=123e4567-e89b-12d3-a456-426614174000
    ///     GET /api/todos/search?title=Complete project
    /// 
    /// At least one parameter (id or title) must be provided
    /// </remarks>
    /// <param name="parameters">Search parameters (ID or title)</param>
    /// <returns>Collection of matching todos</returns>
    /// <response code="200">Returns matching todos</response>
    /// <response code="400">If the search parameters are invalid</response>
    /// <response code="404">If not found specific todo</response>
    /// <response code="429">Too many requests - rate limit exceeded</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponseDto<IEnumerable<TodoDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<ValidationErrorResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<TodoDto>>>> FindTodos(
    [FromQuery] TodoSearchParametersDto parameters)
    {
        try
        {
            _logger.LogInformation("Starting todo search with parameters: ID: {Id}, Title: {Title}",
                parameters.Id, parameters.Title);

            var todos = await _todoService.FindTodosAsync(parameters);

            if (!todos.Any())
            {
                return NotFound(ApiResponseDto<IEnumerable<TodoDto>>.Failure(
                    StatusCodes.Status404NotFound,
                    "No todos found matching the specified criteria",
                    todos));
            }

            return Ok(ApiResponseDto<IEnumerable<TodoDto>>.Success(
                todos,
                "Successfully retrieved matching todos"));
        }
        catch (TodoTaskAPI.Core.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error in FindTodos endpoint");
            return BadRequest(ApiResponseDto<ValidationErrorResponse>.Failure(
                StatusCodes.Status400BadRequest,
                ex.Message,
                new ValidationErrorResponse
                {
                    Errors = new Dictionary<string, string[]>
                    {
                    { "Validation", new[] { ex.Message } }
                    }
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in FindTodos endpoint");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponseDto<object>.Failure(
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred while searching for todos"));
        }
    }

    /// <summary>
    /// Creates a new todo item
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/todos
    ///     {
    ///         "title": "Complete project",
    ///         "description": "Finish the REST API implementation",
    ///         "expiryDateTime": "2024-12-31T23:59:59Z",
    ///         "percentComplete": 0
    ///     }
    /// 
    /// </remarks>
    /// <param name="createTodoDto">Todo creation data</param>
    /// <returns>Created todo item</returns>
    /// <response code="201">Returns the newly created todo</response>
    /// <response code="400">If the todo data is invalid</response>
    /// <response code="429">Too many requests - rate limit exceeded</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponseDto<TodoDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseDto<ValidationErrorResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<TodoDto>>> Create([FromBody] CreateTodoDto createTodoDto)
    {
        try
        {
            _logger.LogInformation("Starting todo creation process for title: {Title}", createTodoDto.Title);

            // Model state validation block - validates incoming DTO against data annotations
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .Where(x => x.Value?.Errors.Any() == true)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );

                _logger.LogWarning("Model validation failed for todo creation. Errors: {@ValidationErrors}", validationErrors);

                return BadRequest(ApiResponseDto<ValidationErrorResponse>.Failure(
                    StatusCodes.Status400BadRequest,
                    "One or more validation errors occurred",
                    new ValidationErrorResponse { Errors = validationErrors }
                ));
            }

            var todo = await _todoService.CreateTodoAsync(createTodoDto);

            _logger.LogInformation("Successfully created todo with ID: {TodoId}", todo.Id);

            return Created(
                $"api/todos/{todo.Id}",
                ApiResponseDto<TodoDto>.Success(
                    todo,
                    $"Todo '{todo.Title}' created successfully with ID: {todo.Id}")
            );
        }
        catch (TodoTaskAPI.Core.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred");
            return BadRequest(ApiResponseDto<ValidationErrorResponse>.Failure(
                StatusCodes.Status400BadRequest,
                ex.Message,
                new ValidationErrorResponse
                {
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Validation", new[] { ex.Message } }
                    }
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating todo");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponseDto<ValidationErrorResponse>.Failure(
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred while creating the todo"+ex.Message));
        }
    }

    /// <summary>
    /// Gets todos within specified time period
    /// </summary>
    /// <remarks>
    /// Sample requests:
    /// 
    ///     GET /api/todos/upcoming?period=Today
    ///     GET /api/todos/upcoming?period=Tomorrow
    ///     GET /api/todos/upcoming?period=CurrentWeek
    ///     GET /api/todos/upcoming?period=Custom&amp;startDate=2024-12-31&amp;endDate=2025-01-07
    /// 
    /// </remarks>
    /// <param name="timePeriodDto">Time period parameters</param>
    /// <returns>Collection of todos within the specified period</returns>
    /// <response code="200">Returns matching todos</response>
    /// <response code="400">If the parameters are invalid</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(ApiResponseDto<IEnumerable<TodoDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<ValidationErrorResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<TodoDto>>>> GetUpcoming(
        [FromQuery] TodoTimePeriodParametersDto timePeriodDto)
    {
        try
        {
            _logger.LogInformation(
                "Getting upcoming todos for period: {Period}",
                timePeriodDto.Period);

            var todos = await _todoService.GetTodosByTimePeriodAsync(timePeriodDto);

            return Ok(ApiResponseDto<IEnumerable<TodoDto>>.Success(
                todos,
                $"Successfully retrieved todos for period: {timePeriodDto.Period}"));
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error in GetUpcoming endpoint");
            return BadRequest(ApiResponseDto<ValidationErrorResponse>.Failure(
                StatusCodes.Status400BadRequest,
                ex.Message,
                new ValidationErrorResponse
                {
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Validation", new[] { ex.Message } }
                    }
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetUpcoming endpoint");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponseDto<object>.Failure(
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred while retrieving upcoming todos"));
        }
    }

    /// <summary>
    /// Updates an existing todo item
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/todos/{id}
    ///     {
    ///         "title": "Updated title",
    ///         "description": "Updated description",
    ///         "expiryDateTime": "2024-12-31T23:59:59Z",
    ///         "percentComplete": 50,
    ///         "isDone": false
    ///     }
    /// 
    /// Only provide the properties you want to update. Omitted properties will remain unchanged.
    /// </remarks>
    /// <param name="id">ID of todo to update</param>
    /// <param name="updateTodoDto">Update data</param>
    /// <returns>Updated todo item</returns>
    /// <response code="200">Returns the updated todo</response>
    /// <response code="400">If the update data is invalid</response>
    /// <response code="404">If todo with specified ID is not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponseDto<TodoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<ValidationErrorResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponseDto<TodoDto>>> Update(Guid id, UpdateTodoDto updateTodoDto)
    {
        try
        {
            _logger.LogInformation("Starting todo update for ID: {TodoId}", id);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );

                return BadRequest(ApiResponseDto<ValidationErrorResponse>.Failure(
                    StatusCodes.Status400BadRequest,
                    "Validation failed",
                    new ValidationErrorResponse { Errors = errors }
                ));
            }

            var updatedTodo = await _todoService.UpdateTodoAsync(id, updateTodoDto);

            return Ok(ApiResponseDto<TodoDto>.Success(
                updatedTodo,
                $"Todo '{updatedTodo.Title}' updated successfully"));
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred during todo update");
            return BadRequest(ApiResponseDto<ValidationErrorResponse>.Failure(
                StatusCodes.Status400BadRequest,
                ex.Message,
                new ValidationErrorResponse
                {
                    Errors = new Dictionary<string, string[]>
                    {
                    { "Validation", new[] { ex.Message } }
                    }
                }));
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Todo not found during update");
            return NotFound(ApiResponseDto<object>.Failure(
                StatusCodes.Status404NotFound,
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating todo");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponseDto<object>.Failure(
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred while updating the todo"));
        }
    }
}

/// <summary>
/// Response model for validation errors
/// </summary>
public class ValidationErrorResponse
{
    public Dictionary<string, string[]> Errors { get; set; } = new();
}