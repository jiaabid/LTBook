using Microsoft.Extensions.AI;

namespace ToolBook.UnitTests.Fakes;

/// <summary>Chat client that returns a scripted answer and records what it was asked. Also used by the integration tests (linked file).</summary>
public sealed class ScriptedChatClient(Func<IReadOnlyList<ChatMessage>, string> answer) : IChatClient
{
    public List<IReadOnlyList<ChatMessage>> Requests { get; } = [];

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var list = messages.ToList();
        Requests.Add(list);
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, answer(list))));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
    }
}
