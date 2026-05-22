namespace Arabidopsis.LiteRAG.Models;

public interface IVectorStore
{
    void Add(VectorEntry entry);
    void AddRange(List<VectorEntry> entries);
    List<(VectorEntry Entry, float Score)> Search(float[] queryVector, int topK = 5);
}
