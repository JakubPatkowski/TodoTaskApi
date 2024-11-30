using TodoTaskAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TodoTaskAPI.Core.Interfaces;
using TodoTaskAPI.Infrastructure.Repositories;
using TodoTaskAPI.Application.Interfaces;
using TodoTaskAPI.Application.Services;

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

builder.Services.AddControllers();


var app = builder.Build();


// Configure middleware
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

