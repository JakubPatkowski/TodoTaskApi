using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using TodoTaskAPI.API.Middleware;
using TodoTaskAPI.API.Swagger;
using TodoTaskAPI.Application.Interfaces;
using TodoTaskAPI.Application.Services;
using TodoTaskAPI.Core.Interfaces;
using TodoTaskAPI.Infrastructure.Data;
using TodoTaskAPI.Infrastructure.Repositories;

namespace TodoTaskAPI.API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Configures application services
        /// </summary>
        /// <param name="services">Service collection to configure</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Register repository and service dependencies
            services.AddScoped<ITodoRepository, TodoRepository>();
            services.AddScoped<ITodoService, TodoService>();

            // Configure Swagger/OpenAPI
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SchemaFilter<EnumSchemaFilter>();
            });

            // Configure JSON serialization
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            // Configure database context based on environment
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing")
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            }
            else
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));
            }

            // Configure CORS
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            services.AddControllers();
        }

        /// <summary>
        /// Configures the HTTP request pipeline
        /// </summary>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Initialize database
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Startup>>();

                try
                {
                    // Migrations
                    context.Database.Migrate();

                    // Seed data
                    DbInitializer.Initialize(context).GetAwaiter().GetResult();

                    logger.LogInformation("Database initialization completed");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while initializing the database");
                    throw;
                }
            }

            // Configure middleware pipeline
            app.UseMiddleware<RequestLoggingMiddleware>();
            app.UseMiddleware<RateLimitingMiddleware>();
            app.UseMiddleware<ErrorHandlingMiddleware>();

            // Enable Swagger
            app.UseSwagger();
            app.UseSwaggerUI();

            // Configure routing and endpoints
            app.UseCors();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
