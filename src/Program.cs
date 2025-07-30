using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using WikiRAG.Data;
using WikiRAG.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// Configure PostgreSQL with PgVector
builder.Services.AddDbContext<WikiRagDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseVector()));

// Register services
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IChunkingService, ChunkingService>();

// Add health checks with database check
builder.Services.AddHealthChecks()
    .AddDbContextCheck<WikiRagDbContext>();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("Development");
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Map controllers
app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/api/health");

app.UseStatusCodePages();
app.Run();
