using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace ToolBook.UnitTests.Fakes;

/// <summary>
/// Deterministic, offline embedding generator: a normalised bag-of-words vector built by hashing each word into a
/// fixed number of buckets. Texts that share words are similar. Good enough to test retrieval without a model.
/// Also used by the integration tests (linked file).
/// </summary>
public sealed partial class HashingEmbeddingGenerator(int dimensions = 512) : IEmbeddingGenerator<string, Embedding<float>>
{
    private static readonly HashSet<string> StopWords =
        ["the", "a", "an", "and", "or", "of", "to", "in", "on", "for", "is", "are", "be", "it", "how", "do", "i", "you", "with", "what", "this", "that", "my", "can"];

    public int Calls { get; private set; }

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        Calls++;
        var embeddings = values.Select(v => new Embedding<float>(Embed(v)));
        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
    }

    public float[] Embed(string text)
    {
        var vector = new float[dimensions];
        foreach (Match word in WordRegex().Matches(text.ToLowerInvariant()))
        {
            if (!StopWords.Contains(word.Value))
            {
                vector[StableHash(word.Value) % dimensions] += 1f;
            }
        }

        var norm = MathF.Sqrt(vector.Sum(x => x * x));
        return norm == 0 ? vector : [.. vector.Select(x => x / norm)];
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
    }

    // FNV-1a: string.GetHashCode() is randomised per process, so it is not stable across runs.
    private static int StableHash(string s)
    {
        var hash = 2166136261u;
        foreach (var ch in s)
        {
            hash = (hash ^ ch) * 16777619u;
        }

        return (int)(hash & 0x7FFFFFFF);
    }

    [GeneratedRegex(@"[a-z0-9]+")]
    private static partial Regex WordRegex();
}
