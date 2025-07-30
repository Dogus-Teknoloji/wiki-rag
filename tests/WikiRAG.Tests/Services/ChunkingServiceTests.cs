using Microsoft.EntityFrameworkCore;
using WikiRAG.Data;
using WikiRAG.Models;
using WikiRAG.Services;
using Xunit;

namespace WikiRAG.Tests.Services;

public class ChunkingServiceTests : IDisposable
{
    private readonly WikiRagDbContext _context;
    private readonly ChunkingService _chunkingService;

    public ChunkingServiceTests()
    {
        var options = new DbContextOptionsBuilder<WikiRagDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WikiRagDbContext(options);
        _chunkingService = new ChunkingService(_context);
    }

    [Fact]
    public void ChunkDocument_HeaderBased_ShouldPreserveHierarchy()
    {
        // Arrange
        var document = new Document
        {
            Id = 1,
            Title = "Test Document",
            Content = @"# Main Heading

This is content under main heading.

## Sub Heading 1

Content under sub heading 1.

### Deep Heading

Content under deep heading.

## Sub Heading 2

Content under sub heading 2."
        };

        var options = new ChunkingOptions
        {
            MaxChunkSize = 200,
            PreserveCodeBlocks = true
        };

        // Act
        var chunks = _chunkingService.ChunkDocument(document, ChunkingStrategy.HeaderBased, options);

        // Assert
        Assert.NotEmpty(chunks);
        
        // First chunk should have main heading
        var firstChunk = chunks.First();
        Assert.Contains("# Main Heading", firstChunk.Content);
        Assert.Equal("Test Document", firstChunk.Metadata.SourceDocumentTitle);
        
        // Check that parent headers are preserved
        var deepChunk = chunks.FirstOrDefault(c => c.Content.Contains("### Deep Heading"));
        Assert.NotNull(deepChunk);
        Assert.Contains("Main Heading", deepChunk.Metadata.ParentHeaders);
        Assert.Contains("Sub Heading 1", deepChunk.Metadata.ParentHeaders);
    }

    [Fact]
    public void ChunkDocument_FixedSize_ShouldRespectSizeLimit()
    {
        // Arrange
        var longContent = string.Join(" ", Enumerable.Repeat("This is a test sentence.", 100));
        var document = new Document
        {
            Id = 1,
            Title = "Long Document",
            Content = longContent
        };

        var options = new ChunkingOptions
        {
            MaxChunkSize = 500,
            OverlapPercentage = 10
        };

        // Act
        var chunks = _chunkingService.ChunkDocument(document, ChunkingStrategy.FixedSize, options);

        // Assert
        Assert.True(chunks.Count > 1);
        
        foreach (var chunk in chunks.Take(chunks.Count - 1)) // All except last chunk
        {
            Assert.True(chunk.Content.Length <= options.MaxChunkSize * 1.1); // Allow some flexibility
        }
    }

    [Fact]
    public void ChunkDocument_WithCodeBlocks_ShouldPreserveCodeIntegrity()
    {
        // Arrange
        var document = new Document
        {
            Id = 1,
            Title = "Technical Document",
            Content = @"# Code Example

Here's some code:

```csharp
public class Example 
{
    public void Method() 
    {
        Console.WriteLine(""Hello World"");
    }
}
```

And some more text after the code block."
        };

        var options = new ChunkingOptions
        {
            MaxChunkSize = 200,
            PreserveCodeBlocks = true
        };

        // Act
        var chunks = _chunkingService.ChunkDocument(document, ChunkingStrategy.HeaderBased, options);

        // Assert
        var codeChunk = chunks.FirstOrDefault(c => c.Content.Contains("```csharp"));
        Assert.NotNull(codeChunk);
        Assert.Contains("public class Example", codeChunk.Content);
        Assert.Contains("Console.WriteLine", codeChunk.Content);
    }

    [Fact]
    public void ChunkDocument_SemanticBoundary_ShouldCreateLogicalChunks()
    {
        // Arrange
        var document = new Document
        {
            Id = 1,
            Title = "Article",
            Content = @"# Introduction

This is the introduction paragraph with some content.

This is another paragraph in the introduction.

# Main Content

This section contains the main content of the article.

It has multiple paragraphs that should be chunked logically.

# Conclusion

This is the conclusion section."
        };

        var options = new ChunkingOptions
        {
            MaxChunkSize = 300
        };

        // Act
        var chunks = _chunkingService.ChunkDocument(document, ChunkingStrategy.SemanticBoundary, options);

        // Assert
        Assert.NotEmpty(chunks);
        Assert.True(chunks.Count >= 2);
        
        // Each chunk should have meaningful content
        foreach (var chunk in chunks)
        {
            Assert.False(string.IsNullOrWhiteSpace(chunk.Content));
            Assert.True(chunk.Content.Length > 10); // Should have substantial content
        }
    }

    [Fact]
    public void ChunkDocument_ShouldCategorizeContent()
    {
        // Arrange
        var document = new Document
        {
            Id = 1,
            Title = "Mixed Content",
            Content = @"# API Documentation

This describes the user API interface and methods.

# Problem Resolution

There was an error in the system that needed fixing.

# General Information

This is just general information about the system."
        };

        var options = new ChunkingOptions();

        // Act
        var chunks = _chunkingService.ChunkDocument(document, ChunkingStrategy.HeaderBased, options);

        // Assert
        var apiChunk = chunks.FirstOrDefault(c => c.Content.Contains("API interface"));
        var problemChunk = chunks.FirstOrDefault(c => c.Content.Contains("error in the system"));
        var generalChunk = chunks.FirstOrDefault(c => c.Content.Contains("general information"));

        Assert.NotNull(apiChunk);
        Assert.NotNull(problemChunk);
        Assert.NotNull(generalChunk);

        Assert.Equal("interface_usage", apiChunk.Metadata.ContentCategory);
        Assert.Equal("problem_resolution", problemChunk.Metadata.ContentCategory);
        Assert.Equal("general", generalChunk.Metadata.ContentCategory);
    }

    [Fact]
    public async Task CreateChunksAsync_ShouldCreateDatabaseEntities()
    {
        // Arrange
        var document = new Document
        {
            Id = 1,
            Title = "Test Document",
            Content = "# Test\n\nThis is test content."
        };

        // Act
        var chunks = await _chunkingService.CreateChunksAsync(document, ChunkingStrategy.HeaderBased);

        // Assert
        Assert.NotEmpty(chunks);
        
        foreach (var chunk in chunks)
        {
            Assert.Equal(document.Id, chunk.DocumentId);
            Assert.False(string.IsNullOrEmpty(chunk.Content));
            Assert.False(string.IsNullOrEmpty(chunk.Metadata));
            Assert.True(chunk.ChunkIndex >= 0);
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
