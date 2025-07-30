using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace WikiRAG.Models;

[Table("chunks")]
public class Chunk
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("document_id")]
    public int DocumentId { get; set; }

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("embedding")]
    public Vector? Embedding { get; set; }

    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    [Column("chunk_index")]
    public int ChunkIndex { get; set; }

    // Navigation property
    [ForeignKey("DocumentId")]
    public virtual Document Document { get; set; } = null!;
}
