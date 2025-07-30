using Microsoft.EntityFrameworkCore;
using WikiRAG.Models;
using Pgvector.EntityFrameworkCore;

namespace WikiRAG.Data;

public class WikiRagDbContext : DbContext
{
    public WikiRagDbContext(DbContextOptions<WikiRagDbContext> options) : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; }
    public DbSet<Chunk> Chunks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable vector extension
        modelBuilder.HasPostgresExtension("vector");

        // Configure Document entity
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityByDefaultColumn();
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Tags).HasColumnType("text[]");
        });

        // Configure Chunk entity
        modelBuilder.Entity<Chunk>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityByDefaultColumn();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Embedding).HasColumnType("vector(1536)");
            entity.Property(e => e.Metadata).HasColumnType("jsonb");

            // Configure unique constraint
            entity.HasIndex(e => new { e.DocumentId, e.ChunkIndex })
                  .IsUnique()
                  .HasDatabaseName("unique_chunk");

            // Configure vector index for similarity search
            entity.HasIndex(e => e.Embedding)
                  .HasMethod("ivfflat")
                  .HasOperators("vector_cosine_ops")
                  .HasStorageParameter("lists", 100);

            // Configure foreign key relationship
            entity.HasOne(e => e.Document)
                  .WithMany(d => d.Chunks)
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
