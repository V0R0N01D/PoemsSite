using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace Poems.Loader.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "authors",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("authors_pk", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "poems",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    author_id = table.Column<int>(type: "integer", nullable: false),
                    searchvector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("poems_pk", x => x.id);
                    table.ForeignKey(
                        name: "poems_authors_fk",
                        column: x => x.author_id,
                        principalTable: "authors",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "authors_unique",
                table: "authors",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_poems_author_id",
                table: "poems",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "poems_search_vector_idx",
                table: "poems",
                column: "searchvector")
                .Annotation("Npgsql:IndexMethod", "gin");

            // PostgreSQL full-text functions and triggers are versioned with the schema.
            migrationBuilder.Sql("""
                CREATE FUNCTION public.generate_search_vector(author text, title text, content text)
                RETURNS tsvector AS $$
                    SELECT setweight(to_tsvector('russian', coalesce(author, '')), 'A') ||
                           setweight(to_tsvector('russian', coalesce(title, '')), 'B') ||
                           setweight(to_tsvector('russian', coalesce(content, '')), 'C');
                $$ LANGUAGE sql IMMUTABLE;

                CREATE FUNCTION public.poems_search_vector_update()
                RETURNS trigger AS $$
                DECLARE
                    author_name text;
                BEGIN
                    SELECT name INTO author_name FROM public.authors WHERE id = NEW.author_id;
                    NEW.searchvector := public.generate_search_vector(author_name, NEW.title, NEW.content);
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER poems_search_vector_update
                BEFORE INSERT OR UPDATE ON public.poems
                FOR EACH ROW EXECUTE FUNCTION public.poems_search_vector_update();

                CREATE FUNCTION public.authors_search_vector_update()
                RETURNS trigger AS $$
                BEGIN
                    UPDATE public.poems
                    SET searchvector = public.generate_search_vector(NEW.name, title, content)
                    WHERE author_id = NEW.id;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER authors_search_vector_update
                AFTER UPDATE OF name ON public.authors
                FOR EACH ROW WHEN (OLD.name IS DISTINCT FROM NEW.name)
                EXECUTE FUNCTION public.authors_search_vector_update();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER authors_search_vector_update ON public.authors;
                DROP FUNCTION public.authors_search_vector_update();
                DROP TRIGGER poems_search_vector_update ON public.poems;
                DROP FUNCTION public.poems_search_vector_update();
                DROP FUNCTION public.generate_search_vector(text, text, text);
                """);

            migrationBuilder.DropTable(
                name: "poems");

            migrationBuilder.DropTable(
                name: "authors");
        }
    }
}
