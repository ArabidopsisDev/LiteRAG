using Arabidopsis.LiteRAG.Models;
using Arabidopsis.LiteRAG.Processes.Interfaces;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace Arabidopsis.LiteRAG.Processes.Implements;

public class DeepSeekClustering(string apiKey) : IClustering<Semantics>
{
    private readonly ChatClient _chatClient = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
    {
        Endpoint = new Uri("https://api.deepseek.com")
    }).GetChatClient("deepseek-v4-flash");

    private const int MaxConcurrentRequests = 16;

    /// <summary>
    /// Process chunks concurrently: segment and generate semantics for each chunk
    /// </summary>
    public async Task<Dictionary<Semantics, string>> ClusterAsync(List<string> chunks, CancellationToken cts = default)
    {
        var results = new ConcurrentDictionary<Semantics, string>();

        await Parallel.ForEachAsync(chunks,
            new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentRequests, CancellationToken = cts },
            async (chunk, _) =>
            {
                var segmentedSemantics = await ProcessChunkAsync(chunk);
                foreach (var item in segmentedSemantics)
                {
                    results.TryAdd(item.Item1, item.Item2);
                }
            });

        return results.OrderBy(x => x.Key.Id).ToDictionary(x => x.Key, x => x.Value);
    }

    /// <summary>
    /// Process single chunk: segment by atomic knowledge points and generate semantics
    /// </summary>
    private async Task<List<(Semantics, string)>> ProcessChunkAsync(string chunk)
    {
        var prompt = BuildPrompt(chunk);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("你是一个专业的文本分析专家。请完成以下任务：\n" +
                                  "1. 将输入的文本块按照知识点的原子性进行分割\n" +
                                  "2. 确保每个分割后的段落只表达一个独立、完整的知识点\n" +
                                  "3. 为每个知识点段落生成2-3个关键词标签\n" +
                                  "4. 生成不超过20字的单句摘要\n" +
                                  "5. 对于被截断的知识点，如果有明确的语义信息进行补全，否则直接丢弃" +
                                  "6. 返回JSON数组，每个元素包含text、tags、summary字段，无其他内容，不要添加markdown代码块结构"),
            new UserChatMessage(prompt)
        };

        var response = await _chatClient.CompleteChatAsync(messages, new ChatCompletionOptions
        {
            Temperature = 0.3f,
            MaxOutputTokenCount = 66666
        });

        var json = response.Value.Content[0].Text;
        return ParseResponse(json);
    }

    /// <summary>
    /// Build prompt for single chunk processing
    /// </summary>
    private static string BuildPrompt(string chunk)
    {
        var sb = new StringBuilder();
        sb.AppendLine("请处理以下文本块：");
        sb.AppendLine();
        sb.AppendLine(chunk);
        sb.AppendLine();
        sb.AppendLine("返回JSON数组格式，示例：");
        sb.AppendLine("[");
        sb.AppendLine("  {\"text\": \"分割后的知识点1\", \"tags\": [\"标签1\", \"标签2\"], \"summary\": \"摘要\"},");
        sb.AppendLine("  {\"text\": \"分割后的知识点2\", \"tags\": [\"标签1\", \"标签2\"], \"summary\": \"摘要\"}");
        sb.AppendLine("]");

        return sb.ToString();
    }

    /// <summary>
    /// Parse response and return semantics-text pairs with auto-incremented IDs
    /// </summary>
    private static List<(Semantics, string)> ParseResponse(string json)
    {
        var result = new List<(Semantics, string)>();
        var idCounter = 0;

        using var doc = JsonDocument.Parse(json);
        var array = doc.RootElement.EnumerateArray().ToList();

        foreach (var item in array)
        {
            var text = item.TryGetProperty("text", out var textProp)
                ? textProp.GetString() ?? ""
                : "";

            if (string.IsNullOrWhiteSpace(text))
                continue;

            var semantics = new Semantics
            {
                Id = idCounter++,
                Tags = item.TryGetProperty("tags", out var tags)
                    ? tags.EnumerateArray()
                        .Select(t => t.GetString() ?? "")
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList()
                    : [],
                Summary = item.TryGetProperty("summary", out var summary)
                    ? (summary.GetString() ?? "").Trim()
                    : ""
            };

            result.Add((semantics, text.Trim()));
        }


        return result;
    }
}
