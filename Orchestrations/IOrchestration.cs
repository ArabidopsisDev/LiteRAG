using Arabidopsis.LiteRAG.Models;

namespace Arabidopsis.LiteRAG.Orchestrations;

public interface IOrchestration
{
    public Task<IVectorStore?> BuildAsync(string text, CancellationToken cts);
}
