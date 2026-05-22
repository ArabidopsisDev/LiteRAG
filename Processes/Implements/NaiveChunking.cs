using Arabidopsis.LiteRAG.Processes.Interfaces;
using System.Text;
using System.Text.RegularExpressions;

namespace Arabidopsis.LiteRAG.Processes.Implements;

public class NaiveChunking : IChunking
{
    private readonly string _filePath;
    private readonly int _chunkSize;
    private readonly int _overlap;

    public NaiveChunking(string filePath, int chunkSize = 1500, int overlap = 50)
    {
        _filePath = filePath;

        // The optimal value for this parameter cannot be determined,
        // as the developers lack the funds to conduct experiments
        _chunkSize = chunkSize;

        _overlap = overlap;
    }

    public List<string> Slice()
    {
        var text = File.ReadAllText(_filePath);
        var slices = SplitText(text);

        return slices.Select(x => x.Replace("\n", "").Trim()).ToList();
    }


    public List<string> SplitText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        text = CleanText(text);
        var paragraphs = SplitByParagraphs(text);

        var chunks = new List<string>();
        foreach (var para in paragraphs)
        {
            chunks.AddRange(SplitParagraph(para));
        }

        return chunks;
    }

    private string CleanText(string text)
    {
        text = Regex.Replace(text, @"\n\s*\n", "\n\n");
        text = text.Trim();
        return text;
    }

    private List<string> SplitByParagraphs(string text)
    {
        var paragraphs = new List<string>();
        var currentPara = new StringBuilder();

        using var reader = new StringReader(text);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (currentPara.Length > 0)
                {
                    paragraphs.Add(currentPara.ToString().Trim());
                    currentPara.Clear();
                }
            }
            else
            {
                currentPara.AppendLine(line);
            }
        }

        if (currentPara.Length > 0)
        {
            paragraphs.Add(currentPara.ToString().Trim());
        }

        return paragraphs;
    }

    private List<string> SplitParagraph(string paragraph)
    {
        if (paragraph.Length <= _chunkSize)
        {
            return new List<string> { paragraph };
        }

        var chunks = new List<string>();
        var sentences = SplitIntoSentences(paragraph);

        var currentChunk = new StringBuilder();

        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length + 1 > _chunkSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());

                string overlap = GetOverlap(currentChunk.ToString());
                currentChunk.Clear();
                currentChunk.Append(overlap);
            }

            currentChunk.Append(sentence);
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }

    private List<string> SplitIntoSentences(string text)
    {
        var separators = new[] { "。", "！", "？", "；", ".", "!", "?", ";" };

        var sentences = new List<string>();
        var currentSentence = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            currentSentence.Append(text[i]);

            foreach (var sep in separators)
            {
                if (text[i].ToString() == sep)
                {
                    sentences.Add(currentSentence.ToString());
                    currentSentence.Clear();
                    break;
                }
            }
        }

        if (currentSentence.Length > 0)
        {
            sentences.Add(currentSentence.ToString());
        }

        return sentences;
    }

    private string GetOverlap(string chunk)
    {
        if (chunk.Length <= _overlap)
            return chunk;

        int start = chunk.Length - _overlap;

        int actualStart = chunk.Length;
        for (int i = start; i < chunk.Length; i++)
        {
            if (i > 0 && (chunk[i - 1] == '。' || chunk[i - 1] == '！' || chunk[i - 1] == '？'))
            {
                actualStart = i;
                break;
            }
        }

        if (actualStart >= chunk.Length)
            actualStart = start;

        return chunk.Substring(actualStart);
    }
}
