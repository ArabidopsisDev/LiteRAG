using Arabidopsis.LiteRAG.Models;
using Arabidopsis.LiteRAG.Processes.Interfaces;

namespace Arabidopsis.LiteRAG.Orchestrations;

public class LinearOrchestration<TIndex> : IOrchestration where TIndex : notnull
{
    private IChunking? _chunking;
    private IClustering<TIndex>? _clustering;
    private IEmbedding<TIndex>? _embedding;
    private IVectoring<TIndex>? _vectoring;

    public LinearOrchestration<TIndex> AddChunking(IChunking chunking)
    {
        _chunking = chunking;
        return this;
    }

    public LinearOrchestration<TIndex> AddClustering(IClustering<TIndex> clustering)
    {
        _clustering = clustering;
        return this;
    }

    public LinearOrchestration<TIndex> AddEmbedding(IEmbedding<TIndex> embedding)
    {
        _embedding = embedding;
        return this;
    }

    public LinearOrchestration<TIndex> AddVectoring(IVectoring<TIndex> vectoring)
    {
        _vectoring = vectoring;
        return this;
    }

    public async Task<IVectorStore?> BuildAsync(CancellationToken cts)
    {
        var chunk = _chunking?.Slice();
        var cluster = await _clustering?.ClusterAsync(chunk);
        var embed = await _embedding?.EmbedAsync(cluster, cts);
        var knowledgeBase = _vectoring?.BuildKnowledgeBase(cluster, embed, cts);

        return knowledgeBase;
    }
}