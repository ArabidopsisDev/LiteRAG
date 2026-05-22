namespace Arabidopsis.LiteRAG.Models;

public class VectorEntry
{
    public required string Id { get; set; }

    /// <summary>
    /// Semantic metadata associated with this text chunk.
    /// Contains tags and summary information.
    /// </summary>
    public required Semantics Semantics { get; set; }

    public required string Text { get; set; }
    public required float[] Vector { get; set; }
    public required string SourceFile { get; set; }
    public DateTime CreatedAt { get; set; }
}