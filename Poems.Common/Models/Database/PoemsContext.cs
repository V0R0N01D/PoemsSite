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
            entity.HasKey(e => e.Id).HasName("authors_pk");

            entity.ToTable("authors");

            entity.HasIndex(e => e.Name, "authors_unique").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<Poem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("poems_pk");

            entity.ToTable("poems");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AuthorId).HasColumnName("author_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.Searchvector).HasColumnName("searchvector");
            entity.Property(e => e.Title).HasColumnName("title");

            entity.HasOne(d => d.Author).WithMany(p => p.Poems)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("poems_authors_fk");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
