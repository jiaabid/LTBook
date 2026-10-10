namespace ToolBook.SharedKernel.Http;

/// <summary>Authorization policy names shared by all modules. The Host defines what each policy requires.</summary>
public static class Policies
{
    public const string Admin = "Admin";

    /// <summary>Rate limiter policy for the AI assistant.</summary>
    public const string AssistantRateLimit = "assistant";
}
