namespace Poems.Common.Database;

public partial class Author
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Poem> Poems { get; set; } = new List<Poem>();
}
