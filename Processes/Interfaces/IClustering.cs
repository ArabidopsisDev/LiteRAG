namespace Arabidopsis.LiteRAG.Processes.Interfaces;

public interface IClustering<TIndex> where TIndex : notnull
{
    public Task<Dictionary<TIndex, string>>
        ClusterAsync(List<string> chunks, CancellationToken cts = default);
}
