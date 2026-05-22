namespace Arabidopsis.LiteRAG.Processes.Interfaces;

public interface IEmbedding<TIndex> where TIndex : notnull
{
    public Task<Dictionary<TIndex, float[]>> EmbedAsync(
        Dictionary<TIndex, string> semanticChunks,
        CancellationToken cts);
}

