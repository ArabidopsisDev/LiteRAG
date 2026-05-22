using Arabidopsis.LiteRAG.Models;
using Arabidopsis.LiteRAG.Orchestrations;
using Arabidopsis.LiteRAG.Processes.Implements;

namespace Arabidopsis.LiteRAG
{
    internal class Program
    {
        public static async Task Main()
        {
            var deepSeekApi = Environment.GetEnvironmentVariable("DSAPI")!;
            var qwenApi = Environment.GetEnvironmentVariable("BitchSDAU")!;
            var src = new CancellationTokenSource();

            var ragPipeline = new LinearOrchestration<Semantics>()
                .AddChunking(new NaiveChunking("text.txt"))
                .AddClustering(new DeepSeekClustering(deepSeekApi))
                .AddEmbedding(new QwenEmbedding(qwenApi))
                .AddVectoring(new InMemoryVectoring(qwenApi, "text.txt"));

            var knowledgeBase = await ragPipeline.BuildAsync(src.Token);
        }
    }
}
