namespace KeepApi.Infrastructure.Llm
{
    public interface ILlmClient
    {
        Task<string> SummarizeAsync(string prompt, CancellationToken cancellationToken);

        /// <summary>
        /// Bir görsel veya belgeyi (base64 veri + mime type) LLM'e gönderip verilen prompt'a göre metin bir yanıt üretir. Dosya sunucuda kalıcı olarak saklanmaz; sadece istek gövdesinde LLM'e iletilir.
        /// </summary>

        Task<string> SummarizeAttachmentAsync(byte[] fileBytes, string mimeType, string prompt, CancellationToken cancellationToken);
    }
}