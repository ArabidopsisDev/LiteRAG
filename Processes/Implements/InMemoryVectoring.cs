using Arabidopsis.LiteRAG.Models;
using Arabidopsis.LiteRAG.Processes.Interfaces;
using OpenAI;
using OpenAI.Embeddings;
using System.ClientModel;

namespace Arabidopsis.LiteRAG.Processes.Implements;

public class InMemoryVectoring : IVectoring<Semantics>
{

    private readonly IVectorStore _vectorStore;
    private readonly EmbeddingClient _client;
    private readonly string _srcFile;

    public InMemoryVectoring(string apiKey, string srcFile,IVectorStore? vectorStore = null)
    {
        _srcFile = srcFile;

        if (vectorStore is null)
            _vectorStore = new MemoryVectorStore();
        else
            _vectorStore = vectorStore;

        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1")
        };

        var openAiClient = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        _client = openAiClient.GetEmbeddingClient("text-embedding-v4");
    }

    /// <summary>
    /// Builds the knowledge base using semantic-tagged chunks and their embeddings.
    /// </summary>
    /// <param name="semanticChunks">Dictionary mapping Semantics metadata to text content</param>
    /// <param name="vectors">Dictionary mapping Semantics to their corresponding embeddings</param>
    /// <param name="sourceFile">The source file name</param>
    /// <param name="cts">Cancellation token</param>
    public IVectorStore BuildKnowledgeBase(
        Dictionary<Semantics, string> semanticChunks,
        Dictionary<Semantics, float[]> vectors,
        CancellationToken cts)
    {
        var entries = semanticChunks
            .Select(kvp => new VectorEntry
            {
                Id = Guid.NewGuid().ToString(),
                Semantics = kvp.Key,
                Text = kvp.Value,
                Vector = vectors[kvp.Key],
                SourceFile = _srcFile,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        _vectorStore.AddRange(entries);

        Console.WriteLine($"知识库已构建：{entries.Count} 个向量，来自 {_srcFile}");
        return _vectorStore;
    }

    /// <summary>
    /// Searches the knowledge base with semantic-aware ranking.
    /// </summary>
    public async Task<List<(string Text, float Score)>> SearchAsync(string query, int topK = 5)
    {
        var response = await _client.GenerateEmbeddingAsync(query);
        var queryVector = response.Value.ToFloats().ToArray();

        var vectorResults = _vectorStore.Search(queryVector, topK * 2);

        var scoredResults = vectorResults
            .Select(result =>
            {
                var vectorScore = result.Score;
                var semanticScore = CalculateSemanticRelevance(query, result.Entry.Semantics);

                // 70% vector similarity + 30% semantic relevance
                var compositeScore = (vectorScore * 0.7f) + (semanticScore * 0.3f);

                return (result.Entry.Text, Score: compositeScore);
            })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();

        return scoredResults;
    }

    /// <summary>
    /// Calculates semantic relevance score based on tag and summary matching.
    /// </summary>
    private float CalculateSemanticRelevance(string query, Semantics semantics)
    {
        float relevanceScore = 0f;
        var queryTokens = query.Split(new[] { ' ', '，', '。', '、', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // Tag matching (60% weight)
        if (semantics.Tags.Count > 0)
        {
            var matchedTags = semantics.Tags.Count(tag =>
                queryTokens.Any(token => tag.Contains(token, StringComparison.OrdinalIgnoreCase)) ||
                query.Contains(tag, StringComparison.OrdinalIgnoreCase));

            float tagScore = (float)matchedTags / semantics.Tags.Count;
            relevanceScore += Math.Min(tagScore, 1f) * 0.6f;
        }

        // Summary matching (40% weight)
        if (!string.IsNullOrEmpty(semantics.Summary))
        {
            var summaryTokens = semantics.Summary.Split(new[] { ' ', '，', '。', '、', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var matchedWords = queryTokens.Count(token =>
                summaryTokens.Any(summaryToken => summaryToken.Contains(token, StringComparison.OrdinalIgnoreCase)));

            float summaryScore = queryTokens.Length > 0
                ? (float)matchedWords / queryTokens.Length
                : 0f;

            relevanceScore += Math.Min(summaryScore, 1f) * 0.4f;
        }

        return Math.Min(relevanceScore, 1f);
    }
}
