using Microsoft.EntityFrameworkCore;
using Markdig;
using WikiRAG.Data;
using WikiRAG.Models;
using WikiRAG.Models.DTOs;

namespace WikiRAG.Services;

public class DocumentService : IDocumentService
{
    private readonly WikiRagDbContext _context;
    private readonly MarkdownPipeline _markdownPipeline;
    private readonly IChunkingService _chunkingService;

    public DocumentService(WikiRagDbContext context, IChunkingService chunkingService)
    {
        _context = context;
        _chunkingService = chunkingService;
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public async Task<DocumentResponse> CreateDocumentAsync(DocumentRequest request)
    {
        // Validate markdown content
        if (!await ValidateMarkdownAsync(request.Content))
        {
            throw new ArgumentException("Invalid Markdown content provided.");
        }

        var document = new Document
        {
            Title = request.Title.Trim(),
            Content = request.Content,
            Author = request.Author?.Trim(),
            Tags = request.Tags?.ToArray(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return MapToResponse(document);
    }

    public async Task<DocumentResponse?> GetDocumentAsync(int id)
    {
        var document = await _context.Documents
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == id);

        return document == null ? null : MapToResponse(document);
    }

    public async Task<PagedResponse<DocumentSummaryResponse>> GetDocumentsAsync(
        int page = 1, 
        int pageSize = 20, 
        string? search = null, 
        string[]? tags = null)
    {
        var query = _context.Documents.Include(d => d.Chunks).AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d => d.Title.Contains(search) || 
                                   d.Content.Contains(search) ||
                                   (d.Author != null && d.Author.Contains(search)));
        }

        // Apply tags filter
        if (tags != null && tags.Length > 0)
        {
            query = query.Where(d => d.Tags != null && d.Tags.Any(t => tags.Contains(t)));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var documents = await query
            .OrderByDescending(d => d.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DocumentSummaryResponse
            {
                Id = d.Id,
                Title = d.Title,
                Author = d.Author,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                Tags = d.Tags,
                ChunkCount = d.Chunks.Count,
                ContentLength = d.Content.Length
            })
            .ToListAsync();

        return new PagedResponse<DocumentSummaryResponse>
        {
            Items = documents,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }

    public async Task<DocumentResponse?> UpdateDocumentAsync(int id, DocumentRequest request)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null)
            return null;

        // Validate markdown content
        if (!await ValidateMarkdownAsync(request.Content))
        {
            throw new ArgumentException("Invalid Markdown content provided.");
        }

        document.Title = request.Title.Trim();
        document.Content = request.Content;
        document.Author = request.Author?.Trim();
        document.Tags = request.Tags?.ToArray();
        document.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToResponse(document);
    }

    public async Task<bool> DeleteDocumentAsync(int id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null)
            return false;

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<BulkDocumentResponse> CreateDocumentsBulkAsync(BulkDocumentRequest request)
    {
        var response = new BulkDocumentResponse();
        var documentsToAdd = new List<Document>();

        for (int i = 0; i < request.Documents.Count; i++)
        {
            var docRequest = request.Documents[i];
            try
            {
                // Validate markdown content
                if (!await ValidateMarkdownAsync(docRequest.Content))
                {
                    response.Errors.Add(new BulkErrorResponse
                    {
                        Index = i,
                        Title = docRequest.Title,
                        Error = "Invalid Markdown content"
                    });
                    continue;
                }

                var document = new Document
                {
                    Title = docRequest.Title.Trim(),
                    Content = docRequest.Content,
                    Author = docRequest.Author?.Trim(),
                    Tags = docRequest.Tags?.ToArray(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                documentsToAdd.Add(document);
            }
            catch (Exception ex)
            {
                response.Errors.Add(new BulkErrorResponse
                {
                    Index = i,
                    Title = docRequest.Title,
                    Error = ex.Message
                });
            }
        }

        if (documentsToAdd.Any())
        {
            _context.Documents.AddRange(documentsToAdd);
            await _context.SaveChangesAsync();

            response.SuccessfulDocuments = documentsToAdd.Select(MapToResponse).ToList();
        }

        response.SuccessCount = documentsToAdd.Count;
        response.ErrorCount = response.Errors.Count;

        return response;
    }

    public async Task<bool> ValidateMarkdownAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        try
        {
            // Attempt to parse the markdown
            var document = Markdown.Parse(content, _markdownPipeline);
            
            // Basic validation - if parsing doesn't throw, content is valid markdown
            // You can add more sophisticated validation here if needed
            
            return await Task.FromResult(true);
        }
        catch
        {
            return false;
        }
    }

    private static DocumentResponse MapToResponse(Document document)
    {
        return new DocumentResponse
        {
            Id = document.Id,
            Title = document.Title,
            Content = document.Content,
            Author = document.Author,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            Tags = document.Tags,
            ChunkCount = document.Chunks?.Count ?? 0
        };
    }

    public async Task<bool> ProcessDocumentChunksAsync(int documentId, ChunkingStrategy strategy = ChunkingStrategy.HeaderBased, ChunkingOptions? options = null)
    {
        var document = await _context.Documents
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
            return false;

        // Remove existing chunks
        if (document.Chunks.Any())
        {
            _context.Chunks.RemoveRange(document.Chunks);
        }

        // Generate new chunks
        var chunks = await _chunkingService.CreateChunksAsync(document, strategy, options);
        
        // Add chunks to database
        _context.Chunks.AddRange(chunks);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<Chunk>> GetDocumentChunksAsync(int documentId)
    {
        return await _context.Chunks
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync();
    }
}
