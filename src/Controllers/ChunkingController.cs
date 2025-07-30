using Microsoft.AspNetCore.Mvc;
using WikiRAG.Models;
using WikiRAG.Models.DTOs;
using WikiRAG.Services;

namespace WikiRAG.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChunkingController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IChunkingService _chunkingService;

    public ChunkingController(IDocumentService documentService, IChunkingService chunkingService)
    {
        _documentService = documentService;
        _chunkingService = chunkingService;
    }

    /// <summary>
    /// Process a document into chunks using the specified strategy
    /// </summary>
    /// <param name="documentId">The ID of the document to process</param>
    /// <param name="request">Chunking configuration</param>
    /// <returns>Success status and chunk count</returns>
    [HttpPost("{documentId}/process")]
    public async Task<ActionResult<ChunkingResponse>> ProcessDocumentChunks(
        int documentId, 
        [FromBody] ChunkingRequest request)
    {
        try
        {
            var options = new ChunkingOptions
            {
                MaxChunkSize = request.MaxChunkSize ?? 1000,
                OverlapPercentage = request.OverlapPercentage ?? 10,
                PreserveCodeBlocks = request.PreserveCodeBlocks ?? true,
                PreserveTables = request.PreserveTables ?? true,
                PreserveLists = request.PreserveLists ?? true
            };

            var success = await _documentService.ProcessDocumentChunksAsync(
                documentId, 
                request.Strategy, 
                options);

            if (!success)
            {
                return NotFound($"Document with ID {documentId} not found.");
            }

            var chunks = await _documentService.GetDocumentChunksAsync(documentId);

            return Ok(new ChunkingResponse
            {
                Success = true,
                ChunkCount = chunks.Count,
                Strategy = request.Strategy.ToString(),
                Message = $"Document successfully processed into {chunks.Count} chunks using {request.Strategy} strategy."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ChunkingResponse
            {
                Success = false,
                ChunkCount = 0,
                Strategy = request.Strategy.ToString(),
                Message = $"Error processing document: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get all chunks for a document
    /// </summary>
    /// <param name="documentId">The ID of the document</param>
    /// <returns>List of chunks with metadata</returns>
    [HttpGet("{documentId}/chunks")]
    public async Task<ActionResult<ChunkDetailsResponse>> GetDocumentChunks(int documentId)
    {
        try
        {
            var document = await _documentService.GetDocumentAsync(documentId);
            if (document == null)
            {
                return NotFound($"Document with ID {documentId} not found.");
            }

            var chunks = await _documentService.GetDocumentChunksAsync(documentId);

            var chunkDetails = chunks.Select(c => new ChunkDetail
            {
                Id = c.Id,
                ChunkIndex = c.ChunkIndex,
                Content = c.Content,
                ContentLength = c.Content.Length,
                Metadata = c.Metadata
            }).ToList();

            return Ok(new ChunkDetailsResponse
            {
                DocumentId = documentId,
                DocumentTitle = document.Title,
                TotalChunks = chunks.Count,
                Chunks = chunkDetails
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Preview how a document would be chunked without saving to database
    /// </summary>
    /// <param name="documentId">The ID of the document</param>
    /// <param name="request">Chunking configuration</param>
    /// <returns>Preview of chunks</returns>
    [HttpPost("{documentId}/preview")]
    public async Task<ActionResult<ChunkPreviewResponse>> PreviewDocumentChunks(
        int documentId, 
        [FromBody] ChunkingRequest request)
    {
        try
        {
            var document = await _documentService.GetDocumentAsync(documentId);
            if (document == null)
            {
                return NotFound($"Document with ID {documentId} not found.");
            }

            var documentEntity = new Document
            {
                Id = document.Id,
                Title = document.Title,
                Content = document.Content,
                Author = document.Author,
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt,
                Tags = document.Tags
            };

            var options = new ChunkingOptions
            {
                MaxChunkSize = request.MaxChunkSize ?? 1000,
                OverlapPercentage = request.OverlapPercentage ?? 10,
                PreserveCodeBlocks = request.PreserveCodeBlocks ?? true,
                PreserveTables = request.PreserveTables ?? true,
                PreserveLists = request.PreserveLists ?? true
            };

            var chunks = _chunkingService.ChunkDocument(documentEntity, request.Strategy, options);

            var chunkPreviews = chunks.Select(c => new ChunkPreview
            {
                ChunkIndex = c.ChunkIndex,
                Content = c.Content.Length > 200 ? c.Content.Substring(0, 200) + "..." : c.Content,
                ContentLength = c.Content.Length,
                ParentHeaders = c.Metadata.ParentHeaders,
                ContentCategory = c.Metadata.ContentCategory
            }).ToList();

            return Ok(new ChunkPreviewResponse
            {
                DocumentId = documentId,
                DocumentTitle = document.Title,
                Strategy = request.Strategy.ToString(),
                TotalChunks = chunks.Count,
                ChunkPreviews = chunkPreviews
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
