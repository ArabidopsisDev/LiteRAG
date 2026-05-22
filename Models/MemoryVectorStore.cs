namespace Arabidopsis.LiteRAG.Models;

public class MemoryVectorStore : IVectorStore
{
    private readonly List<VectorEntry> _entries = [];
    private readonly Lock _lock = new();

    public void Add(VectorEntry entry)
    {
        lock (_lock)
        {
            _entries.Add(entry);
        }
    }

    public void AddRange(List<VectorEntry> entries)
    {
        lock (_lock)
        {
            _entries.AddRange(entries);
        }
    }

    public List<(VectorEntry Entry, float Score)> Search(float[] queryVector, int topK = 5)
    {
        lock (_lock)
        {
            return _entries
                .Select(entry => (entry, Score: CosineSimilarity(queryVector, entry.Vector)))
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .ToList();
        }
    }

    private float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return dot / (float)(Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
