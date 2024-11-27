using NpgsqlTypes;

namespace Poems.Common.Database;

public partial class Poem
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public int AuthorId { get; set; }

    public NpgsqlTsVector? Searchvector { get; set; }

    public virtual Author Author { get; set; } = null!;
}
