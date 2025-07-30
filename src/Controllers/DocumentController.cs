using Microsoft.AspNetCore.Mvc;
using WikiRAG.Models.DTOs;
using WikiRAG.Services;
using System.ComponentModel.DataAnnotations;

namespace WikiRAG.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(IDocumentService documentService, ILogger<DocumentController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Ingest a new document into the knowledge base
    /// </summary>
    /// <param name="request">Document details</param>
    /// <returns>Created document details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentResponse>> CreateDocument([FromBody] DocumentRequest request)
    {
        try
        {
            _logger.LogInformation("Creating new document with title: {Title}", request.Title);

            var response = await _documentService.CreateDocumentAsync(request);

            _logger.LogInformation("Successfully created document with ID: {DocumentId}", response.Id);

            return CreatedAtAction(
                nameof(GetDocument),
                new { id = response.Id },
                response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid document creation request: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document with title: {Title}", request.Title);
            return StatusCode(500, new { error = "An error occurred while creating the document" });
        }
    }

    /// <summary>
    /// Update an existing document
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <param name="request">Updated document details</param>
    /// <returns>Updated document details</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentResponse>> UpdateDocument(int id, [FromBody] DocumentRequest request)
    {
        try
        {
            _logger.LogInformation("Updating document with ID: {DocumentId}", id);

            var response = await _documentService.UpdateDocumentAsync(id, request);
            if (response == null)
            {
                _logger.LogWarning("Document with ID {DocumentId} not found for update", id);
                return NotFound(new { error = "Document not found" });
            }

            _logger.LogInformation("Successfully updated document with ID: {DocumentId}", id);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid document update request for ID {DocumentId}: {Error}", id, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document with ID: {DocumentId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the document" });
        }
    }

    /// <summary>
    /// Delete a document from the knowledge base
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        try
        {
            _logger.LogInformation("Deleting document with ID: {DocumentId}", id);

            var success = await _documentService.DeleteDocumentAsync(id);
            if (!success)
            {
                _logger.LogWarning("Document with ID {DocumentId} not found for deletion", id);
                return NotFound(new { error = "Document not found" });
            }

            _logger.LogInformation("Successfully deleted document with ID: {DocumentId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document with ID: {DocumentId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the document" });
        }
    }

    /// <summary>
    /// List all documents with pagination and optional filtering
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="search">Search term for title, content, or author</param>
    /// <param name="tags">Filter by tags (comma-separated)</param>
    /// <returns>Paginated list of documents</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<DocumentSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResponse<DocumentSummaryResponse>>> GetDocuments(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? tags = null)
    {
        try
        {
            _logger.LogInformation("Retrieving documents - Page: {Page}, PageSize: {PageSize}, Search: {Search}, Tags: {Tags}",
                page, pageSize, search, tags);

            var tagArray = string.IsNullOrWhiteSpace(tags) 
                ? null 
                : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var response = await _documentService.GetDocumentsAsync(page, pageSize, search, tagArray);

            _logger.LogInformation("Retrieved {Count} documents out of {Total} total",
                response.Items.Count, response.TotalItems);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents");
            return StatusCode(500, new { error = "An error occurred while retrieving documents" });
        }
    }

    /// <summary>
    /// Retrieve a specific document by ID
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <returns>Document details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentResponse>> GetDocument(int id)
    {
        try
        {
            _logger.LogInformation("Retrieving document with ID: {DocumentId}", id);

            var response = await _documentService.GetDocumentAsync(id);
            if (response == null)
            {
                _logger.LogWarning("Document with ID {DocumentId} not found", id);
                return NotFound(new { error = "Document not found" });
            }

            _logger.LogInformation("Successfully retrieved document with ID: {DocumentId}", id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document with ID: {DocumentId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the document" });
        }
    }

    /// <summary>
    /// Bulk upload multiple documents
    /// </summary>
    /// <param name="request">Bulk document upload request</param>
    /// <returns>Bulk upload results</returns>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(BulkDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BulkDocumentResponse>> CreateDocumentsBulk([FromBody] BulkDocumentRequest request)
    {
        try
        {
            if (request.Documents == null || !request.Documents.Any())
            {
                return BadRequest(new { error = "No documents provided for bulk upload" });
            }

            if (request.Documents.Count > 100)
            {
                return BadRequest(new { error = "Bulk upload limited to 100 documents per request" });
            }

            _logger.LogInformation("Processing bulk document upload with {Count} documents", request.Documents.Count);

            var response = await _documentService.CreateDocumentsBulkAsync(request);

            _logger.LogInformation("Bulk upload completed - Success: {SuccessCount}, Errors: {ErrorCount}",
                response.SuccessCount, response.ErrorCount);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk document upload");
            return StatusCode(500, new { error = "An error occurred while processing the bulk upload" });
        }
    }
}
