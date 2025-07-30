using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WikiRAG.Data;
using WikiRAG.Models;

namespace WikiRAG.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseTestController : ControllerBase
{
    private readonly WikiRagDbContext _context;
    private readonly ILogger<DatabaseTestController> _logger;

    public DatabaseTestController(WikiRagDbContext context, ILogger<DatabaseTestController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("connection")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            return Ok(new { connected = canConnect, message = "Database connection test completed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed");
            return StatusCode(500, new { connected = false, error = ex.Message });
        }
    }

    [HttpGet("tables")]
    public async Task<IActionResult> TestTables()
    {
        try
        {
            var documentCount = await _context.Documents.CountAsync();
            var chunkCount = await _context.Chunks.CountAsync();
            
            return Ok(new 
            { 
                tables = new 
                {
                    documents = new { exists = true, count = documentCount },
                    chunks = new { exists = true, count = chunkCount }
                },
                message = "Tables are accessible"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Table test failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("test-document")]
    public async Task<IActionResult> CreateTestDocument()
    {
        try
        {
            var testDoc = new Document
            {
                Title = "Test Document",
                Content = "This is a test document to verify database functionality.",
                Author = "System Test",
                Tags = new[] { "test", "verification" }
            };

            _context.Documents.Add(testDoc);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Test document created successfully", documentId = testDoc.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test document");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
