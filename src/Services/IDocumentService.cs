using WikiRAG.Models;
using WikiRAG.Models.DTOs;

namespace WikiRAG.Services;

public interface IDocumentService
{
    Task<DocumentResponse> CreateDocumentAsync(DocumentRequest request);
    Task<DocumentResponse?> GetDocumentAsync(int id);
    Task<PagedResponse<DocumentSummaryResponse>> GetDocumentsAsync(int page = 1, int pageSize = 20, string? search = null, string[]? tags = null);
    Task<DocumentResponse?> UpdateDocumentAsync(int id, DocumentRequest request);
    Task<bool> DeleteDocumentAsync(int id);
    Task<BulkDocumentResponse> CreateDocumentsBulkAsync(BulkDocumentRequest request);
    Task<bool> ValidateMarkdownAsync(string content);
    Task<bool> ProcessDocumentChunksAsync(int documentId, ChunkingStrategy strategy = ChunkingStrategy.HeaderBased, ChunkingOptions? options = null);
    Task<List<Chunk>> GetDocumentChunksAsync(int documentId);
}