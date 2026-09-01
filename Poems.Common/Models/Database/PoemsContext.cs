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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
