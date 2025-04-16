using System.Net.Http;

namespace VM.Services.Interfaces;

public interface IServerClient
{
    Task DownloadImage(string url, string localPath);
    Task<HttpResponseMessage> GetAsync(string url);
    Task<byte[]> GetByteArrayAsync(string url);
    Task<string> GetToken();
    Task<bool> IsServerConnectable();
    Task<HttpResponseMessage> PostAsync<T>(string url, T? body = null) where T : class;
    Task<HttpResponseMessage> PostAsync(string url, MultipartFormDataContent form);
}