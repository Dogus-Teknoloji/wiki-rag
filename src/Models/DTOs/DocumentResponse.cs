namespace WikiRAG.Models.DTOs;

public class DocumentResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Author { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string[]? Tags { get; set; }
    public int ChunkCount { get; set; }
}

public class DocumentSummaryResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string[]? Tags { get; set; }
    public int ChunkCount { get; set; }
    public int ContentLength { get; set; }
}

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class BulkDocumentRequest
{
    public List<DocumentRequest> Documents { get; set; } = new();
}

public class BulkDocumentResponse
{
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<DocumentResponse> SuccessfulDocuments { get; set; } = new();
    public List<BulkErrorResponse> Errors { get; set; } = new();
}

public class BulkErrorResponse
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
