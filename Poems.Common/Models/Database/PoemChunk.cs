using Pgvector;

namespace Poems.Common.Models.Database;

public class PoemChunk
{
    public int PoemId { get; set; }
    public int ChunkNumber { get; set; }
    public string Text { get; set; } = null!;
    public Vector Embedding { get; set; } = null!;
    public Poem Poem { get; set; } = null!;
}
