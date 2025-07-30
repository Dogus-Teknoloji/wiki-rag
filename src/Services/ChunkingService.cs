using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using WikiRAG.Data;
using WikiRAG.Models;

namespace WikiRAG.Services;

public class ChunkingService : IChunkingService
{
    private readonly WikiRagDbContext _context;
    private readonly MarkdownPipeline _markdownPipeline;

    public ChunkingService(WikiRagDbContext context)
    {
        _context = context;
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public List<DocumentChunk> ChunkDocument(Document document, ChunkingStrategy strategy, ChunkingOptions? options = null)
    {
        options ??= new ChunkingOptions();

        // Safety checks to prevent runaway processing
        if (string.IsNullOrEmpty(document.Content))
        {
            return new List<DocumentChunk>();
        }

        // Prevent processing extremely large documents that could cause memory issues
        const int maxDocumentSize = 50 * 1024 * 1024; // 50MB
        if (document.Content.Length > maxDocumentSize)
        {
            throw new ArgumentException($"Document too large ({document.Content.Length} chars). Maximum supported size is {maxDocumentSize} characters.");
        }

        // Set reasonable defaults for chunk size if not specified
        if (options.MaxChunkSize <= 0)
        {
            options.MaxChunkSize = 4000; // Default 4K chars
        }

        var startTime = DateTime.UtcNow;
        var initialMemory = GC.GetTotalMemory(false);

        try
        {
            var result = strategy switch
            {
                ChunkingStrategy.HeaderBased => ChunkByHeaders(document, options),
                ChunkingStrategy.FixedSize => ChunkByFixedSize(document, options),
                ChunkingStrategy.SemanticBoundary => ChunkBySemanticBoundary(document, options),
                _ => throw new ArgumentException($"Unsupported chunking strategy: {strategy}")
            };

            // Log performance metrics for debugging
            var duration = DateTime.UtcNow - startTime;
            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = finalMemory - initialMemory;

            if (duration.TotalSeconds > 30 || memoryUsed > 100 * 1024 * 1024) // 30 seconds or 100MB
            {
                Console.WriteLine($"Performance warning: Chunking took {duration.TotalSeconds:F2}s and used {memoryUsed / 1024 / 1024:F2}MB for {result.Count} chunks");
            }

            return result;
        }
        catch (OutOfMemoryException)
        {
            GC.Collect(2, GCCollectionMode.Forced, true);
            throw new InvalidOperationException("Document chunking exceeded available memory. Try reducing chunk size or document size.");
        }
    }

    public Task<List<Chunk>> CreateChunksAsync(Document document, ChunkingStrategy strategy, ChunkingOptions? options = null)
    {
        var documentChunks = ChunkDocument(document, strategy, options);
        var chunks = new List<Chunk>();

        for (int i = 0; i < documentChunks.Count; i++)
        {
            var documentChunk = documentChunks[i];
            var chunk = new Chunk
            {
                DocumentId = document.Id,
                Content = documentChunk.Content,
                ChunkIndex = documentChunk.ChunkIndex,
                Metadata = JsonSerializer.Serialize(documentChunk.Metadata),
                Document = document
            };
            chunks.Add(chunk);
        }

        return Task.FromResult(chunks);
    }

    private List<DocumentChunk> ChunkByHeaders(Document document, ChunkingOptions options)
    {
        var chunks = new List<DocumentChunk>();
        var markdownDocument = Markdown.Parse(document.Content, _markdownPipeline);
        
        var headerStack = new Stack<(int level, string text)>();
        var currentContent = new StringBuilder();
        var chunkIndex = 0;

        foreach (var block in markdownDocument)
        {
            if (block is HeadingBlock heading)
            {
                // Save previous chunk if it has content
                if (currentContent.Length > 0)
                {
                    chunks.Add(CreateDocumentChunk(
                        currentContent.ToString().Trim(),
                        chunkIndex++,
                        document,
                        GetParentHeaders(headerStack),
                        options
                    ));
                    currentContent.Clear();
                }

                // Update header stack
                UpdateHeaderStack(headerStack, heading.Level, GetHeadingText(heading));
                
                // Add heading to content
                currentContent.AppendLine(RenderMarkdownBlock(block));
            }
            else
            {
                // Handle special blocks
                var blockContent = HandleSpecialBlocks(block, options);
                currentContent.AppendLine(blockContent);

                // Check if current chunk is getting too large
                if (currentContent.Length > options.MaxChunkSize)
                {
                    chunks.Add(CreateDocumentChunk(
                        currentContent.ToString().Trim(),
                        chunkIndex++,
                        document,
                        GetParentHeaders(headerStack),
                        options
                    ));
                    currentContent.Clear();
                }
            }
        }

        // Add final chunk if there's remaining content
        if (currentContent.Length > 0)
        {
            chunks.Add(CreateDocumentChunk(
                currentContent.ToString().Trim(),
                chunkIndex,
                document,
                GetParentHeaders(headerStack),
                options
            ));
        }

        return chunks;
    }

    private List<DocumentChunk> ChunkByFixedSize(Document document, ChunkingOptions options)
    {
        var chunks = new List<DocumentChunk>();
        var content = document.Content;
        var overlapSize = (int)(options.MaxChunkSize * options.OverlapPercentage / 100.0);
        var chunkIndex = 0;

        // Handle special blocks first
        var preservedBlocks = ExtractPreservedBlocks(content, options);
        var processedContent = content;

        for (int i = 0; i < preservedBlocks.Count; i++)
        {
            processedContent = processedContent.Replace(preservedBlocks[i], $"__PRESERVED_BLOCK_{i}__");
        }

        var position = 0;
        var effectiveAdvance = Math.Max(1, options.MaxChunkSize - overlapSize);
        var maxIterations = Math.Max(10, (processedContent.Length / effectiveAdvance) + 5); // Ensure minimum 10 iterations
        var iterations = 0;
        
        while (position < processedContent.Length && iterations < maxIterations)
        {
            iterations++;
            
            var chunkSize = Math.Min(options.MaxChunkSize, processedContent.Length - position);
            var chunkContent = processedContent.Substring(position, chunkSize);

            // Restore preserved blocks
            for (int i = 0; i < preservedBlocks.Count; i++)
            {
                chunkContent = chunkContent.Replace($"__PRESERVED_BLOCK_{i}__", preservedBlocks[i]);
            }

            // Try to break at word boundary
            var actualChunkSize = chunkSize;
            if (position + chunkSize < processedContent.Length)
            {
                var lastSpaceIndex = chunkContent.LastIndexOf(' ');
                if (lastSpaceIndex > chunkSize * 0.8) // Only adjust if we don't lose too much content
                {
                    actualChunkSize = lastSpaceIndex + 1;
                    chunkContent = chunkContent.Substring(0, actualChunkSize);
                }
            }

            chunks.Add(CreateDocumentChunk(
                chunkContent.Trim(),
                chunkIndex++,
                document,
                new List<string>(),
                options
            ));

            // Calculate advance - ensure we always make meaningful progress
            var advance = Math.Max(1, actualChunkSize - overlapSize);
            
            // Special case: if we're near the end and advance would be tiny, just take the remaining content
            if (processedContent.Length - position <= options.MaxChunkSize && advance < options.MaxChunkSize * 0.1)
            {
                break; // We've processed enough, the remaining will be handled by the final chunk
            }
            
            position += advance;
        }
        
        if (iterations >= maxIterations)
        {
            throw new InvalidOperationException($"Fixed-size chunking exceeded maximum iterations ({maxIterations}). Content length: {processedContent.Length}, chunk size: {options.MaxChunkSize}, overlap: {overlapSize}, position: {position}, last advance would be: {Math.Max(1, Math.Min(options.MaxChunkSize, processedContent.Length - position) - overlapSize)}");
        }

        return chunks;
    }

    private List<DocumentChunk> ChunkBySemanticBoundary(Document document, ChunkingOptions options)
    {
        // For now, use a combination of header-based and paragraph-based chunking
        // This can be enhanced with more sophisticated semantic analysis
        var chunks = new List<DocumentChunk>();
        var markdownDocument = Markdown.Parse(document.Content, _markdownPipeline);
        
        var headerStack = new Stack<(int level, string text)>();
        var currentContent = new StringBuilder();
        var chunkIndex = 0;

        foreach (var block in markdownDocument)
        {
            var blockContent = HandleSpecialBlocks(block, options);
            
            if (block is HeadingBlock heading)
            {
                // Similar to header-based chunking but with semantic considerations
                if (currentContent.Length > 0 && ShouldCreateSemanticChunk(currentContent.ToString(), options))
                {
                    chunks.Add(CreateDocumentChunk(
                        currentContent.ToString().Trim(),
                        chunkIndex++,
                        document,
                        GetParentHeaders(headerStack),
                        options
                    ));
                    currentContent.Clear();
                }

                UpdateHeaderStack(headerStack, heading.Level, GetHeadingText(heading));
                currentContent.AppendLine(RenderMarkdownBlock(block));
            }
            else if (block is ParagraphBlock && currentContent.Length > options.MaxChunkSize * 0.8)
            {
                // Create chunk at paragraph boundary if we're approaching size limit
                chunks.Add(CreateDocumentChunk(
                    currentContent.ToString().Trim(),
                    chunkIndex++,
                    document,
                    GetParentHeaders(headerStack),
                    options
                ));
                currentContent.Clear();
                currentContent.AppendLine(blockContent);
            }
            else
            {
                currentContent.AppendLine(blockContent);
            }
        }

        // Add final chunk
        if (currentContent.Length > 0)
        {
            chunks.Add(CreateDocumentChunk(
                currentContent.ToString().Trim(),
                chunkIndex,
                document,
                GetParentHeaders(headerStack),
                options
            ));
        }

        return chunks;
    }

    private DocumentChunk CreateDocumentChunk(string content, int chunkIndex, Document document, List<string> parentHeaders, ChunkingOptions options)
    {
        var metadata = new ChunkMetadata
        {
            SourceDocumentId = document.Id.ToString(),
            SourceDocumentTitle = document.Title,
            ChunkIndex = chunkIndex,
            ParentHeaders = parentHeaders,
            ContentCategory = DetermineContentCategory(content),
            AdditionalMetadata = new Dictionary<string, string>
            {
                ["chunk_size"] = content.Length.ToString(),
                ["created_at"] = DateTime.UtcNow.ToString("O"),
                ["chunking_strategy"] = "header_based" // This should be passed from the calling method
            }
        };

        return new DocumentChunk
        {
            Content = content,
            ChunkIndex = chunkIndex,
            Metadata = metadata
        };
    }

    private void UpdateHeaderStack(Stack<(int level, string text)> headerStack, int level, string text)
    {
        // Prevent excessive nesting
        const int maxHeaderDepth = 10;
        
        // Remove headers of equal or lower level
        while (headerStack.Count > 0 && headerStack.Peek().level >= level)
        {
            headerStack.Pop();
        }
        
        // Only add if we haven't exceeded max depth
        if (headerStack.Count < maxHeaderDepth)
        {
            headerStack.Push((level, text));
        }
    }

    private List<string> GetParentHeaders(Stack<(int level, string text)> headerStack)
    {
        return headerStack.Reverse().Select(h => h.text).ToList();
    }

    private string GetHeadingText(HeadingBlock heading)
    {
        var sb = new StringBuilder();
        if (heading.Inline != null)
        {
            foreach (var inline in heading.Inline)
            {
                if (inline is LiteralInline literal)
                {
                    sb.Append(literal.Content);
                }
            }
        }
        return sb.ToString();
    }

    private string HandleSpecialBlocks(Block block, ChunkingOptions options)
    {
        return block switch
        {
            CodeBlock code when options.PreserveCodeBlocks => RenderMarkdownBlock(block),
            Table table when options.PreserveTables => RenderMarkdownBlock(block),
            ListBlock list when options.PreserveLists => RenderMarkdownBlock(block),
            _ => RenderMarkdownBlock(block)
        };
    }

    private string RenderMarkdownBlock(Block block)
    {
        try
        {
            using var writer = new StringWriter();
            var renderer = new Markdig.Renderers.Normalize.NormalizeRenderer(writer);
            renderer.Render(block);
            var result = writer.ToString();
            
            // If NormalizeRenderer isn't working properly, fall back to block properties
            if (string.IsNullOrWhiteSpace(result) || result.Contains("Markdig.Syntax."))
            {
                return ExtractBlockContent(block);
            }
            
            return result;
        }
        catch (Exception)
        {
            // Fallback to extracting content directly from block properties
            return ExtractBlockContent(block);
        }
    }

    private string ExtractBlockContent(Block block)
    {
        return block switch
        {
            HeadingBlock heading => $"{new string('#', heading.Level)} {GetHeadingText(heading)}",
            ParagraphBlock paragraph => ExtractInlineContent(paragraph.Inline),
            CodeBlock code => code.Lines.ToString() ?? string.Empty,
            _ => block.ToString() ?? string.Empty // Last resort
        };
    }

    private string ExtractInlineContent(ContainerInline? inline)
    {
        if (inline == null) return string.Empty;
        
        var sb = new StringBuilder();
        foreach (var child in inline)
        {
            if (child is LiteralInline literal)
            {
                sb.Append(literal.Content);
            }
        }
        return sb.ToString();
    }

    private List<string> ExtractPreservedBlocks(string content, ChunkingOptions options)
    {
        var preservedBlocks = new List<string>();
        
        if (options.PreserveCodeBlocks)
        {
            // Extract code blocks
            var codeBlockRegex = new Regex(@"```[\s\S]*?```", RegexOptions.Multiline);
            preservedBlocks.AddRange(codeBlockRegex.Matches(content).Select(m => m.Value));
        }

        if (options.PreserveTables)
        {
            // Extract tables (simplified - just lines starting with |)
            var lines = content.Split('\n');
            var tableLines = new List<string>();
            var inTable = false;

            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("|"))
                {
                    if (!inTable)
                    {
                        inTable = true;
                        tableLines.Clear();
                    }
                    tableLines.Add(line);
                }
                else if (inTable)
                {
                    preservedBlocks.Add(string.Join("\n", tableLines));
                    inTable = false;
                }
            }

            if (inTable && tableLines.Count > 0)
            {
                preservedBlocks.Add(string.Join("\n", tableLines));
            }
        }

        return preservedBlocks;
    }

    private bool ShouldCreateSemanticChunk(string content, ChunkingOptions options)
    {
        // Simple heuristic for semantic boundaries
        if (content.Length < options.MaxChunkSize * 0.5)
            return false;

        // Look for natural breaks: double newlines, end of lists, etc.
        return content.EndsWith("\n\n") || 
               content.Contains(".\n\n") || 
               content.Length > options.MaxChunkSize * 0.8;
    }

    private string DetermineContentCategory(string content)
    {
        // Simple content categorization based on keywords and patterns
        var lowerContent = content.ToLowerInvariant();

        if (lowerContent.Contains("problem") || lowerContent.Contains("issue") || lowerContent.Contains("error"))
            return "problem_resolution";
        
        if (lowerContent.Contains("api") || lowerContent.Contains("interface") || lowerContent.Contains("method"))
            return "interface_usage";
        
        if (lowerContent.Contains("```") || lowerContent.Contains("code") || lowerContent.Contains("function"))
            return "technical_docs";

        return "general";
    }
}
