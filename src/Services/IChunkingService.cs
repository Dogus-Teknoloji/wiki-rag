using WikiRAG.Models;

namespace WikiRAG.Services;

public enum ChunkingStrategy
{
    HeaderBased,
    FixedSize,
    SemanticBoundary
}

public class ChunkingOptions
{
    public int MaxChunkSize { get; set; } = 1000;
    public int OverlapPercentage { get; set; } = 10;
    public bool PreserveCodeBlocks { get; set; } = true;
    public bool PreserveTables { get; set; } = true;
    public bool PreserveLists { get; set; } = true;
}

public class DocumentChunk
{
    public string Content { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public ChunkMetadata Metadata { get; set; } = new();
}

public class ChunkMetadata
{
    public string SourceDocumentId { get; set; } = string.Empty;
    public string SourceDocumentTitle { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public List<string> ParentHeaders { get; set; } = new();
    public Dictionary<string, string> AdditionalMetadata { get; set; } = new();
    public string ContentCategory { get; set; } = "general";
}

public interface IChunkingService
{
    List<DocumentChunk> ChunkDocument(Document document, ChunkingStrategy strategy, ChunkingOptions? options = null);
    Task<List<Chunk>> CreateChunksAsync(Document document, ChunkingStrategy strategy, ChunkingOptions? options = null);
}
