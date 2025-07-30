using WikiRAG.Services;

namespace WikiRAG.Models.DTOs;

public class ChunkingRequest
{
    public ChunkingStrategy Strategy { get; set; } = ChunkingStrategy.HeaderBased;
    public int? MaxChunkSize { get; set; }
    public int? OverlapPercentage { get; set; }
    public bool? PreserveCodeBlocks { get; set; }
    public bool? PreserveTables { get; set; }
    public bool? PreserveLists { get; set; }
}

public class ChunkingResponse
{
    public bool Success { get; set; }
    public int ChunkCount { get; set; }
    public string Strategy { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ChunkDetailsResponse
{
    public int DocumentId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public int TotalChunks { get; set; }
    public List<ChunkDetail> Chunks { get; set; } = new();
}

public class ChunkDetail
{
    public int Id { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public int ContentLength { get; set; }
    public string? Metadata { get; set; }
}

public class ChunkPreviewResponse
{
    public int DocumentId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public string Strategy { get; set; } = string.Empty;
    public int TotalChunks { get; set; }
    public List<ChunkPreview> ChunkPreviews { get; set; } = new();
}

public class ChunkPreview
{
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public int ContentLength { get; set; }
    public List<string> ParentHeaders { get; set; } = new();
    public string ContentCategory { get; set; } = string.Empty;
}
