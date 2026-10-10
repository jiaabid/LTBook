namespace ToolBook.Knowledge.Assistant;

internal enum AssistantProvider
{
    /// <summary>Local models via Ollama (free, no API key, runs on the developer machine).</summary>
    Ollama,

    /// <summary>Gemini API free tier (needs an API key).</summary>
    Gemini,
}

/// <summary>
/// Configuration section <c>Assistant</c>. Both providers are reached through their OpenAI-compatible endpoints, so
/// one client implementation serves both. Endpoint and models default per provider and can be overridden.
/// The Gemini API key comes from user secrets or the <c>Assistant__ApiKey</c> / <c>GEMINI_API_KEY</c> environment
/// variable and is never committed. Without a usable provider the assistant is disabled.
/// </summary>
internal sealed class AssistantOptions
{
    public const string SectionName = "Assistant";

    public AssistantProvider Provider { get; set; } = AssistantProvider.Gemini;

    public string? ApiKey { get; set; }

    public string? Endpoint { get; set; }

    public string? ChatModel { get; set; }

    public string? EmbeddingModel { get; set; }

    /// <summary>Optional reduced embedding size (the model default is used when null).</summary>
    public int? EmbeddingDimensions { get; set; }

    /// <summary>Chunks retrieved per question.</summary>
    public int TopK { get; set; } = 5;

    /// <summary>
    /// Cosine similarity below which a chunk is considered irrelevant. If the best chunk is below this, the assistant
    /// answers "not found" instead of guessing. Embedding-model specific: for nomic-embed-text on the sample manuals,
    /// on-topic questions scored ≥ 0.59 and off-topic ones ≤ 0.52.
    /// </summary>
    public double MinSimilarity { get; set; } = 0.55;

    /// <summary>Local CPU inference can be slow; allow generous time per model call.</summary>
    public int TimeoutSeconds { get; set; } = 180;

    public string ResolvedEndpoint => Endpoint ?? Provider switch
    {
        AssistantProvider.Ollama => "http://localhost:11434/v1/",
        _ => "https://generativelanguage.googleapis.com/v1beta/openai/",
    };

    public string ResolvedChatModel => ChatModel ?? Provider switch
    {
        AssistantProvider.Ollama => "llama3.2:3b",
        _ => "gemini-2.5-flash",
    };

    public string ResolvedEmbeddingModel => EmbeddingModel ?? Provider switch
    {
        AssistantProvider.Ollama => "nomic-embed-text",
        _ => "gemini-embedding-001",
    };

    /// <summary>Ollama ignores the key but the OpenAI client requires a non-empty one.</summary>
    public string? ResolveApiKey(string? fallbackApiKey) => Provider switch
    {
        AssistantProvider.Ollama => string.IsNullOrWhiteSpace(ApiKey) ? "ollama" : ApiKey,
        _ => string.IsNullOrWhiteSpace(ApiKey) ? fallbackApiKey : ApiKey,
    };
}
