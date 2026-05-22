using Arabidopsis.LiteRAG.Models;

namespace Arabidopsis.LiteRAG.Processes.Interfaces;

public interface IVectoring<TIndex> where TIndex : notnull
{
    public IVectorStore BuildKnowledgeBase(
        Dictionary<TIndex, string> semanticChunks,
        Dictionary<TIndex, float[]> vectors,
        CancellationToken cts);
}
