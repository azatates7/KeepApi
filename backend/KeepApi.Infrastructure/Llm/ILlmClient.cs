namespace KeepApi.Infrastructure.Llm
{
    public interface ILlmClient
    {
        Task<string> SummarizeAsync(string prompt, CancellationToken ct);
    }
}