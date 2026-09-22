using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Poems.Loader.Migrations
{
    /// <inheritdoc />
    public partial class AddVectorSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "poem_chunks",
                columns: table => new
                {
                    poem_id = table.Column<int>(type: "integer", nullable: false),
                    chunk_number = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    embedding = table.Column<Vector>(type: "vector(384)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("poem_chunks_pk", x => new { x.poem_id, x.chunk_number });
                    table.ForeignKey(
                        name: "poem_chunks_poems_fk",
                        column: x => x.poem_id,
                        principalTable: "poems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "poem_chunks_embedding_idx",
                table: "poem_chunks",
                column: "embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "poem_chunks");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
