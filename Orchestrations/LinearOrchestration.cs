using Arabidopsis.LiteRAG.Models;
using Arabidopsis.LiteRAG.Processes.Interfaces;

namespace Arabidopsis.LiteRAG.Orchestrations
{
    public class LinearOrchestration<TIndex> : IOrchestration where TIndex : notnull
    {
        private IChunking? _chunking;
        private IClustering<TIndex>? _clustering;
        private IEmbedding<TIndex>? _embedding;
        private IVectoring<TIndex>? _vectoring;

        public void SetChunking(IChunking chunking) => _chunking = chunking;
        public void SetClustering(IClustering<TIndex> clustering) => _clustering = clustering;
        public void SetEmbedding(IEmbedding<TIndex> embedding) => _embedding = embedding;
        public void SetVectoring(IVectoring<TIndex> vectoring) => _vectoring = vectoring;

        public async Task<IVectorStore?> BuildAsync(string text, CancellationToken cts)
        {
            var chunk = _chunking?.Slice();
            var cluster = await _clustering?.ClusterAsync(chunk);
            var embed = await _embedding?.EmbedAsync(cluster, cts);
            var knowledgeBase = _vectoring?.BuildKnowledgeBase(cluster, embed, cts);

            return knowledgeBase;
        }
    }
}
