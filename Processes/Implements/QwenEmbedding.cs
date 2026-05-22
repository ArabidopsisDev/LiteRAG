using Arabidopsis.LiteRAG.Processes.Interfaces;
using OpenAI;
using OpenAI.Embeddings;
using System.ClientModel;
using Arabidopsis.LiteRAG.Models;

namespace Arabidopsis.LiteRAG.Processes.Implements;

public class QwenEmbedding :IEmbedding<Semantics>
{
    private readonly EmbeddingClient _client;

    private const int BatchSize = 10;
    private const int MaxConcurrency = 10;

    public QwenEmbedding(string apiKey)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1")
        };

        var openAiClient = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        _client = openAiClient.GetEmbeddingClient("text-embedding-v4");
    }

    /// <summary>
    /// Generates embeddings for texts with associated semantic metadata.
    /// Maintains one-to-one pairing between Semantics and their corresponding embeddings.
    /// </summary>
    /// <param name="semanticChunks">Dictionary mapping Semantics metadata to text content</param>
    /// <param name="cts">Cancellation token</param>
    /// <returns>Dictionary with Semantics keys and embedding arrays as values</returns>
    public async Task<Dictionary<Semantics, float[]>> EmbedAsync(
        Dictionary<Semantics, string> semanticChunks,
        CancellationToken cts)
    {
        if (semanticChunks.Count == 0) return [];

        var semanticsList = semanticChunks.Keys.ToList();
        var textsList = semanticChunks.Values.ToList();

        // Results array maintains index correspondence with input order
        var results = new float[textsList.Count][];

        var batches = textsList
            .Select((text, index) => new { text, index })
            .Chunk(BatchSize)
            .ToList();

        using var semaphore = new SemaphoreSlim(MaxConcurrency);

        var tasks = batches.Select(async batch =>
        {
            await semaphore.WaitAsync(cts);
            try
            {
                var batchTexts = batch.Select(b => b.text).ToList();
                var response = await _client.GenerateEmbeddingsAsync(batchTexts, options: null, cts);

                for (int i = 0; i < batch.Length; i++)
                    results[batch[i].index] = response.Value[i].ToFloats().ToArray();
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        // Reconstruct the result dictionary maintaining Semantics-Embedding pairing
        var resultDictionary = new Dictionary<Semantics, float[]>();
        for (int i = 0; i < semanticsList.Count; i++)
        {
            resultDictionary[semanticsList[i]] = results[i];
        }

        return resultDictionary;
    }
}
