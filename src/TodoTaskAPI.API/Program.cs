using TodoTaskAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TodoTaskAPI.Core.Interfaces;
using TodoTaskAPI.Infrastructure.Repositories;
using TodoTaskAPI.Application.Interfaces;
using TodoTaskAPI.Application.Services;
using TodoTaskAPI.API.Middleware;
using Microsoft.OpenApi.Models;
using System.Reflection;
using TodoTaskAPI.API.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add Repositories
builder.Services.AddScoped<ITodoRepository, TodoRepository>();



// Add Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ITodoService, TodoService>();


// Add Database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Swagger Configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Todo Tasks API",
        Version = "v1",
        Description = "An API for managing todo tasks"
    });

    // Enable XML comments in Swagger
    try
    {
        var xmlFile = "TodoTaskAPI.API.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
        else
        {
            // Log warning but don't crash the application
            var logger = LoggerFactory.Create(builder => builder.AddConsole())
                                    .CreateLogger("Startup");
            logger.LogWarning("XML documentation file not found at: {Path}", xmlPath);
        }
    }
    catch (Exception ex)
    {
        // Log error but don't crash the application
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
                                .CreateLogger("Startup");
        logger.LogError(ex, "Error loading XML documentation file");
    }
});

builder.Services.AddControllers();


var app = builder.Build();


// Configure middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

app.MapControllers();

//app.UseHttpsRedirection();

// Migration
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Data Initialization
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    await DbInitializer.Initialize(context);
}

app.Run();

