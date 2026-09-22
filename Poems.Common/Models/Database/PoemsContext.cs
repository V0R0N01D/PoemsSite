using Microsoft.EntityFrameworkCore;

namespace Poems.Common.Models.Database;

public partial class PoemsContext : DbContext
{
    public PoemsContext(DbContextOptions<PoemsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Author> Authors { get; set; }

    public virtual DbSet<Poem> Poems { get; set; }

    public DbSet<PoemChunk> PoemChunks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(author => author.Id).HasName("authors_pk");

            entity.ToTable("authors");

            entity.HasIndex(author => author.Name, "authors_unique").IsUnique();

            entity.Property(author => author.Id).HasColumnName("id");
            entity.Property(author => author.Name).HasColumnName("name");
        });

        modelBuilder.Entity<Poem>(poemEntity =>
        {
            poemEntity.HasKey(poem => poem.Id).HasName("poems_pk");

            poemEntity.ToTable("poems");

            poemEntity.HasIndex(e => e.SearchVector, "poems_search_vector_idx").HasMethod("gin");

            poemEntity.Property(poem => poem.Id).HasColumnName("id");
            poemEntity.Property(poem => poem.AuthorId).HasColumnName("author_id");
            poemEntity.Property(poem => poem.Content).HasColumnName("content");
            poemEntity.Property(poem => poem.SearchVector).HasColumnName("searchvector");
            poemEntity.Property(poem => poem.Title).HasColumnName("title");

            poemEntity
                .HasOne(poem => poem.Author)
                .WithMany(author => author.Poems)
                .HasForeignKey(poem => poem.AuthorId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("poems_authors_fk");
        });

        modelBuilder.Entity<PoemChunk>(entity =>
        {
            entity.ToTable("poem_chunks");
            entity.HasKey(poemChank => new { poemChank.PoemId, poemChank.ChunkNumber }).HasName("poem_chunks_pk");
            entity.Property(poemChank => poemChank.PoemId).HasColumnName("poem_id");
            entity.Property(poemChank => poemChank.ChunkNumber).HasColumnName("chunk_number");
            entity.Property(poemChank => poemChank.Text).HasColumnName("text");
            entity.Property(poemChank => poemChank.Embedding).HasColumnName("embedding").HasColumnType("vector(384)");
            entity
                .HasIndex(poemChank => poemChank.Embedding, "poem_chunks_embedding_idx")
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops");
            entity
                .HasOne(poemChank => poemChank.Poem)
                .WithMany()
                .HasForeignKey(e => e.PoemId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("poem_chunks_poems_fk");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
